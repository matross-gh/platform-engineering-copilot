using System.Text.RegularExpressions;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase;

/// <summary>
/// Parameter declaration parsed from a Bicep module's source.
/// </summary>
public class BicepModuleParameterInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public string? DefaultValue { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Output declaration parsed from a Bicep module's source.
/// </summary>
public class BicepModuleOutputInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Shared regex-based parser for the self-documenting Bicep module convention used by the
/// GitHub-backed module registry (metadata moduleinfo block, @description/@allowed/@secure
/// decorators). Used by both the module explainer tool and the ACR deployment tool so the
/// parsing rules only need to be verified correct in one place.
/// </summary>
public static class BicepModuleMetadataParser
{
    // Matches "metadata moduleinfo = { key: 'value' ... }" blocks.
    private static readonly Regex MetadataBlockRegex = new(
        @"metadata\s+moduleinfo\s*=\s*\{(?<body>[\s\S]*?)\n\}",
        RegexOptions.Compiled);

    private static readonly Regex MetadataKvRegex = new(
        @"(?<key>\w+)\s*:\s*(?:'''(?<val3>[\s\S]*?)'''|'(?<val1>[^']*)')",
        RegexOptions.Compiled);

    // Matches optional @description/@allowed/@secure decorators (in any order) above a param declaration.
    private static readonly Regex ParamRegex = new(
        @"(?:@description\(\s*(?:'''(?<desc3>[\s\S]*?)'''|'(?<desc1>[^']*)')\s*\)\s*\r?\n\s*|@allowed\(\s*\[(?<allowed>[\s\S]*?)\]\s*\)\s*\r?\n\s*|@secure\(\)\s*\r?\n\s*)*param\s+(?<name>\w+)\s+(?<type>[\w<>\[\]\.]+)(?:\s*=\s*(?<default>[^\r\n]+))?",
        RegexOptions.Compiled);

    private static readonly Regex OutputRegex = new(
        @"(?:@description\(\s*(?:'''(?<desc3>[\s\S]*?)'''|'(?<desc1>[^']*)')\s*\)\s*\r?\n\s*)?output\s+(?<name>\w+)\s+(?<type>[\w<>\[\]\.]+)\s*=",
        RegexOptions.Compiled);

    public static Dictionary<string, string> ParseModuleInfo(string source)
    {
        var result = new Dictionary<string, string>();
        var blockMatch = MetadataBlockRegex.Match(source);
        if (!blockMatch.Success)
        {
            return result;
        }

        foreach (Match kv in MetadataKvRegex.Matches(blockMatch.Groups["body"].Value))
        {
            var key = kv.Groups["key"].Value;
            var value = kv.Groups["val3"].Success ? kv.Groups["val3"].Value.Trim() : kv.Groups["val1"].Value;
            result[key] = value;
        }

        return result;
    }

    public static List<BicepModuleParameterInfo> ParseParameters(string source)
    {
        var results = new List<BicepModuleParameterInfo>();

        foreach (Match match in ParamRegex.Matches(source))
        {
            var descriptions = CollectCaptures(match, "desc3", "desc1");
            var allowedRaw = match.Groups["allowed"].Success
                ? match.Groups["allowed"].Captures.Select(c => c.Value).ToList()
                : new List<string>();

            var allowedValues = allowedRaw
                .SelectMany(a => a.Split('\n'))
                .Select(v => v.Trim().Trim(',').Trim('\''))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var defaultValue = match.Groups["default"].Success ? match.Groups["default"].Value.Trim() : null;

            results.Add(new BicepModuleParameterInfo
            {
                Name = match.Groups["name"].Value,
                Type = match.Groups["type"].Value,
                Required = defaultValue == null,
                DefaultValue = defaultValue,
                AllowedValues = allowedValues.Count > 0 ? allowedValues : null,
                Description = descriptions.Count > 0 ? string.Join(" ", descriptions).Trim() : null
            });
        }

        return results;
    }

    public static List<BicepModuleOutputInfo> ParseOutputs(string source)
    {
        var results = new List<BicepModuleOutputInfo>();

        foreach (Match match in OutputRegex.Matches(source))
        {
            var descriptions = CollectCaptures(match, "desc3", "desc1");

            results.Add(new BicepModuleOutputInfo
            {
                Name = match.Groups["name"].Value,
                Type = match.Groups["type"].Value,
                Description = descriptions.Count > 0 ? string.Join(" ", descriptions).Trim() : null
            });
        }

        return results;
    }

    /// <summary>
    /// Best-effort ACR OCI module reference ("br:registry/path:tag"), derived from the module's
    /// 'parent'/'version' metadata and its file name, per the registry's naming convention.
    /// Returns null if the metadata doesn't have enough information (e.g. no 'parent'/'version').
    /// </summary>
    public static string? ComputeAcrModuleReference(IReadOnlyDictionary<string, string> moduleInfo, string filePath, string? acrRegistry)
    {
        if (!moduleInfo.TryGetValue("parent", out var parent) || !moduleInfo.TryGetValue("version", out var version))
        {
            return null;
        }

        var moduleId = $"{parent.ToLowerInvariant()}/{Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant()}";
        return string.IsNullOrEmpty(acrRegistry) ? $"{moduleId}:{version}" : $"br:{acrRegistry}/{moduleId}:{version}";
    }

    /// <summary>
    /// Collects captures for two alternate groups (e.g. triple-quoted vs single-quoted string variants)
    /// in the order they occurred, since a decorator only matches one alternative per occurrence.
    /// </summary>
    private static List<string> CollectCaptures(Match match, params string[] groupNames)
    {
        return groupNames
            .SelectMany(g => match.Groups[g].Captures.Cast<Capture>())
            .Select(c => c.Value.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }
}
