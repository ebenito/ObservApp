using System.Net;
using System.Text;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Articulos;

public sealed class WpRestArticleProviderTests
{
	private const string BaseUrl = "https://tubkala.com";
	private const string ProxyUrl = "https://localhost:7294";

	private const string CategoriesJson =
		"""[{"id":34,"slug":"astroturismo"},{"id":12,"slug":"astronomia"}]""";

	private const string PostsJson =
		"""
		[{"id":1,"date":"2026-11-01T10:00:00","link":"https://tubkala.com/a/",
		  "title":{"rendered":"Perseidas"},"excerpt":{"rendered":"<p>Lluvia</p>"},"content":{"rendered":"<p>c</p>"}}]
		""";

	private const string FeedXml =
		"""
		<?xml version="1.0" encoding="UTF-8"?>
		<rss version="2.0"><channel><title>Tubkala</title>
		<item><title>Noche de estrellas</title><link>https://tubkala.com/1/</link><guid>1</guid>
		  <pubDate>Sat, 01 Nov 2026 10:00:00 +0000</pubDate>
		  <category><![CDATA[Astronomía]]></category><category><![CDATA[Curiosidades]]></category>
		  <description>uno</description></item>
		<item><title>Ruta por la sierra</title><link>https://tubkala.com/2/</link><guid>2</guid>
		  <pubDate>Fri, 31 Oct 2026 10:00:00 +0000</pubDate>
		  <category><![CDATA[Senderismo]]></category>
		  <description>dos</description></item>
		<item><title>Eclipse de agosto</title><link>https://tubkala.com/3/</link><guid>3</guid>
		  <pubDate>Thu, 30 Oct 2026 10:00:00 +0000</pubDate>
		  <category><![CDATA[Eclipse]]></category>
		  <description>tres</description></item>
		</channel></rss>
		""";

	// ── Helpers puros ─────────────────────────────────────────────────────────

	[Theory]
	[InlineData("Astronomía", "astronomia")]
	[InlineData("  Gafas eclipse ", "gafas-eclipse")]
	[InlineData("astroturismo", "astroturismo")]
	[InlineData("Ciència &amp; Cosmos", "ciencia-&-cosmos")]
	[InlineData("", "")]
	public void NormalizeSlug_RemovesAccentsCaseAndSpaces(string input, string expected)
	{
		Assert.Equal(expected, WpRestArticleProvider.NormalizeSlug(input));
	}

	[Fact]
	public void BuildPostsUrl_WithoutCategories_HasNoCategoriesParameter()
	{
		var url = WpRestArticleProvider.BuildPostsUrl(BaseUrl + "/", 2, 10, null);

		Assert.StartsWith($"{BaseUrl}/wp-json/wp/v2/posts?page=2&per_page=10&_fields=", url);
		Assert.DoesNotContain("categories=", url);
	}

	[Fact]
	public void BuildPostsUrl_WithCategories_AppendsCategoriesParameter()
	{
		var url = WpRestArticleProvider.BuildPostsUrl(BaseUrl, 1, 10, "12,34");

		Assert.EndsWith("&categories=12,34", url);
	}

	[Fact]
	public void ParseCategoryIds_DirectArray_ReturnsOnlyRequestedSlugsSorted()
	{
		var json = """[{"id":34,"slug":"astroturismo"},{"id":12,"slug":"astronomia"},{"id":99,"slug":"otra"}]""";

		var ids = WpRestArticleProvider.ParseCategoryIds(json, ["astronomia", "astroturismo"]);

		Assert.Equal(new[] { 12, 34 }, ids.ToArray());
	}

	[Fact]
	public void ParseCategoryIds_ProxyWrapper_ReadsPostsField()
	{
		var json = """{"total":2,"totalPages":1,"posts":[{"id":12,"slug":"astronomia"}]}""";

		var ids = WpRestArticleProvider.ParseCategoryIds(json, ["astronomia"]);

		Assert.Equal(new[] { 12 }, ids.ToArray());
	}

	[Theory]
	[InlineData("no es json")]
	[InlineData("{\"message\":\"rest_forbidden\"}")]
	[InlineData("[]")]
	public void ParseCategoryIds_InvalidOrEmpty_ReturnsEmpty(string json)
	{
		Assert.Empty(WpRestArticleProvider.ParseCategoryIds(json, ["astronomia"]));
	}

