using System.Diagnostics;

namespace ObservApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Capturar excepciones no controladas para evitar el diálogo JIT
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "ObservApp" };

		// ── Cierre limpio en Windows ─────────────────────────────────────────
		window.Destroying += OnWindowDestroying;

		return window;
	}

	private void OnWindowDestroying(object? sender, EventArgs e)
	{
		try
		{
			// Forzar terminación limpia del proceso en Windows
			// Evita el diálogo JIT "excepción win32 no controlada"
#if WINDOWS
            Microsoft.UI.Xaml.Application.Current?.Exit();
#endif
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[App.OnWindowDestroying] {ex.Message}");
		}
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var ex = e.ExceptionObject as Exception;
		Debug.WriteLine($"[UNHANDLED] {ex?.Message}\n{ex?.StackTrace}");
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		Debug.WriteLine($"[UNOBSERVED] {e.Exception?.Message}");
		e.SetObserved(); // Evita que la excepción tumbe el proceso
	}
}