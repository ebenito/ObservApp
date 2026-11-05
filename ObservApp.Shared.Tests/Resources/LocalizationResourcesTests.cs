using System.Xml.Linq;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Resources;

public sealed class LocalizationResourcesTests
{
	private static readonly string[] Languages = ["es", "en", "fr", "de", "it", "pt", "ru", "ar"];

	private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Resources = new(LoadAll);

	public static TheoryData<string> AllLanguages()
	{
		var data = new TheoryData<string>();
		foreach (var lang in Languages)
		{
			data.Add(lang);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(AllLanguages))]
	public void EveryLanguage_HasTheSameKeysAsSpanish(string lang)
	{
		var spanish = Resources.Value["es"].Keys.ToHashSet();
		var other = Resources.Value[lang].Keys.ToHashSet();

		Assert.Empty(spanish.Except(other));
		Assert.Empty(other.Except(spanish));
	}

	[Theory]
	[MemberData(nameof(AllLanguages))]
	public void EveryLanguage_HasNoEmptyValues(string lang)
	{
		var empty = Resources.Value[lang].Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key);

		Assert.Empty(empty);
	}

	[Fact]
	public void EclipseCatalog_RegionKeysExistInEveryLanguage()
	{
		var keys = new EclipseCalculatorService(new FixedTimeProvider(new DateTime(2020, 1, 1)))
			.GetUpcomingEclipses(years: 50)
			.Select(e => e.RegionKey)
			.Distinct()
			.ToList();

		Assert.NotEmpty(keys);
		foreach (var lang in Languages)
		{
			Assert.All(keys, k => Assert.True(Resources.Value[lang].ContainsKey(k), $"Falta {k} en App.{lang}.resx"));
		}
	}

	private static Dictionary<string, Dictionary<string, string>> LoadAll()
	{
		var folder = FindStringsFolder();
		return Languages.ToDictionary(
			lang => lang,
			lang => XDocument.Load(Path.Combine(folder, $"App.{lang}.resx"))
				.Root!.Elements("data")
				.ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty));
	}

	private static string FindStringsFolder()
	{
		for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			var candidate = Path.Combine(dir.FullName, "ObservApp.Shared", "Resources", "Strings");
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
		}

		throw new DirectoryNotFoundException("No se encontró ObservApp.Shared/Resources/Strings");
	}

	private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), TimeSpan.Zero);
	}
}
