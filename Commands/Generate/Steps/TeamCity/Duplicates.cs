﻿// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps.TeamCity;

// ReSharper disable once UnusedType.Global
public class Duplicates : StepBase
{
    // ResharperDuplicates.xml
    public const string Key = "code-duplication";
    const string FileName = "duplicates.xml";

    static string GetPath()
    {
        return Path.Combine(Program.InputDirectory, ".docfx", "reports", "duplicates");
    }

    /// <inheritdoc />
    public override void Clean()
    {
        string folder = GetPath();
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Output.LogLine("Removed previous inspection report.");
        }
    }

    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Code Duplication";
    }

    public override void Execute()
    {
        if (!Program.IsTeamCityAgent)
        {
            string stubFolder = GetPath() ?? Program.ProcessDirectory;
            if (!Directory.Exists(stubFolder))
            {
                Directory.CreateDirectory(stubFolder);
            }
            File.WriteAllText(Path.Combine(stubFolder, FileName),
                "This is a stub for actual content generated by CI/CD");

            Output.LogLine("Skipping. Not running inside of CI/CD.");
            return;
        }

        string teamCityArtifact =
            Path.GetFullPath(Path.Combine(Program.ProcessDirectory, "..", "..", "..", "..", "Staging",
                "ResharperDuplicates.xml"));
        if (File.Exists(teamCityArtifact))
        {
            Output.LogLine("Copying code duplication artifacts.");
            if (!Directory.Exists(GetPath()))
            {
                Directory.CreateDirectory(GetPath());
            }

            File.Copy(teamCityArtifact, Path.Combine(GetPath(), FileName));
        }
        else
        {
            Output.Warning($"Unable to find code duplication artifacts at {teamCityArtifact}.");
        }
    }
}