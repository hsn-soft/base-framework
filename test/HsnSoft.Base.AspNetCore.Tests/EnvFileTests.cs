using Microsoft.AspNetCore.Builder;
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
    public void InAWebApplication_TheFileStillOverridesTheSettingsFilesOfTheApplication_ButNotItsVariables()
    {
        // a web application builder has several sources of variables (the prefixed ones of the host come first, before appsettings.json): the file has to go below the last one
        string folder = Directory.CreateTempSubdirectory().FullName;
        string path = Path.Combine(folder, ".env");
        File.WriteAllText(Path.Combine(folder, "appsettings.json"), "{ \"EnvFileWeb\": { \"A\": \"from-settings\", \"B\": \"from-settings\", \"Empty\": \"\" } }");
        File.WriteAllText(path, "EnvFileWeb__A=from-file\nEnvFileWeb__B=from-file\nEnvFileWeb__Empty=from-file\n");
        Environment.SetEnvironmentVariable("EnvFileWeb__B", "from-variable");
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = folder });

            builder.Configuration.AddBaseEnvFile(path);

            Assert.Equal("from-file", builder.Configuration["EnvFileWeb:A"]);
            Assert.Equal("from-file", builder.Configuration["EnvFileWeb:Empty"]);   // an empty key of appsettings.json is a placeholder, and the file fills it
            Assert.Equal("from-variable", builder.Configuration["EnvFileWeb:B"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EnvFileWeb__B", null);
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
    public void WithoutAPath_TheFileOfTheCurrentFolderIsFound_ThenTheOneOfTheApplication()
    {
        string current = Directory.CreateTempSubdirectory().FullName;
        string application = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.Null(EnvFile.FindFile(current, application));

            File.WriteAllText(Path.Combine(application, ".env"), "A=1\n");
            Assert.Equal(Path.Combine(application, ".env"), EnvFile.FindFile(current, application));

            File.WriteAllText(Path.Combine(current, ".env"), "A=2\n");
            Assert.Equal(Path.Combine(current, ".env"), EnvFile.FindFile(current, application));
        }
        finally
        {
            Directory.Delete(current, true);
            Directory.Delete(application, true);
        }
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EnvFile.AddBaseEnvFile(null!));
        Assert.Throws<ArgumentNullException>(() => EnvFile.Parse(null!).ToList());
    }
}