	// ── Ruta directa (MAUI) ───────────────────────────────────────────────────

	[Fact]
	public async Task GetPageAsync_Direct_ResolvesCategoriesOnceAndFiltersPosts()
	{
		var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath switch
		{
			"/wp-json/wp/v2/categories" => Json(CategoriesJson),
			"/wp-json/wp/v2/posts" => Json(PostsJson, ("X-WP-Total", "1"), ("X-WP-TotalPages", "1")),
			_ => new HttpResponseMessage(HttpStatusCode.NotFound),
		});
		var provider = CreateDirect(handler, ["astronomia", "astroturismo"]);

		var first = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);
		var second = await provider.GetPageAsync(2, 10, TestContext.Current.CancellationToken);

		Assert.Null(provider.LastError);
		Assert.False(provider.LastCallUsedRssFallback);
		Assert.Single(first.Items);
		Assert.Equal("Perseidas", first.Items[0].Title);
		Assert.Equal(1, first.Total);
		Assert.Single(second.Items);

		var categoriesCalls = handler.Requests.Where(u => u.Contains("/wp/v2/categories")).ToList();
		var postsCalls = handler.Requests.Where(u => u.Contains("/wp/v2/posts")).ToList();
		Assert.Single(categoriesCalls);
		Assert.Contains("slug=astronomia%2Castroturismo", categoriesCalls[0]);
		Assert.Equal(2, postsCalls.Count);
		Assert.All(postsCalls, u => Assert.Contains("&categories=12,34", u));
		Assert.Contains("page=2", postsCalls[1]);
	}

	[Fact]
	public async Task GetPageAsync_Direct_WithoutCategories_DoesNotCallCategoriesEndpoint()
	{
		var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath switch
		{
			"/wp-json/wp/v2/posts" => Json(PostsJson, ("X-WP-Total", "1"), ("X-WP-TotalPages", "1")),
			_ => new HttpResponseMessage(HttpStatusCode.NotFound),
		});
		var provider = CreateDirect(handler, categorySlugs: null);

		var page = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		Assert.Single(page.Items);
		Assert.DoesNotContain(handler.Requests, u => u.Contains("/wp/v2/categories"));
		Assert.DoesNotContain(handler.Requests, u => u.Contains("categories="));
	}

	[Fact]
	public async Task GetPageAsync_Direct_UnknownSlugs_ShowsAllPostsAndReportsIt()
	{
		var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath switch
		{
			"/wp-json/wp/v2/categories" => Json("[]"),
			"/wp-json/wp/v2/posts" => Json(PostsJson, ("X-WP-Total", "1"), ("X-WP-TotalPages", "1")),
			_ => new HttpResponseMessage(HttpStatusCode.NotFound),
		});
		var provider = CreateDirect(handler, ["inexistente"]);

		var page = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		Assert.Single(page.Items);
		Assert.NotNull(provider.LastError);
		Assert.Contains("inexistente", provider.LastError);
		Assert.DoesNotContain(handler.Requests, u => u.Contains("categories="));
	}

	[Fact]
	public async Task GetPageAsync_Direct_RestBlocked_FallsBackToRssFilteredByCategoryName()
	{
		// Escenario real de Tubkala: WP Cerber devuelve 403 HTML a wp/v2 pero sirve /feed/.
		var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath switch
		{
			"/feed/" => Xml(FeedXml),
			_ => Html(HttpStatusCode.Forbidden, "<!DOCTYPE html><html><title>403 Access Forbidden</title></html>"),
		});
		var provider = CreateDirect(handler, ["astronomia", "eclipse"]);

		var page = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		Assert.True(provider.LastCallUsedRssFallback);
		Assert.Null(provider.LastError);
		Assert.Equal(new[] { "Noche de estrellas", "Eclipse de agosto" }, page.Items.Select(a => a.Title).ToArray());
		Assert.Equal(2, page.Total);
	}

	[Fact]
	public async Task GetPageAsync_Direct_RssFallbackWithoutMatchingCategories_ReportsIt()
	{
		var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath switch
		{
			"/feed/" => Xml(FeedXml),
			_ => new HttpResponseMessage(HttpStatusCode.Forbidden),
		});
		var provider = CreateDirect(handler, ["geologia"]);

		var page = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		Assert.Empty(page.Items);
		Assert.Contains("categorías configuradas", provider.LastError);
	}

	// ── Ruta por proxy (WASM) ─────────────────────────────────────────────────

	[Fact]
	public async Task GetPageAsync_Proxy_ResolvesCategoriesThroughWpProxyAndFiltersPosts()
	{
		var handler = new FakeHandler(req =>
		{
			var target = ProxiedUrl(req);
			if (target.Contains("/wp/v2/categories"))
				return Json("""{"total":2,"totalPages":1,"posts":""" + CategoriesJson + "}");
			if (target.Contains("/wp/v2/posts"))
				return Json("""{"total":1,"totalPages":1,"posts":""" + PostsJson + "}");
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var provider = CreateProxied(handler, ["astronomia", "astroturismo"]);

		var page = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		Assert.Null(provider.LastError);
		Assert.Single(page.Items);
		Assert.All(handler.Requests, u => Assert.StartsWith($"{ProxyUrl}/api/wp-proxy?url=", u));
		var postsCall = handler.Requests.Single(u => ProxiedUrl(u).Contains("/wp/v2/posts"));
		Assert.Contains("categories=12,34", ProxiedUrl(postsCall));
	}

	[Fact]
	public async Task GetPageAsync_Proxy_ProblemJson_ReportsUpstreamDetail()
	{
		// El proxy de Azure/SSR responde 502 problem+json con el detalle del origen;
		// el RSS también falla para que el aviso no se borre al tener respaldo.
		var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath switch
		{
			"/api/wp-proxy" => Json("""{"title":"Proxy error","status":502,"detail":"WP API respondió 403"}""", HttpStatusCode.BadGateway),
			_ => new HttpResponseMessage(HttpStatusCode.BadGateway),
		});
		var provider = CreateProxied(handler, ["astronomia"]);

		var page = await provider.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		Assert.Empty(page.Items);
		Assert.NotNull(provider.LastError);
		Assert.Contains("WP API respondió 403", provider.LastError);
		Assert.DoesNotContain("net_http", provider.LastError);
	}

	// ── Infraestructura ───────────────────────────────────────────────────────

	private static WpRestArticleProvider CreateDirect(FakeHandler handler, IEnumerable<string>? categorySlugs)
		=> new(new HttpClient(handler), BaseUrl, "Tubkala", "es", proxyBaseUrl: null, categorySlugs: categorySlugs);

	private static WpRestArticleProvider CreateProxied(FakeHandler handler, IEnumerable<string>? categorySlugs)
		=> new(new HttpClient(handler), BaseUrl, "Tubkala", "es", proxyBaseUrl: ProxyUrl, categorySlugs: categorySlugs);

	private static string ProxiedUrl(HttpRequestMessage req) => ProxiedUrl(req.RequestUri!.ToString());

	private static string ProxiedUrl(string requestUrl)
	{
		var query = new Uri(requestUrl).Query;
		var marker = "url=";
		var index = query.IndexOf(marker, StringComparison.Ordinal);
		return index < 0 ? string.Empty : Uri.UnescapeDataString(query[(index + marker.Length)..]);
	}

	private static HttpResponseMessage Json(string body, params (string Name, string Value)[] headers)
		=> Json(body, HttpStatusCode.OK, headers);

	private static HttpResponseMessage Json(string body, HttpStatusCode status, params (string Name, string Value)[] headers)
	{
		var response = new HttpResponseMessage(status)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/json"),
		};
		foreach (var (name, value) in headers)
			response.Headers.TryAddWithoutValidation(name, value);
		return response;
	}

	private static HttpResponseMessage Xml(string body)
		=> new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/rss+xml") };

	private static HttpResponseMessage Html(HttpStatusCode status, string body)
		=> new(status) { Content = new StringContent(body, Encoding.UTF8, "text/html") };

	private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
	{
		public List<string> Requests { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Requests.Add(request.RequestUri!.ToString());
			return Task.FromResult(respond(request));
		}
	}
}
