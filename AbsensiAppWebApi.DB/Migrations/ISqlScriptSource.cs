using System.Collections.Generic;

namespace AbsensiAppWebApi.DB.Migrations;

/// <summary>A single SQL migration script, identified by its file name.</summary>
public sealed record SqlScript(string Name, string Sql);

/// <summary>Supplies migration scripts in the order they must be applied.</summary>
public interface ISqlScriptSource
{
    IReadOnlyList<SqlScript> GetScripts();
}
