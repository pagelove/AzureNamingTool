using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace AzureNamingTool.UnitTests;

public class PageloveNamingProfileTests
{
    [Fact]
    public void RepositoryConfiguration_MatchesPageloveNamingStandard()
    {
        using var components = LoadJson("resourcecomponents.json");
        using var delimiters = LoadJson("resourcedelimiters.json");
        using var services = LoadJson("resourceprojappsvcs.json");
        using var functions = LoadJson("resourcefunctions.json");
        using var environments = LoadJson("resourceenvironments.json");
        using var locations = LoadJson("resourcelocations.json");
        using var resourceTypes = LoadJson("resourcetypes.json");

        EnabledComponents(components).Should().Equal(
            "ResourceType",
            "ResourceProjAppSvc",
            "ResourceFunction",
            "ResourceEnvironment",
            "ResourceLocation",
            "ResourceInstance");

        EnabledDelimiters(delimiters).Should().Equal("-");
        ShortNames(services).Should().Equal("cloud", "marketing", "acme", "ukgovcloud");
        ShortNames(functions).Should().Equal("domhttp", "domdav", "dom", "fdb", "vk", "edge", "dns", "artifacts", "naming");
        ShortNames(environments).Should().Equal("prod", "canary", "dev");
        ShortNames(locations).Should().Equal("cus", "eus2", "global");

        ResourceType(resourceTypes, "Resources/resourcegroups").GetProperty("exclude").GetString().Should().Be("Org");
        ResourceType(resourceTypes, "Resources/resourcegroups").GetProperty("optional").GetString().Should().Contain("Function");
        ResourceType(resourceTypes, "Web/serverfarms").GetProperty("ShortName").GetString().Should().Be("plan");
        ResourceType(resourceTypes, "Web/serverfarms").GetProperty("exclude").GetString().Should().Be("Org");
        ResourceType(resourceTypes, "Web/sites", "Web App").GetProperty("ShortName").GetString().Should().Be("app");
        ResourceType(resourceTypes, "Web/sites", "Web App").GetProperty("exclude").GetString().Should().Be("Org");
        AssertResourceTypeOverride(resourceTypes, "Cdn/profiles", "", "afd", "Org", "Function");
        AssertResourceTypeOverride(resourceTypes, "Cdn/profiles/endpoints", "", "afde", "Org", "Function");
        AssertResourceTypeOverride(resourceTypes, "Network/frontDoors", "", "afd", "Org", "Function");
        AssertResourceTypeOverride(resourceTypes, "KeyVault/vaults", "", "kv", "Org", "Function");
        AssertResourceTypeOverride(resourceTypes, "Network/virtualNetworks", "", "vnet", "Org", "Function");
    }

    private static JsonDocument LoadJson(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "repository", fileName);
            if (File.Exists(candidate))
            {
                return JsonDocument.Parse(File.ReadAllText(candidate));
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find src/repository/{fileName}");
    }

    private static IEnumerable<string> EnabledComponents(JsonDocument document)
    {
        return document.RootElement
            .EnumerateArray()
            .Where(item => item.GetProperty("enabled").GetBoolean())
            .OrderBy(item => item.GetProperty("sortOrder").GetInt32())
            .Select(item => item.GetProperty("name").GetString()!);
    }

    private static IEnumerable<string> EnabledDelimiters(JsonDocument document)
    {
        return document.RootElement
            .EnumerateArray()
            .Where(item => item.GetProperty("enabled").GetBoolean())
            .OrderBy(item => item.GetProperty("sortOrder").GetInt32())
            .Select(item => item.GetProperty("delimiter").GetString()!);
    }

    private static IEnumerable<string> ShortNames(JsonDocument document)
    {
        return document.RootElement
            .EnumerateArray()
            .OrderBy(item => item.TryGetProperty("sortOrder", out var sortOrder) ? sortOrder.GetInt32() : item.GetProperty("id").GetInt32())
            .Select(item => item.GetProperty("shortName").GetString()!);
    }

    private static JsonElement ResourceType(JsonDocument document, string resource, string property = "")
    {
        return document.RootElement
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("resource").GetString() == resource &&
                item.GetProperty("property").GetString() == property);
    }

    private static void AssertResourceTypeOverride(
        JsonDocument document,
        string resource,
        string property,
        string shortName,
        string exclude,
        string optionalContains)
    {
        var resourceType = ResourceType(document, resource, property);

        resourceType.GetProperty("ShortName").GetString().Should().Be(shortName);
        resourceType.GetProperty("exclude").GetString().Should().Be(exclude);
        resourceType.GetProperty("optional").GetString().Should().Contain(optionalContains);
    }
}
