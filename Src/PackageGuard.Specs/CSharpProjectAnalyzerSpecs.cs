﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageGuard.Core;
using Pathy;

namespace PackageGuard.Specs;

[TestClass]
public class CSharpProjectAnalyzerSpecs
{
    private readonly CSharpProjectScanner cSharpProjectScanner = new(NullLogger.Instance);

    private readonly NuGetPackageAnalyzer
        nuGetPackageAnalyzer = new(NullLogger.Instance, new LicenseFetcher(NullLogger.Instance));

    private ChainablePath ProjectPath =>
        Assembly.GetExecutingAssembly().Location.ToPath().Directory / ".." / ".." / ".." / "PackageGuard.Specs.csproj";

    [TestMethod]
    public async Task Either_a_denylist_or_a_allowlist_is_required()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
            };

        // Act
        var act = async () => await analyzer.ExecuteAnalysis();

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Either*allowlist*denylist*");
    }

    [TestMethod]
    public async Task Can_deny_an_entire_package()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                DenyList = new DenyList
                {
                    Packages =
                    [
                        new PackageSelector("FluentAssertions")
                    ]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().ContainEquivalentOf(new
        {
            PackageId = "FluentAssertions",
            Version = "8.2.0",
            License = "Unknown"
        });
    }

    [TestMethod]
    public async Task Can_deny_a_specific_version()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                DenyList = new DenyList
                {
                    Packages =
                    [
                        new PackageSelector("FluentAssertions", "8.2.0")
                    ]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().ContainEquivalentOf(new
        {
            PackageId = "FluentAssertions",
            Version = "8.2.0",
            License = "Unknown"
        });
    }

    [TestMethod]
    public async Task Does_not_deny_a_version_if_the_range_does_not_match()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                DenyList = new DenyList
                {
                    Packages =
                    [
                        new PackageSelector("FluentAssertions", "[7.0.0,8.0.0)"),
                    ]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Can_deny_a_version_based_on_a_range()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                DenyList = new DenyList
                {
                    Packages =
                    [
                        new PackageSelector("FluentAssertions", "[8.0.0,9.0.0)"),
                    ]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().ContainEquivalentOf(new
        {
            PackageId = "FluentAssertions",
            Version = "8.2.0",
            License = "Unknown"
        });
    }

    [TestMethod]
    public async Task Can_denyist_a_license()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                DenyList = new DenyList
                {
                    Licenses = ["mit"]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Select(x => x.PackageId).Should().Contain(["CliWrap", "coverlet.collector", "JetBrains.Annotations"]);
        violations.Should().NotContainEquivalentOf(new
        {
            PackageId = "FluentAssertions"
        });
    }

    [TestMethod]
    public async Task Can_allow_a_license()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                AllowList = new AllowList
                {
                    Licenses = ["mit"]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().ContainEquivalentOf(new
        {
            PackageId = "FluentAssertions"
        });
    }

    [TestMethod]
    public async Task Can_allow_an_unknown_license()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                AllowList = new AllowList
                {
                    Licenses = ["mit", "apache-2.0", "unknown"]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Denying_a_license_overrides_an_allowed_license()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                AllowList =
                {
                    Licenses = ["mit"]
                },
                DenyList = new DenyList
                {
                    Licenses = ["mit"]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Select(x => x.PackageId).Should().Contain(["CliWrap", "coverlet.collector", "JetBrains.Annotations"]);
    }

    [TestMethod]
    public async Task A_package_version_outside_the_allowed_range_is_a_violation()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                AllowList = new AllowList
                {
                     Packages =
                    [
                        new PackageSelector("FluentAssertions", "[7.0.0,8.0.0)"),
                    ]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().ContainEquivalentOf(new
        {
            PackageId = "FluentAssertions",
            Version = "8.2.0",
            License = "Unknown"
        });
    }

    [TestMethod]
    public async Task A_package_version_inside_the_allowed_range_is_okay()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                AllowList = new AllowList
                {
                    Packages =
                    [
                        new PackageSelector("FluentAssertions", "[8.0.0,9.0.0)"),
                    ]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Can_allow_a_package_that_violates_the_allowed_licenses()
    {
        // Arrange
        var analyzer =
            new CSharpProjectAnalyzer(cSharpProjectScanner, nuGetPackageAnalyzer)
            {
                ProjectPath = ProjectPath,
                AllowList = new AllowList
                {
                    Licenses = ["mit", "apache-2.0"],
                    Packages = [new PackageSelector("FluentAssertions")]
                }
            };

        // Act
        var violations = await analyzer.ExecuteAnalysis();

        // Assert
        violations.Should().BeEmpty();
    }
}
