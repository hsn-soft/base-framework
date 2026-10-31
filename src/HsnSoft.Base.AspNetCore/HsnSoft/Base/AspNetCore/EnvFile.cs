using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;

namespace HsnSoft.Base.AspNetCore;

/// <summary>A <c>.env</c> file: the keys a service takes from a file beside it, a developer's machine or a Compose volume, instead of the settings it ships with.</summary>
public static class EnvFile
{
    /// <summary>
    /// Adds the keys of a <c>.env</c> file to the configuration, below the environment variables (a variable of the platform always wins) and above the settings files. The file is looked for in the current folder, then in the folder of
    /// the application; there is no error when it is not there. A line is <c>KEY=value</c>, <c>export KEY=value</c> or a comment (<c>#</c>); a value may be in single or double quotes; <c>A__B</c> means <c>A:B</c>, as in a variable.
    /// </summary>
    /// <param name="configuration">The configuration being built.</param>
    /// <param name="path">The file; when null the usual places are searched.</param>
    /// <returns>The path of the file that was read, or null.</returns>
    public static string? AddBaseEnvFile(this IConfigurationBuilder configuration, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? file = path ?? FindFile();
        if (file is null || !File.Exists(file))
        {
            return null;
        }

        var source = new MemoryConfigurationSource { InitialData = Parse(File.ReadAllLines(file)) };
        int environment = configuration.Sources.ToList().FindIndex(item => item is EnvironmentVariablesConfigurationSource);
        if (environment >= 0)
        {
            configuration.Sources.Insert(environment, source);
        }
        else
        {
            configuration.Add(source);
        }

        return file;
    }

    /// <summary>The keys of the lines of a <c>.env</c> file (<c>__</c> turned into <c>:</c>); what is not a <c>KEY=value</c> line is skipped.</summary>
    public static IEnumerable<KeyValuePair<string, string?>> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line[7..].TrimStart();
            }

            int equals = line.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0)
            {
                continue;
            }

            string key = line[..equals].Trim().Replace("__", ":", StringComparison.Ordinal);
            string value = line[(equals + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
            {
                value = value[1..^1];
            }

            if (key.Length > 0)
            {
                yield return new KeyValuePair<string, string?>(key, value);
            }
        }
    }

    private static string? FindFile()
    {
        foreach (string folder in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            string candidate = Path.Combine(folder, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
