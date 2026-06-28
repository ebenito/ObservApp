using System.Globalization;
using System.Net.Sockets;
using System.Text;
using ObservApp.Shared.Interfaces;
using ObservApp.Shared.Models;

namespace ObservApp.Services;

// NOTA DE UBICACIÓN: este archivo vive físicamente en
// ObservApp/Platforms/Android/ para que el SDK de MAUI lo compile
// EXCLUSIVAMENTE en el TargetFramework net10.0-android, sin necesidad de
// guardas #if. El namespace se mantiene como ObservApp.Services por simetría
// con el resto de servicios de plataforma del proyecto.

/// <summary>
/// Implementación de <see cref="ICameraService"/> para Android usando el
/// protocolo estándar PTP/IP (ISO 15740 sobre TCP, puerto 15740 por defecto)
/// directamente vía <see cref="Socket"/>/<see cref="TcpClient"/>. No depende
/// de ningún SDK propietario: cualquier cámara que exponga su modo
/// "Wi-Fi remoto" / "Smart Remote Control" como servidor PTP/IP (la mayoría
/// de réflex/mirrorless modernas — Canon, Nikon, Sony, Fujifilm, Panasonic,
/// Olympus) puede controlarse con este cliente, una vez el teléfono está
/// conectado a la red WiFi que crea la cámara (o ambos a la misma red).
///
/// LIMITACIÓN CONOCIDA Y DOCUMENTADA: el estándar PTP solo define un
/// conjunto BASE de "device properties" (FNumber, ExposureTime,
/// ExposureIndex...) que algunos fabricantes implementan tal cual y otros
/// sustituyen total o parcialmente por extensiones vendor-specific con sus
/// propios códigos de operación/propiedad (notablemente Canon EOS). Esta
/// clase usa únicamente los códigos PTP estándar (ISO 15740) — si una cámara
/// concreta no responde a SetDevicePropValue con esos códigos, haría falta
/// añadir su extensión vendor en una capa superior; eso queda fuera del
/// alcance de este cliente agnóstico de fabricante.
/// </summary>
public sealed class PtpIpCameraService : ICameraService, IDisposable
{
    private const int DefaultPort = 15740;

    // ── Códigos de paquete de la capa de transporte PTP/IP ──────────────────
    private const uint PKT_INIT_COMMAND_REQUEST = 1;
    private const uint PKT_INIT_COMMAND_ACK     = 2;
    private const uint PKT_INIT_EVENT_REQUEST   = 3;
    private const uint PKT_INIT_EVENT_ACK       = 4;
    private const uint PKT_OPERATION_REQUEST    = 6;
    private const uint PKT_OPERATION_RESPONSE   = 7;
    private const uint PKT_START_DATA           = 9;
    private const uint PKT_DATA                 = 10;
    private const uint PKT_END_DATA              = 12;

    // ── Códigos de operación PTP estándar (ISO 15740) ───────────────────────
    private const ushort OP_OPEN_SESSION          = 0x1002;
    private const ushort OP_CLOSE_SESSION         = 0x1003;
    private const ushort OP_SET_DEVICE_PROP_VALUE = 0x1016;
    private const ushort OP_INITIATE_CAPTURE      = 0x100E;

    private const ushort PTP_RC_OK = 0x2001;

    // ── Códigos de propiedad PTP estándar usados para exposición ────────────
    private const ushort PROP_EXPOSURE_TIME  = 0x500D; // velocidad de obturación
    private const ushort PROP_F_NUMBER       = 0x5007; // apertura (f-number × 100)
    private const ushort PROP_EXPOSURE_INDEX = 0x500F; // ISO

    private readonly Guid _clientGuid = Guid.NewGuid();

    // Serializa el envío de operaciones: PTP es un protocolo de transacciones
    // secuenciales sobre una única conexión — dos llamadas concurrentes
    // (p. ej. SetExposureAsync + TriggerCaptureAsync desde hilos distintos)
    // corromperían la sincronía petición/respuesta si se entrelazan.
    private readonly SemaphoreSlim _opLock = new(1, 1);

    private TcpClient? _commandSocket;
    private TcpClient? _eventSocket;
    private NetworkStream? _commandStream;
    private uint _transactionId = 1;
    private readonly uint _sessionId = 1;

    /// <summary>
    /// IP de la cámara en modo punto de acceso WiFi (o IP del teléfono/cámara
    /// en la red común, según el modo de la cámara). Configurable desde la UI
    /// de ObservApp antes de conectar.
    /// </summary>
    public string Host { get; set; } = "192.168.1.1";

    public int Port { get; set; } = DefaultPort;

