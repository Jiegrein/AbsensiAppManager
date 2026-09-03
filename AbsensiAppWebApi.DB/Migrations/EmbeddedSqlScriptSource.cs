using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AbsensiAppWebApi.DB.Migrations;

/// <summary>
/// Reads the numbered <c>Migrations/*.sql</c> files embedded in this assembly and returns them
/// ordered by their leading number ("1. Initial.sql", "2. Seeding.sql", ...).
/// </summary>
public sealed class EmbeddedSqlScriptSource : ISqlScriptSource
{
    private const string ResourceSuffix = ".sql";
    private const string ResourceFolderMarker = ".Migrations.";
    private static readonly Regex LeadingNumber = new(@"^(\d+)\.", RegexOptions.Compiled);
    private static readonly Assembly ScriptAssembly = typeof(EmbeddedSqlScriptSource).Assembly;

    public IReadOnlyList<SqlScript> GetScripts()
    {
        return ScriptAssembly.GetManifestResourceNames()
            .Where(resource => resource.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            .Select(resource => new { Resource = resource, Name = ToScriptName(resource) })
            .OrderBy(script => Order(script.Name))
            .ThenBy(script => script.Name, StringComparer.OrdinalIgnoreCase)
            .Select(script => new SqlScript(script.Name, ReadResource(script.Resource)))
            .ToList();
    }

    // Resource names look like "AbsensiAppWebApi.DB.Migrations.1. Initial.sql"; keep the file name only.
    private static string ToScriptName(string resource)
    {
        var index = resource.IndexOf(ResourceFolderMarker, StringComparison.Ordinal);
        return index < 0 ? resource : resource[(index + ResourceFolderMarker.Length)..];
    }

    private static int Order(string scriptName)
    {
        var match = LeadingNumber.Match(scriptName);
        return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
    }

    private static string ReadResource(string resource)
    {
        using var stream = ScriptAssembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded SQL script '{resource}' could not be opened.");
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
