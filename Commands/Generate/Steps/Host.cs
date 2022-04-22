// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Dox.Commands.Generate;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.Utils;

namespace Dox.StepsDox.Commands.Generate.Steps;

// ReSharper disable once ClassNeverInstantiated.Global
public class Host : StepBase
{
    public const string Key = "host";

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Host";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        if (Program.IsTeamCityAgent)
        {
            Output.LogLine("Hosting was skipped due to running in TeamCity");
            return;
        }

        string docfxJsonPath = Path.Combine(GenerateCommand.InputDirectory, ".docfx", "docfx.json");
        if (!File.Exists(docfxJsonPath))
        {
            Output.Error($"Unable to find required docfx.json at {docfxJsonPath}.", -1, true);
        }

        int execute = ChildProcess.Spawn(
            Path.Combine(DocFx.InstallPath, "docfx.exe"),
            DocFx.InstallPath,
            $"{docfxJsonPath}  --serve");

        if (execute == -1)
        {
            Output.Error("An error occured while building the documentation.", -1, true);
        }

        Console.WriteLine($"\nDocumentation should become available at:\n\nhttp://localhost:8080/\n\nYou will need to CTRL+Click that link, or navigate there in a browser.\nClosing the spawned window (PID:{execute}) will stop the hosting.");

        GenerateCommand.IsHosting = true;
    }
}