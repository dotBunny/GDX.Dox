// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using Dox.Utils;

namespace Dox.Commands.Generate;

public static class DeployCommand
{
    public const string Argument = "deploy";

    public static void Process()
    {
        Output.LogLine("Deploying ...");
    }

    public static void RegisterHelp()
    {

    }
}