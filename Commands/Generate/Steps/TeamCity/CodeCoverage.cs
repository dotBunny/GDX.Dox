﻿// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps.TeamCity;

public class CodeCoverage : StepBase
{
    //Staging\ResharperInspection.xml
    public const string Key = "code-coverage";

    static string GetPath()
    {
        return Path.Combine(Program.InputDirectory, ".docfx", "reports", "coverage");
    }

    /// <inheritdoc />
    public override void Clean()
    {
        string folder = GetPath();
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Output.LogLine("Removed previous coverage report.");
        }
    }

    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Code Coverage";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        if (!Program.IsTeamCityAgent)
        {

            string stubFolder = Path.Combine(GetPath(), "Report");
            if (!Directory.Exists(stubFolder))
            {
                Directory.CreateDirectory(stubFolder);
            }
            File.WriteAllText(Path.Combine(stubFolder, "index.html"),
                "This is a stub for actual content generated by CI/CD");

            Output.LogLine("Skipping w/ stub created. Not running inside of CI/CD.");
            return;
        }

        string teamCityArtifact =
            Path.GetFullPath(Path.Combine(Program.ProcessDirectory, "..", "..", "..", "..", "Staging", "CodeCoverage"));

        if (Directory.Exists(teamCityArtifact))
        {
            if (!Directory.Exists(GetPath()))
            {
                Directory.CreateDirectory(GetPath());
            }

            Platform.CopyFilesRecursively(teamCityArtifact, GetPath());
        }
        else
        {
            Output.Error($"Unable to find code coverage artifacts at {teamCityArtifact}.", -1, true);
        }
    }
}