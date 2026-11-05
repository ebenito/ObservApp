const { test } = require('node:test');
const assert = require('node:assert/strict');
const { getAllowedRssUrl, getAllowedWpUrl, buildWpPayload, problem } = require('../src/shared/proxy');

test('rss: admite dominios del catálogo y sus subdominios', () => {
    for (const url of [
        'https://www.astrobitacora.com/feed/',
        'https://feeds.megaphone.fm/nationalaeronauticsandspaceadministration5412631684',
        'http://www.esa.int/rssfeed/Our_Activities/Space_Science',
        'https://apod.nasa.gov/apod.rss',
        'https://tubkala.com/feed/',
    ]) {
        assert.ok(getAllowedRssUrl(url), url);
    }
});

test('rss: rechaza dominios ajenos, parecidos o esquemas no http', () => {
    for (const url of [
        'https://evil.com/feed',
        'https://evilnasa.gov/feed',
        'https://nasa.gov.evil.com/feed',
        'file:///etc/passwd',
        'ftp://nasa.gov/feed',
        'no es una url',
        '',
        null,
    ]) {
        assert.equal(getAllowedRssUrl(url), null, String(url));
    }
});

test('wp: solo la API REST de tubkala.com', () => {
    assert.ok(getAllowedWpUrl('https://tubkala.com/wp-json/wp/v2/posts?per_page=10'));
    assert.ok(getAllowedWpUrl('https://www.tubkala.com/WP-JSON/wp/v2/posts'));
    assert.equal(getAllowedWpUrl('https://tubkala.com/feed/'), null);
    assert.equal(getAllowedWpUrl('https://nottubkala.com/wp-json/wp/v2/posts'), null);
    assert.equal(getAllowedWpUrl('https://evil.com/?r=tubkala.com/wp-json/'), null);
});

test('wp: envuelve los posts con los totales de paginación', () => {
    const response = { headers: new Headers({ 'X-WP-Total': '42', 'X-WP-TotalPages': '5' }) };

    const payload = JSON.parse(buildWpPayload(response, '[{"id":1}]'));

    assert.deepEqual(payload, { total: 42, totalPages: 5, posts: [{ id: 1 }] });
});

test('wp: sin cabeceras de paginación usa 0 y 1', () => {
    const payload = JSON.parse(buildWpPayload({ headers: new Headers() }, '[]'));

    assert.equal(payload.total, 0);
    assert.equal(payload.totalPages, 1);
});

test('problem: formato problem+json', () => {
    const result = problem(400, 'Dominio no permitido');

    assert.equal(result.status, 400);
    assert.equal(result.jsonBody.detail, 'Dominio no permitido');
    assert.match(result.headers['Content-Type'], /problem\+json/);
});
