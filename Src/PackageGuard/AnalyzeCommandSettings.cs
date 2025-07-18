using System.ComponentModel;
using JetBrains.Annotations;
using Pathy;
using Spectre.Console.Cli;

namespace PackageGuard;

[UsedImplicitly]
internal class AnalyzeCommandSettings : CommandSettings
{
    [Description(
        "The path to a directory containing a .sln file, a specific .sln file, or a specific .csproj file. Defaults to the current working directory")]
    [CommandArgument(0, "[path]")]
    public string ProjectPath { get; set; } = string.Empty;

    [Description("The path to the configuration file. Defaults to the config.json in the current working directory.")]
    [CommandOption("-c|--config-path|--configPath")]
    public string ConfigPath { get;  set; } = "config.json";

    [Description("Allow enabling or disabling an interactive mode of \"dotnet restore\". Defaults to true")]
    [CommandOption("-i|--restore-interactive|--restoreinteractive")]
    public bool Interactive {get; set;} = true;

    [Description("Force restoring the NuGet dependencies, even if the lockfile is up-to-date")]
    [CommandOption("-f|--force-restore|--forcerestore")]
    public bool ForceRestore { get; set; } = false;

    [Description("Prevent the restore operation from running, even if the lock file is missing or out-of-date")]
    [CommandOption("-s|--skip-restore|--skiprestore")]
    public bool SkipRestore { get; set; } = false;

    [Description("GitHub API key to use for fetching package licenses. If not specified, you may run into GitHub's rate limiting issues.")]
    [CommandOption("-a|--github-api-key|--githubapikey")]
    public string? GitHubApiKey { get; set; } = Environment.GetEnvironmentVariable("GITHUB_API_KEY");

    [Description("Maintains a cache of the package information to speed up future analysis.")]
    [CommandOption("--use-caching|--usecaching")]
    public bool UseCaching { get; set; } = false;

    [Description("Overrides the file path where analysis data is cached. Defaults to the \"<workingdirectory>/.packageguard/cache.bin\"")]
    [CommandOption("--cache-file-path|--cachefilepath")]
    public string CacheFilePath { get; set; } = ChainablePath.Current / ".packageguard" / "cache.bin";

}
