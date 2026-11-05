// Lógica común de los proxys. Mantener sincronizada con ObservApp.Web/Program.cs
// (MapRssProxyEndpoint y MapWpProxyEndpoint), que da el mismo servicio en desarrollo local.

// Dominios cuyos feeds RSS se pueden pedir (y sus subdominios).
const RSS_ALLOWED_DOMAINS = [
    'tubkala.com',
    'astrobitacora.com',
    'astrobitos.org',
    'megaphone.fm',
    'esa.int',
    'nasa.gov',
    'eso.org',
    'skyandtelescope.org',
    'astronomy.com',
    'spaceweather.com',
];

const WP_ALLOWED_DOMAIN = 'tubkala.com';

const RSS_USER_AGENT = 'ObservApp/1.0 RSS Reader';

// El WAF de Tubkala responde HTML a clientes que no parecen un navegador.
const WP_USER_AGENT =
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 ' +
    '(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36';

const UPSTREAM_TIMEOUT_MS = 15000;

/** Dominio exacto o subdominio suyo (evita que "evilnasa.gov" pase por "nasa.gov"). */
function isHostAllowed(host, domain) {
    const h = host.toLowerCase();
    return h === domain || h.endsWith('.' + domain);
}

/** Devuelve la URL si es absoluta http(s), o null. */
function parseHttpUrl(value) {
    if (!value) return null;
    try {
        const url = new URL(value);
        return url.protocol === 'http:' || url.protocol === 'https:' ? url : null;
    } catch {
        return null;
    }
}

function getAllowedRssUrl(value) {
    const url = parseHttpUrl(value);
    return url && RSS_ALLOWED_DOMAINS.some(d => isHostAllowed(url.hostname, d)) ? url : null;
}

function getAllowedWpUrl(value) {
    const url = parseHttpUrl(value);
    return url &&
        isHostAllowed(url.hostname, WP_ALLOWED_DOMAIN) &&
        (url.pathname + url.search).toLowerCase().includes('/wp-json/')
        ? url
        : null;
}

function fetchUpstream(url, headers) {
    return fetch(url, {
        headers,
        redirect: 'follow',
        signal: AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
    });
}

/** Respuesta de error con el mismo formato (problem+json) que Results.Problem en .NET. */
function problem(status, detail) {
    return {
        status,
        jsonBody: { title: 'Proxy error', status, detail },
        headers: { 'Content-Type': 'application/problem+json; charset=utf-8' },
    };
}

/** Envuelve la respuesta de la API REST de WordPress con los totales de paginación. */
function buildWpPayload(response, json) {
    const total = response.headers.get('X-WP-Total') ?? '0';
    const totalPages = response.headers.get('X-WP-TotalPages') ?? '1';
    return `{"total":${Number(total) || 0},"totalPages":${Number(totalPages) || 1},"posts":${json}}`;
}

module.exports = {
    RSS_USER_AGENT,
    WP_USER_AGENT,
    getAllowedRssUrl,
    getAllowedWpUrl,
    fetchUpstream,
    problem,
    buildWpPayload,
};
