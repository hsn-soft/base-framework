using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.AspNetCore.Tests;

public class EnvFileTests
{
    [Fact]
    public void Parse_ReadsKeyValueLines_AndSkipsCommentsBlanksAndWhatIsNotAKeyValue()
    {
        var parsed = EnvFile.Parse(["# comment", "", "  A=1 ", "export B = two ", "C=\"quoted value\"", "D='single'", "E=", "no equals sign", "=novalue", "F=a=b"]).ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.Equal("1", parsed["A"]);
        Assert.Equal("two", parsed["B"]);
        Assert.Equal("quoted value", parsed["C"]);
        Assert.Equal("single", parsed["D"]);
        Assert.Equal("", parsed["E"]);
        Assert.Equal("a=b", parsed["F"]);
        Assert.Equal(6, parsed.Count);
    }

    [Fact]
    public void Parse_TurnsADoubleUnderscoreIntoALevelOfTheSettings()
    {
        KeyValuePair<string, string?> parsed = EnvFile.Parse(["Scheduler__ApiKey=abc"]).Single();

        Assert.Equal("Scheduler:ApiKey", parsed.Key);
    }

    [Fact]
    public void TheFile_OverridesTheSettingsFile_ButNotAnEnvironmentVariable()
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        string path = Path.Combine(folder, ".env");
        File.WriteAllText(path, "Env_File_Test_A=from-file\nEnv_File_Test_B=from-file\n");
        Environment.SetEnvironmentVariable("Env_File_Test_B", "from-variable");
        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string?> { ["Env_File_Test_A"] = "from-settings", ["Env_File_Test_B"] = "from-settings" });
            builder.AddEnvironmentVariables();

            string? read = builder.AddBaseEnvFile(path);
            IConfigurationRoot configuration = builder.Build();

            Assert.Equal(path, read);
            Assert.Equal("from-file", configuration["Env_File_Test_A"]);
            Assert.Equal("from-variable", configuration["Env_File_Test_B"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Env_File_Test_B", null);
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void AMissingFile_IsNotAnError()
    {
        var builder = new ConfigurationBuilder();

        Assert.Null(builder.AddBaseEnvFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), ".env")));
    }

    [Fact]
    public void WithoutAPath_TheFileOfTheCurrentFolderIsRead()
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        string previous = Directory.GetCurrentDirectory();
        File.WriteAllText(Path.Combine(folder, ".env"), "Env_File_Test_C=here\n");
        Directory.SetCurrentDirectory(folder);
        try
        {
            var builder = new ConfigurationBuilder();

            string? read = builder.AddBaseEnvFile();

            Assert.NotNull(read);
            Assert.Equal("here", builder.Build()["Env_File_Test_C"]);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EnvFile.AddBaseEnvFile(null!));
        Assert.Throws<ArgumentNullException>(() => EnvFile.Parse(null!).ToList());
    }
}
