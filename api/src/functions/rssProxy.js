const { app } = require('@azure/functions');
const { RSS_USER_AGENT, getAllowedRssUrl, fetchUpstream, problem } = require('../shared/proxy');

// GET /api/rss-proxy?url={feed}
// Descarga el feed desde el servidor: el navegador no puede por CORS.
app.http('rss-proxy', {
    route: 'rss-proxy',
    methods: ['GET'],
    authLevel: 'anonymous',
    handler: async (request, context) => {
        const url = getAllowedRssUrl(request.query.get('url'));
        if (!url) {
            return problem(400, 'Dominio no permitido');
        }

        try {
            const response = await fetchUpstream(url, { 'User-Agent': RSS_USER_AGENT });
            if (!response.ok) {
                return problem(502, `El feed respondió ${response.status}`);
            }

            // Se devuelve tal cual: si el origen responde HTML (p. ej. un WAF),
            // RssFeedService lo detecta y lo explica al usuario.
            return {
                status: 200,
                body: await response.text(),
                headers: { 'Content-Type': 'application/xml; charset=utf-8' },
            };
        } catch (error) {
            context.warn(`rss-proxy ${url.hostname}: ${error.message}`);
            return problem(502, error.message);
        }
    },
});
