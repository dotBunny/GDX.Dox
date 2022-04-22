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
    public static string OutputDirectory = GetDefaultOutputFolder();

    public Build()
    {
        Program.Args.RegisterHelp("Build Step", $"{OutputKey} <value>",
            "\t\tPath to copy the generated output too.");
    }


    public static string GetDefaultOutputFolder()
    {
        return Path.Combine(GenerateCommand.InputDirectory, ".docfx", "_site");
    }

    /// <inheritdoc />
    public override void Clean()
    {
        if (Directory.Exists(OutputDirectory))
        {
            Output.LogLine("Deleting previous output folder ...");
            Directory.Delete(OutputDirectory, true);
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
        // Process arguments
        Program.GetParameter(OutputKey, GetDefaultOutputFolder(), out OutputDirectory,
            s =>
            {
                if (Path.IsPathFullyQualified(s))
                {
                    return s;
                }

                return Path.GetFullPath(Path.Combine(Program.ProcessDirectory, s));
            });
        Output.Value("Build.OutputDirectory", OutputDirectory);

        // Check for DocFX json
        string docfxJsonPath = Path.Combine(GenerateCommand.InputDirectory, ".docfx", "docfx.json");
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

        // We need to move this somewhere
        if (OutputDirectory != GetDefaultOutputFolder())
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            Output.LogLine($"Copying output to destination {OutputDirectory}.");
            Platform.CopyFilesRecursively(GetDefaultOutputFolder(), OutputDirectory);
        }
    }
}