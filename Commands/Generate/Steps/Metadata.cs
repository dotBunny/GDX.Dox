﻿// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps;

// ReSharper disable once ClassNeverInstantiated.Global
public class Metadata : StepBase
{
    public const string Key = "metadata";

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Metadata Extraction";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        string docfxJsonPath = Path.Combine(Program.InputDirectory, ".docfx", "docfx.json");
        if (!File.Exists(docfxJsonPath))
        {
            Output.Error($"Unable to find required docfx.json at {docfxJsonPath}.", -1, true);
        }

        bool execute = ChildProcess.WaitFor(
            Path.Combine(DocFx.InstallPath, "docfx.exe"),
            DocFx.InstallPath,
            $"{docfxJsonPath} --metadata");

        if (!execute)
        {
            Output.Error("An error occured while running the Metadata extraction.", -1, true);
        }
    }
}