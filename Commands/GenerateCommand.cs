// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Dox.Commands.Generate.Steps;
using Dox.Commands.Generate.Steps.Reports;
using Dox.Utils;

namespace Dox.Commands.Generate;

public static class GenerateCommand
{
    public const string Argument = "generate";
    public const string StepsKey = "steps";

    public const string InputKey = "input";


    public static string InputDirectory = "../../../../Projects/GDX_Development/Packages/com.dotbunny.gdx";


    /// <summary>
    ///     An ordered list of the steps to be processed as defined.
    /// </summary>
    static readonly List<IStep> k_OrderedSteps = new();

    public static bool IsHosting;

    public static void Process()
    {
        Output.LogLine("Generating ...");

        // Validate provided input directory
        bool validInput = Program.GetParameter(InputKey, InputDirectory, out InputDirectory,
            s =>
            {
                if (Path.IsPathFullyQualified(s))
                {
                    return s;
                }

                return Path.GetFullPath(Path.Combine(Program.ProcessDirectory, s));
            }, Directory.Exists);
        if (!validInput)
        {
            Output.Error($"Unable to find input folder ({InputDirectory}. Please provide a valid absolute path.", -1, true);
        }
        Output.Value("GenerateCommand.InputDirectory", InputDirectory);

        //TODO: Should we validate the path

        // Build out ordered steps
        Program.GetParameter(StepsKey, Config.Steps, out Config.Steps);
        string[] stepSplit = Config.Steps.Split(',', StringSplitOptions.TrimEntries);
        foreach (string targetStep in stepSplit)
        {
            string targetStepLower = targetStep.ToLower();
            if (Program.RegisteredSteps.ContainsKey(targetStepLower))
            {
                k_OrderedSteps.Add(Program.RegisteredSteps[targetStepLower]);
            }
            else
            {
                Output.Log($"Unable to find '{targetStepLower}' step.", ConsoleColor.Yellow);
            }
        }

        if (k_OrderedSteps.Count == 0)
        {
            Output.Error("No steps defined.", -1, true);
        }

        // Process steps
        foreach (IStep step in k_OrderedSteps)
        {
            Output.SectionHeader(step.GetHeader());
            step.Execute();
        }
    }

    public static void RegisterHelp()
    {
        Program.Args.RegisterHelp("Generate", $"{InputKey} <value>",
            $"\t\t\tThe relative or absolute path to the GDX package.\n\t\t\t\t[default] {InputDirectory}", true);
    }
}