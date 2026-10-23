// Formats JSON files the way the .editorconfig asks (2 spaces, LF, one final newline). Property order and number text are kept.
// Runs with only the .NET SDK, so it is the same on every platform:
//
//   dotnet run scripts/check-json-format.cs -- <file>...          rewrite the given files
//   dotnet run scripts/check-json-format.cs -- --check <file>...  only report files that would change (exit code 1)
//   dotnet run scripts/check-json-format.cs -- --all [--check]    every tracked *.json (.vscode/ is skipped: it allows comments)
//
// A file with comments or other non-standard JSON is reported as invalid and left alone.
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

bool check = args.Contains("--check");
List<string> files = args.Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToList();

if (args.Contains("--all"))
{
    ProcessStartInfo gitList = new("git", ["ls-files", "*.json"]) { RedirectStandardOutput = true };
    using Process git = Process.Start(gitList)!;
    files.AddRange(git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(path => !path.StartsWith(".vscode/", StringComparison.Ordinal)));
    git.WaitForExit();
}

JsonWriterOptions writerOptions = new()
{
    Indented = true,
    IndentSize = 2,
    NewLine = "\n",
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};

int changed = 0;
int invalid = 0;
foreach (string file in files.Where(File.Exists))
{
    string original = File.ReadAllText(file);
    string formatted;
    try
    {
        using JsonDocument document = JsonDocument.Parse(original);
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, writerOptions))
        {
            document.WriteTo(writer);
        }

        formatted = Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }
    catch (JsonException exception)
    {
        Console.Error.WriteLine($"invalid JSON, not touched: {file} ({exception.Message})");
        invalid++;
        continue;
    }

    if (formatted == original)
    {
        continue;
    }

    changed++;
    if (check)
    {
        Console.WriteLine($"needs formatting: {file}");
    }
    else
    {
        File.WriteAllText(file, formatted, new UTF8Encoding(false));
        Console.WriteLine($"formatted: {file}");
    }
}

Console.WriteLine($"{files.Count} file(s) looked at, {changed} {(check ? "need formatting" : "formatted")}, {invalid} invalid");
return invalid > 0 || (check && changed > 0) ? 1 : 0;
