// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps;

// ReSharper disable once ClassNeverInstantiated.Global
public class Build : StepBase
{
    public const string Key = "build";
    public const string OutputKey = "output";

    public Build()
    {
        Program.Args.RegisterHelp("Build Step", $"{OutputKey} <value>",
            "\t\tPath to copy the generated output too.");
    }


    public static string GetOutputFolder()
    {
        return Path.Combine(Program.InputDirectory, ".docfx", "_site");
    }

    /// <inheritdoc />
    public override void Clean()
    {
        if (Directory.Exists(GetOutputFolder()))
        {
            Output.LogLine("Deleting previous output folder ...");
            Directory.Delete(GetOutputFolder(), true);
        }
    }

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Build Documentation";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        // Check for DocFX json
        string docfxJsonPath = Path.Combine(Program.InputDirectory, ".docfx", "docfx.json");
        if (!File.Exists(docfxJsonPath))
        {
            Output.Error($"Unable to find required docfx.json at {docfxJsonPath}.", -1, true);
        }

        // Execute call to DocFX
        bool execute = ChildProcess.WaitFor(
            Path.Combine(DocFx.InstallPath, "docfx.exe"),
            DocFx.InstallPath,
            $"{docfxJsonPath} --build");

        if (!execute)
        {
            Output.Error("An error occured while building the documentation.", -1, true);
        }
    }
}