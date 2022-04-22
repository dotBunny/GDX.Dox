// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.NetworkInformation;
using Dox.Utils;

namespace Dox.Commands.Generate;

public static class Init
{
    /// <summary>
    ///     The defined hostname to use when pinging to determine an outside connection.
    /// </summary>
    public static string PingHost = "github.com";
    public const string PingHostKey = "ping-host";

    public static bool Process(string[] args)
    {
        Output.Value("Program.ProcessDirectory", Program.ProcessDirectory);

        // Parse the arguments
        Program.Args = new Arguments(args);

        // PingHost
        Program.GetParameter(PingHostKey, "github.com", out Init.PingHost);
        Output.Value("Init.PingHost", Init.PingHost);

        // Check Internet Connection
        Ping ping = new();
        try
        {
            PingReply reply = ping.Send(Init.PingHost, 3000);
            if (reply != null)
            {
                Program.IsOnline = reply.Status == IPStatus.Success;
            }
        }
        catch (Exception e)
        {
            Output.LogLine(e.Message, ConsoleColor.Yellow);
            Program.IsOnline = false;
        }
        finally
        {
            Output.Value("Program.IsOnline", Program.IsOnline.ToString());
        }

        // Establish if this is a TeamCity
        Program.IsTeamCityAgent = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") != null;
        Output.Value("Program.IsTeamCityAgent", Program.IsTeamCityAgent.ToString());

        // Search the assemblies for included IStep's and create an instance of each, using the system activator.
        Type stepInterface = typeof(IStep);
        Type[] types = Program.ProgramAssembly.GetTypes();
        foreach (Type t in types)
        {
            if (t == stepInterface || !stepInterface.IsAssignableFrom(t) || t.IsAbstract)
            {
                continue;
            }

            // Create instance and register instance
            IStep step = (IStep)Activator.CreateInstance(t);
            if (step == null)
            {
                continue;
            }

            Program.RegisteredSteps.Add(step.GetIdentifier().ToLower(), step);
        }

        return true;
    }
}