    public bool IsConnected { get; private set; }
    public string? LastError { get; private set; }
    public event Action<bool>? ConnectionChanged;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        LastError = null;
        try
        {
            _commandSocket = new TcpClient();
            await _commandSocket.ConnectAsync(Host, Port, cancellationToken);
            _commandStream = _commandSocket.GetStream();

            // ── Handshake del canal de comandos ───────────────────────────
            await WritePacketAsync(_commandStream, PKT_INIT_COMMAND_REQUEST,
                BuildInitCommandRequestPayload(), cancellationToken);

            var (ackType, ackPayload) = await ReadPacketAsync(_commandStream, cancellationToken);
            if (ackType != PKT_INIT_COMMAND_ACK || ackPayload.Length < 4)
            {
                LastError = "La cámara rechazó el handshake PTP/IP (Init Command).";
                return false;
            }

            var connectionNumber = BitConverter.ToUInt32(ackPayload, 0);

            // ── Handshake del canal de eventos (socket TCP separado) ──────
            _eventSocket = new TcpClient();
            await _eventSocket.ConnectAsync(Host, Port, cancellationToken);
            var eventStream = _eventSocket.GetStream();

            await WritePacketAsync(eventStream, PKT_INIT_EVENT_REQUEST,
                BitConverter.GetBytes(connectionNumber), cancellationToken);

            var (eventAckType, _) = await ReadPacketAsync(eventStream, cancellationToken);
            if (eventAckType != PKT_INIT_EVENT_ACK)
            {
                LastError = "La cámara rechazó el handshake PTP/IP (Init Event).";
                return false;
            }

            // ── Abrir sesión PTP ───────────────────────────────────────────
            var opened = await SendOperationAsync(OP_OPEN_SESSION,
                new uint[] { _sessionId }, cancellationToken);

            SetConnected(opened);
            if (!opened)
                LastError ??= "La cámara no aceptó OpenSession (PTP).";

            return opened;
        }
        catch (Exception ex)
        {
            LastError = $"Error de conexión PTP/IP: {ex.Message}";
            SetConnected(false);
            return false;
        }
    }

    public async Task SetExposureAsync(ExposureSetting settings)
    {
        if (_commandStream is null)
        {
            LastError = "No hay conexión PTP/IP activa.";
            return;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var ct = timeoutCts.Token;

            // F-number en PTP estándar se expresa como entero ×100 (f/8 → 800).
            var fNumberValue = ParseApertureToPtpFNumber(settings.Aperture);
            await SetDevicePropValueAsync(PROP_F_NUMBER, fNumberValue, ct);

            await SetDevicePropValueAsync(PROP_EXPOSURE_INDEX, (uint)settings.Iso, ct);

            // ExposureTime en PTP estándar se expresa en décimas de segundo.
            var exposureTimeValue = (uint)Math.Round(settings.ShutterSeconds * 10000.0);
            await SetDevicePropValueAsync(PROP_EXPOSURE_TIME, exposureTimeValue, ct);
        }
        catch (Exception ex)
        {
            LastError = $"Error aplicando exposición vía PTP/IP: {ex.Message}";
        }
    }

    public async Task TriggerCaptureAsync(CancellationToken ct)
    {
        if (_commandStream is null)
        {
            LastError = "No hay conexión PTP/IP activa.";
            return;
        }

        // StorageID=0xFFFFFFFF ("cualquier almacenamiento disponible") y
        // ObjectFormatCode=0 (formato por defecto de la cámara) son los
        // valores estándar recomendados por la especificación PTP cuando no
        // se necesita dirigir la captura a una tarjeta/formato concretos.
        var ok = await SendOperationAsync(OP_INITIATE_CAPTURE,
            new uint[] { 0xFFFFFFFF, 0 }, ct);

        if (!ok)
            LastError = "La cámara no confirmó el disparo (InitiateCapture).";
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_commandStream is not null)
                await SendOperationAsync(OP_CLOSE_SESSION, Array.Empty<uint>(), CancellationToken.None);
        }
        catch
        {
            // Best-effort: si la cámara ya cortó la conexión no es un error a reportar.
        }
        finally
        {
            _commandSocket?.Close();
            _eventSocket?.Close();
            SetConnected(false);
        }
    }

    // ── Helpers de propiedad ──────────────────────────────────────────────────

    private async Task SetDevicePropValueAsync(ushort propCode, uint value, CancellationToken ct)
    {
        // SetDevicePropValue requiere fase de datos: la operación se envía
        // primero (con el código de propiedad como parámetro) y el valor se
        // transmite después en paquetes de datos PTP/IP independientes
        // (Start Data + Data + End Data).
        var dataBytes = BitConverter.GetBytes(value);
        await SendOperationWithDataAsync(OP_SET_DEVICE_PROP_VALUE,
            new uint[] { propCode }, dataBytes, ct);
    }

    private static uint ParseApertureToPtpFNumber(string aperture)
    {
        var trimmed = aperture.Trim();
        var idx = trimmed.IndexOf('/');
        var numberPart = idx >= 0 ? trimmed[(idx + 1)..] : trimmed;

        return double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out var f)
            ? (uint)Math.Round(f * 100)
            : 0;
    }

    // ── Capa de transporte PTP/IP ──────────────────────────────────────────────

    private byte[] BuildInitCommandRequestPayload()
    {
        const string clientName = "ObservApp";
        var nameBytes = Encoding.Unicode.GetBytes(clientName + "\0"); // UTF-16LE terminado en NUL

        using var ms = new MemoryStream();
        ms.Write(_clientGuid.ToByteArray(), 0, 16);
        ms.Write(nameBytes, 0, nameBytes.Length);
        ms.Write(BitConverter.GetBytes(0x00010000u), 0, 4); // versión de protocolo PTP/IP 1.0
        return ms.ToArray();
    }

    private Task<bool> SendOperationAsync(ushort opCode, uint[] parameters, CancellationToken ct)
        => SendOperationWithDataAsync(opCode, parameters, dataPhasePayload: null, ct);

    private async Task<bool> SendOperationWithDataAsync(
        ushort opCode, uint[] parameters, byte[]? dataPhasePayload, CancellationToken ct)
    {
        if (_commandStream is null) return false;

        await _opLock.WaitAsync(ct);
        try
        {
            var txId = _transactionId++;
            var dataPhaseInfo = dataPhasePayload is null ? 1u : 2u; // 1=sin datos, 2=envío de datos al dispositivo

            using var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(dataPhaseInfo), 0, 4);
            ms.Write(BitConverter.GetBytes(opCode), 0, 2);
            ms.Write(BitConverter.GetBytes(txId), 0, 4);
            foreach (var p in parameters)
                ms.Write(BitConverter.GetBytes(p), 0, 4);

            await WritePacketAsync(_commandStream, PKT_OPERATION_REQUEST, ms.ToArray(), ct);

            if (dataPhasePayload is not null)
                await SendDataPhaseAsync(txId, dataPhasePayload, ct);

            var (responseType, responsePayload) = await ReadPacketAsync(_commandStream, ct);
            if (responseType != PKT_OPERATION_RESPONSE || responsePayload.Length < 2)
                return false;

            var responseCode = BitConverter.ToUInt16(responsePayload, 0);
            return responseCode == PTP_RC_OK;
        }
        finally
        {
            _opLock.Release();
        }
    }

    private async Task SendDataPhaseAsync(uint transactionId, byte[] payload, CancellationToken ct)
    {
        if (_commandStream is null) return;

        var startPayload = new byte[12];
        BitConverter.GetBytes(transactionId).CopyTo(startPayload, 0);
        BitConverter.GetBytes((ulong)payload.Length).CopyTo(startPayload, 4);
        await WritePacketAsync(_commandStream, PKT_START_DATA, startPayload, ct);

        var dataPayload = new byte[4 + payload.Length];
        BitConverter.GetBytes(transactionId).CopyTo(dataPayload, 0);
        payload.CopyTo(dataPayload, 4);
        await WritePacketAsync(_commandStream, PKT_DATA, dataPayload, ct);

        var endPayload = BitConverter.GetBytes(transactionId);
        await WritePacketAsync(_commandStream, PKT_END_DATA, endPayload, ct);
    }

    private static async Task WritePacketAsync(
        Stream stream, uint packetType, byte[] payload, CancellationToken ct)
    {
        var length = (uint)(8 + payload.Length); // 4 (longitud) + 4 (tipo) + payload
        var header = new byte[8];
        BitConverter.GetBytes(length).CopyTo(header, 0);
        BitConverter.GetBytes(packetType).CopyTo(header, 4);

        await stream.WriteAsync(header, 0, header.Length, ct);
        if (payload.Length > 0)
            await stream.WriteAsync(payload, 0, payload.Length, ct);
        await stream.FlushAsync(ct);
    }

    private static async Task<(uint Type, byte[] Payload)> ReadPacketAsync(
        Stream stream, CancellationToken ct)
    {
        var header = await ReadExactAsync(stream, 8, ct);
        var length = BitConverter.ToUInt32(header, 0);
        var type = BitConverter.ToUInt32(header, 4);

        var payloadLength = (int)Math.Max(0, length - 8);
        var payload = payloadLength > 0
            ? await ReadExactAsync(stream, payloadLength, ct)
            : Array.Empty<byte>();

        return (type, payload);
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer, offset, count - offset, ct);
            if (read == 0) throw new IOException("Conexión PTP/IP cerrada inesperadamente por la cámara.");
            offset += read;
        }
        return buffer;
    }

    private void SetConnected(bool connected)
    {
        if (IsConnected == connected) return;
        IsConnected = connected;
        ConnectionChanged?.Invoke(connected);
    }

    public void Dispose()
    {
        _commandStream?.Dispose();
        _commandSocket?.Dispose();
        _eventSocket?.Dispose();
        _opLock.Dispose();
    }
}
