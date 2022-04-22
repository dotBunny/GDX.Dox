// Copyright (c) 2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dox.Commands.Generate;
using Dox.Utils;

namespace Dox;

/// <summary>
///     The GDX documentation generator, called Dox for some strange reason.
/// </summary>
static class Program
{
    /// <summary>
    ///     The processed arguments for the generator.
    /// </summary>
    public static Arguments Args;

    /// <summary>
    ///     Is an internet connection present and able to ping an outside host?
    /// </summary>
    public static bool IsOnline;

    /// <summary>
    ///     Is the execution occuring inside of our CI
    /// </summary>
    public static bool IsTeamCityAgent;

    /// <summary>
    ///     A cached absolute path to the process directory.
    /// </summary>
    public static readonly string ProcessDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    /// <summary>
    ///     An instantiated dictionary (by <see cref="IStep.GetIdentifier" />) of possible steps.
    /// </summary>
    public static readonly Dictionary<string, IStep> RegisteredSteps = new();

    /// <summary>
    ///     A cached reference to the programs assembly.
    /// </summary>
    public static Assembly ProgramAssembly;

    /// <summary>
    ///     Execution point of entry.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    // ReSharper disable once UnusedMember.Local
    static void Main(string[] args)
    {
        // Cache reference to local assembly
        ProgramAssembly = typeof(Program).Assembly;

        Output.LogLine(
            $"GDX Documentation Generator | Version {ProgramAssembly.GetName().Version} | Copyright (c) 2022 dotBunny Inc.",
            ConsoleColor.Green);
        Output.LogLine($"Started on {DateTime.Now:F}", ConsoleColor.DarkGray);
        Output.LogLine("Initializing ...");

        bool init = Init.Process(args);
        GenerateCommand.RegisterHelp();
        DeployCommand.RegisterHelp();

        // Check for help request
        if (Args.Has(Arguments.HelpKey) || !init)
        {
            Args.Help();
            return;
        }

        bool hasCommand = Args.Has(GenerateCommand.Argument) || Args.Has(DeployCommand.Argument);
        if (!hasCommand)
        {
            GenerateCommand.Process();
        }
        else
        {
            if (Args.Has(GenerateCommand.Argument))
            {
                GenerateCommand.Process();
            }

            if (Args.Has(DeployCommand.Argument))
            {
                if (GenerateCommand.IsHosting)
                {
                    Output.LogLine("Skipping deployment as documentation is being served.");
                    return;
                }

                DeployCommand.Process();
            }
        }

    }

    /// <summary>
    ///     Get the value for a given parameter from the config, overriden by the arguments, but also have a
    ///     failsafe default value.
    /// </summary>
    /// <param name="key">The argument identifier or config identifier.</param>
    /// <param name="defaultValue">A built-in default value.</param>
    /// <param name="resolvedValue">The resolved parameter value written to the provided <see cref="string" />.</param>
    /// <param name="processFunction">A function to manipulate the value retrieved.</param>
    /// <param name="validateFunction">A function to validate the working value.</param>
    /// <returns>true/false if value was found.</returns>
    public static bool GetParameter(string key, string defaultValue, out string resolvedValue,
        Func<string, string> processFunction = null, Func<string, bool> validateFunction = null)
    {
        bool success = false;
        resolvedValue = processFunction != null ? processFunction.Invoke(defaultValue) : defaultValue;
        if (Args.TryGetValue(key, out string foundOverride))
        {
            if (processFunction != null)
            {
                foundOverride = processFunction.Invoke(foundOverride);
            }

            if (validateFunction != null)
            {
                if (validateFunction.Invoke(foundOverride))
                {
                    resolvedValue = foundOverride;
                    success = true;
                }
            }
            else
            {
                resolvedValue = foundOverride;
                success = true;
            }
        }

        // Special case to check for default value passing validation.
        if (defaultValue == null)
        {
            return success;
        }

        if (validateFunction != null)
        {
            success = validateFunction.Invoke(defaultValue);
        }

        return success;
    }

    /// <summary>
    ///     Set a variable for future reference in the running environment.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    public static void SetEnvironmentVariable(string name, string value)
    {
        if (Args.Has(Arguments.SetTeamCityKey))
        {
            // Set for TeamCity
            Output.LogLine($"##teamcity[setParameter name='{name}' value='{value}']", ConsoleColor.Yellow);
        }

        // Set for user (no-perm request)
        if (Args.Has(Arguments.SetUserEnvironmentKey))
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }
    }
}