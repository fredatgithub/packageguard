﻿using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using PackageGuard.Core;
using Pathy;
using PublicApiGenerator;
using VerifyTests;
using VerifyTests.DiffPlex;
using VerifyXunit;
using Xunit;

namespace PackageGuard.ApiVerificationTests;

public class ApiApproval
{
    private static readonly ChainablePath SourcePath = ChainablePath.Current / ".." / ".." / ".." / "..";

    static ApiApproval()
    {
        VerifyDiffPlex.Initialize(OutputType.Minimal);
    }

    [Fact]
    public Task ApproveApi()
    {
        var assembly = typeof(LicenseFetcher).Assembly;
        var publicApi = assembly.GeneratePublicApi(options: null);

        return Verifier
            .Verify(publicApi)
            .ScrubLinesContaining("FrameworkDisplayName")
            .UseDirectory("ApprovedApi")
            .UseFileName("net8.0")
            .DisableDiff();
    }
}
