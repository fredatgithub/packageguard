﻿using CliWrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.ProjectModel;
using Pathy;

namespace PackageGuard.Core;

public class CSharpProjectAnalyzer(CSharpProjectScanner scanner, NuGetPackageAnalyzer analyzer)
{
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// A path to a folder containing a solution or project file. If it points to a solution, all the projects in that
    /// solution are included. If it points to a directory with more than one solution, it will fail.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// If specified, a list of packages, versions and licenses that are allowed. Everything else is forbidden.
    /// </summary>
    /// <remarks>
    /// Can be overridden by <see cref="DenyList"/>
    /// </remarks>
    public AllowList AllowList { get; set; } = new();

    /// <summary>
    /// If specified, a list of packages, versions and licenses that are forbidden, even if it was listed in <see cref="AllowList"/>.
    /// </summary>
    public DenyList DenyList { get; set; } = new();

    /// <summary>
    /// Specifies whether interactive mode should be enabled for the .NET restore process.
    /// When enabled, the restore operation may prompt for user input, such as authentication information.
    /// </summary>
    /// <value>
    /// Defaults to <c>true</c>.
    /// </value>
    public bool InteractiveRestore { get; set; } = true;

    /// <summary>
    /// Force restoring the NuGet dependencies, even if the lockfile is up-to-date
    /// </summary>
    public bool ForceRestore { get; set; } = false;

    public async Task<PolicyViolation[]> ExecuteAnalysis()
    {
        if (!AllowList.HasPolicies && !DenyList.HasPolicies)
        {
            throw new ArgumentException("Either a allowlist or a denylist must be specified");
        }

        List<string> projectPaths = scanner.FindProjects(ProjectPath);

        PackageInfoCollection packages = new();
        foreach (ChainablePath projectPath in projectPaths)
        {
            Logger.LogHeader($"Getting metadata for packages in {projectPath}");

            var lockFileLoader = new DotNetLockFileLoader
            {
                Logger = Logger,
                InteractiveRestore = InteractiveRestore,
                ForceRestore = ForceRestore
            };

            LockFile? lockFile = lockFileLoader.GetPackageLockFile(projectPath);
            if (lockFile is not null)
            {
                foreach (LockFileLibrary? library in lockFile.Libraries.Where(library => library.Type == "package"))
                {
                    await analyzer.CollectPackageMetadata(projectPath, library.Name, library.Version, packages);
                }
            }
        }

        return VerifyAgainstPolicy(packages);
    }

    private PolicyViolation[] VerifyAgainstPolicy(PackageInfoCollection packages)
    {
        var violations = new List<PolicyViolation>();

        foreach (PackageInfo package in packages)
        {
            if (!AllowList.Complies(package) || !DenyList.Complies(package))
            {
                violations.Add(new PolicyViolation(package.Id, package.Version, package.License!, package.Projects.ToArray()));
            }
        }

        return violations.ToArray();
    }
}
