// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps.Reports;

// ReSharper disable once ClassNeverInstantiated.Global
public class Portability : StepBase
{
    public const string Key = "portability";

    public override string[] GetRequiredStepIdentifiers()
    {
        // Generates project
        return new[] { XmlDocs.Key };
    }

    /// <inheritdoc />
    public override void Clean()
    {
        string folder = GetPath();
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Output.LogLine("Removed previous portability report.");
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
        return "API Portability";
    }

    static string GetPath()
    {
        return Path.Combine(GenerateCommand.InputDirectory, ".docfx", "reports", "portability");
    }

    string GetJsonPath()
    {
        return Path.Combine(GetPath(), "portability.json");
    }

    string GetHtmlPath()
    {
        return Path.Combine(GetPath(), "index.html");
    }

    string GetDgmlPath()
    {
        return Path.Combine(GetPath(), "portability.dgml");
    }


    /// <inheritdoc />
    public override void Execute()
    {
        string gdxLibraryPath =
            Path.GetFullPath(Path.Combine(GenerateCommand.InputDirectory, "..", "..", "Library", "ScriptAssemblies", "GDX.dll"));

        if (!File.Exists(gdxLibraryPath))
        {
            Output.Warning($"Unable to find GDX library at {gdxLibraryPath}");
            return;
        }

        string gdxEditorLibraryPath =
            Path.GetFullPath(Path.Combine(GenerateCommand.InputDirectory, "..", "..", "Library", "ScriptAssemblies",
                "GDX.Editor.dll"));

        if (!File.Exists(gdxLibraryPath))
        {
            Output.Warning($"Unable to find GDX library at {gdxLibraryPath}");
            return;
        }

        bool jsonExecute = ChildProcess.WaitFor(
            Path.Combine(ApiPort.InstallPath, "ApiPort.exe"),
            ApiPort.InstallPath,
            $"analyze -f {gdxLibraryPath} -f {gdxEditorLibraryPath} -o {GetJsonPath()} -r json");
        bool htmlExecute = ChildProcess.WaitFor(
            Path.Combine(ApiPort.InstallPath, "ApiPort.exe"),
            ApiPort.InstallPath,
            $"analyze -f {gdxLibraryPath} -f {gdxEditorLibraryPath} -o {GetHtmlPath()} -r html");
        bool dgmlExecute = ChildProcess.WaitFor(
            Path.Combine(ApiPort.InstallPath, "ApiPort.exe"),
            ApiPort.InstallPath,
            $"analyze -f {gdxLibraryPath} -f {gdxEditorLibraryPath} -o {GetDgmlPath()} -r dgml");

        if (!jsonExecute || !htmlExecute || !dgmlExecute)
        {
            Output.Error("ApiPort did not execute successfully.", -1, true);
        }
    }
}