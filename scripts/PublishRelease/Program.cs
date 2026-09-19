using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var options = ReleasePublishOptions.Parse(args);
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    throw new InvalidOperationException("GITHUB_TOKEN is not set.");
}

if (!File.Exists(options.AssetPath))
{
    throw new FileNotFoundException($"Asset not found: {options.AssetPath}", options.AssetPath);
}

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHub-Copilot", "1.0"));

var release = await GetOrCreateReleaseAsync(httpClient, options);
var assetName = Path.GetFileName(options.AssetPath);

foreach (var asset in release.Assets.Where(asset => string.Equals(asset.Name, assetName, StringComparison.OrdinalIgnoreCase)))
{
    using var deleteResponse = await httpClient.DeleteAsync($"https://api.github.com/repos/{options.Repository}/releases/assets/{asset.Id}");
    deleteResponse.EnsureSuccessStatusCode();
}

var uploadUri = new Uri(GetUploadUrl(release.UploadUrl, assetName));
await using var assetStream = File.OpenRead(options.AssetPath);
using var assetContent = new StreamContent(assetStream);
assetContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(options.AssetPath));
using var uploadResponse = await httpClient.PostAsync(uploadUri, assetContent);
uploadResponse.EnsureSuccessStatusCode();

Console.WriteLine($"RELEASE_OK https://github.com/{options.Repository}/releases/tag/{options.Tag}");

static async Task<GitHubRelease> GetOrCreateReleaseAsync(HttpClient httpClient, ReleasePublishOptions options)
{
    using var getResponse = await httpClient.GetAsync($"https://api.github.com/repos/{options.Repository}/releases/tags/{options.Tag}");
    if (getResponse.StatusCode != HttpStatusCode.NotFound)
    {
        getResponse.EnsureSuccessStatusCode();
        return await ReadJsonAsync<GitHubRelease>(getResponse);
    }

    var payload = new CreateReleaseRequest(
        options.Tag,
        "main",
        options.ReleaseName,
        options.ReleaseNotes,
        false,
        false);

    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    using var createResponse = await httpClient.PostAsync($"https://api.github.com/repos/{options.Repository}/releases", content);
    createResponse.EnsureSuccessStatusCode();
    return await ReadJsonAsync<GitHubRelease>(createResponse);
}

static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
{
    await using var responseStream = await response.Content.ReadAsStreamAsync();
    var value = await JsonSerializer.DeserializeAsync<T>(responseStream, JsonOptions.Default);
    return value ?? throw new InvalidOperationException("GitHub API returned an empty response.");
}

static string GetUploadUrl(string uploadUrlTemplate, string assetName)
{
    var baseUrl = uploadUrlTemplate.Split('{', 2)[0];
    return $"{baseUrl}?name={Uri.EscapeDataString(assetName)}";
}

static string GetContentType(string assetPath)
{
    return string.Equals(Path.GetExtension(assetPath), ".zip", StringComparison.OrdinalIgnoreCase)
        ? "application/zip"
        : "application/octet-stream";
}

internal sealed record ReleasePublishOptions(
    string Repository,
    string Tag,
    string ReleaseName,
    string AssetPath,
    string ReleaseNotes)
{
    public static ReleasePublishOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{argument}'.", nameof(args));
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for argument '{argument}'.", nameof(args));
            }

            values[argument[2..]] = args[++index];
        }

        return new ReleasePublishOptions(
            GetRequired(values, "repository"),
            GetRequired(values, "tag"),
            GetRequired(values, "release-name"),
            Path.GetFullPath(GetRequired(values, "asset-path")),
            GetRequired(values, "release-notes"));
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> values, string key)
    {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new ArgumentException($"Missing required argument '--{key}'.");
    }
}

internal sealed record GitHubRelease(long Id, string UploadUrl, GitHubAsset[] Assets);

internal sealed record GitHubAsset(long Id, string Name);

internal sealed record CreateReleaseRequest(
    string Tag_Name,
    string Target_Commitish,
    string Name,
    string Body,
    bool Draft,
    bool Prerelease);

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}