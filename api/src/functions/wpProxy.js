const { app } = require('@azure/functions');
const { WP_USER_AGENT, getAllowedWpUrl, fetchUpstream, problem, buildWpPayload } = require('../shared/proxy');

// GET /api/wp-proxy?url={url de la API REST de WordPress de tubkala.com}
// Desde el navegador no se puede fijar User-Agent y el WAF de Tubkala devuelve HTML.
app.http('wp-proxy', {
    route: 'wp-proxy',
    methods: ['GET'],
    authLevel: 'anonymous',
    handler: async (request, context) => {
        const url = getAllowedWpUrl(request.query.get('url'));
        if (!url) {
            return problem(400, 'URL no permitida');
        }

        try {
            const response = await fetchUpstream(url, {
                'User-Agent': WP_USER_AGENT,
                Accept: 'application/json',
            });
            if (!response.ok) {
                return problem(502, `WP API respondió ${response.status}`);
            }

            const json = await response.text();
            if (json.trimStart().startsWith('<')) {
                return problem(502, 'WP API devolvió HTML en lugar de JSON');
            }

            return {
                status: 200,
                body: buildWpPayload(response, json),
                headers: { 'Content-Type': 'application/json; charset=utf-8' },
            };
        } catch (error) {
            context.warn(`wp-proxy: ${error.message}`);
            return problem(502, error.message);
        }
    },
});
