// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps;

public class ProjectFiles : StepBase
{
    public const string Key = "proj";

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Generate Project Files";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        string projectPath = Path.GetFullPath(Path.Combine(Program.InputDirectory, "..", ".."));
        string testForProject = Path.Combine(projectPath, "GDX.csproj");
        if (File.Exists(testForProject))
        {
            Output.LogLine("Found existing GDX.csproj, using it instead!");
            return;
        }

        string findUnity = Unity.GetLatest();
        if (string.IsNullOrEmpty(findUnity))
        {
            Output.Error("Unable to find Unity.", -1, true);
            return;
        }

        bool execute = ChildProcess.WaitFor(
            findUnity,
            projectPath,
            $"-projectPath {projectPath} -executeMethod GDX.Editor.Automation.GenerateProjectFiles -quit");

        if (!execute)
        {
            Output.Error("An error occured while generating project files.", -1, true);
        }
    }
}