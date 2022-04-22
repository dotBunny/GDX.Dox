// Copyright (c) 2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using Dox.Commands.Generate.Steps;
using Dox.Commands.Generate.Steps.Files;
using Dox.Commands.Generate.Steps.Reports;
using Dox.Commands.Generate.Steps.TeamCity;
using Dox.Commands.Generate.Steps.Templates;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.StepsDox.Commands.Generate.Steps;
using Dox.Utils;

namespace Dox;

public static class Config
{
    public const string DoxUri = "https://github.com/dotBunny/GDX.Documentation/";
    public const string DocFxUri = "https://dotnet.github.io/docfx/";

    public const string GitCommit = "https://github.com/dotBunny/GDX/commit/";

    /// <summary>
    ///     Relative to the
    /// </summary>
    public const string PackageFolder = "Packages";

    public static readonly string ShortDate = DateTime.Now.ToString("yyyy-MM-dd");

    public static readonly string[] AllSteps =
    {
        DocFx.Key, ApiPort.Key, Clean.Key, Changelog.Key, SecurityPolicy.Key, CodeOfConduct.Key, License.Key,
        Footer.Key, XmlDocs.Key, Metadata.Key, TableOfContents.Key, Portability.Key,
        CodeInspection.Key, Duplicates.Key, CodeCoverage.Key, Build.Key, Host.Key
    };

    public static readonly string CleanBuildSteps = AllSteps.Concatenate(",");



    public static string Steps = CleanBuildSteps;

}