using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core;

namespace WinCarePro.Infrastructure.Services;

public sealed record AppUpdateManifest(
    string VersionString,
    string DownloadUrl,
    string ExpectedSha256,
    string Changelog,
    string ReleaseNotes,
    bool HasUpdate);

/// <summary>
/// Centralized self-update manifest fetcher and parser shared by MainWindow and SettingsPage.
/// </summary>
public static class AppUpdateService
{
    public const string ManifestBaseUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";

    public static async Task<string> FetchManifestJsonAsync(HttpClient httpClient, CancellationToken ct = default)
    {
        string url = $"{ManifestBaseUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true
        };
        using var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public static AppUpdateManifest ParseUpdateManifest(string json, bool betaEnabled, Version currentVersion)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string versionStr;
        string downloadUrl;
        string expectedSha256;
        string changelog;
        string releaseNotes = root.TryGetProperty("releaseNotes", out var rnProp) ? rnProp.GetString() ?? "" : "";

        if (betaEnabled && root.TryGetProperty("beta_version", out var betaVerProp))
        {
            versionStr = betaVerProp.GetString() ?? AppConstants.DefaultVersionString;
            downloadUrl = (root.TryGetProperty("beta_downloadUrl", out var bdlProp) ? bdlProp.GetString() : null)
                ?? (root.TryGetProperty("beta_url", out var buProp) ? buProp.GetString() : "") ?? "";
            expectedSha256 = root.TryGetProperty("beta_sha256", out var bshaProp) ? bshaProp.GetString() ?? "" : "";
            changelog = root.TryGetProperty("beta_changelog", out var bclProp) ? bclProp.GetString() ?? "" : "";
        }
        else
        {
            versionStr = root.TryGetProperty("version", out var verProp) ? verProp.GetString() ?? AppConstants.DefaultVersionString : AppConstants.DefaultVersionString;
            downloadUrl = (root.TryGetProperty("downloadUrl", out var dlProp) ? dlProp.GetString() : null)
                ?? (root.TryGetProperty("url", out var uProp) ? uProp.GetString() : "") ?? "";
            expectedSha256 = root.TryGetProperty("sha256", out var shaProp) ? shaProp.GetString() ?? "" : "";
            changelog = root.TryGetProperty("changelog", out var clProp) ? clProp.GetString() ?? "" : "";
        }

        string cleanRemote = Regex.Replace(versionStr.Split('-')[0], @"[^\d\.]", "").TrimEnd('.');
        bool hasUpdate = Version.TryParse(cleanRemote, out var remoteVersion)
            ? remoteVersion > currentVersion
            : !string.Equals(versionStr.Trim(), currentVersion.ToString(), StringComparison.OrdinalIgnoreCase);

        return new AppUpdateManifest(versionStr, downloadUrl, expectedSha256, changelog, releaseNotes, hasUpdate);
    }
}
