// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Dox.Commands.Generate.Steps;
using Dox.Utils;

namespace Dox.Commands.Generate;

public static class DeployCommand
{
    public enum Branch
    {
        Unknown,
        Main,
        Dev
    }

    public const string Argument = "deploy";
    public const string BranchKey = "branch";
    public const string TempFolder = "deploy";

    public static Branch TargetBranch = Branch.Unknown;
    public static string TargetFolder = Path.Combine(Program.ProcessDirectory, TempFolder);

    public static void Process()
    {
        Output.LogLine("Deploying ...");

        if (!Program.Args.Has(BranchKey))
        {
            Output.Error("The branch must be set via argument.", -1, true);
        }

        Program.GetParameter(BranchKey, null, out string branch);
        TargetBranch = branch.ToLower() switch
        {
            "main" => Branch.Main,
            "dev" => Branch.Dev,
            _ => TargetBranch
        };
        if (TargetBranch == Branch.Unknown)
        {
            Output.Error("Unrecognized branch.", -1, true);
        }

        string gitRepository = TargetBranch switch
        {
            Branch.Dev => "https://github.com/dotBunny/GDX.DevDocs",
            Branch.Main => "https://github.com/dotBunny/GDX.MainDocs",
            _ => null
        };
        if (gitRepository == null)
        {
            Output.Error("Unable to properly identify Git repository.", -1, true);
        }

        // We do not want any preexisting conflicts
        if (Directory.Exists(TargetFolder))
        {
            DirectoryInfo di = new DirectoryInfo(TargetFolder);
            Platform.NormalizeFolder(di);
            Directory.Delete(TargetFolder, true);
        }

        // Checkout
        Git.GetOrUpdate($"{TargetBranch.ToString()} Docs", TargetFolder, gitRepository);
        Git.Checkout(TargetFolder, "main");

        // Delete the existing docs
        string docsFolder = Path.Combine(TargetFolder, "docs");

        // Preserve CNAME
        bool hasCNAME = false;
        string tempFile = Path.Combine(Program.ProcessDirectory, "cname.tmp");
        string cnameFile = Path.Combine(docsFolder, "CNAME");
        if (File.Exists(cnameFile))
        {
            File.Copy(cnameFile, tempFile);
            hasCNAME = true;
        }

        Directory.Delete(docsFolder, true);
        Directory.CreateDirectory(docsFolder);

        // Put back CNAME
        if (hasCNAME)
        {
            File.Copy(tempFile, cnameFile);
        }

        File.Delete(tempFile);

        // Move set in place
        string sourceFolder = Build.GetOutputFolder();
        Output.LogLine($"Copying output ({sourceFolder}) to destination ({docsFolder}).");
        Platform.CopyFilesRecursively(Build.GetOutputFolder(), docsFolder);

        // Create commit
        string repoCommit = Git.GetHeadCommit(Program.InputDirectory).Substring(0, 7);
        string commitMessage = $"Generated documentation at {repoCommit}.";
        Git.Commit(TargetFolder, commitMessage);

        // Push
        if (!Program.IsTeamCityAgent)
        {
            Output.Warning("Not pushing commit as this is not in CI/CD");
            return;
        }
        ChildProcess.WaitFor(Platform.IsWindows() ? "git.exe" : "git", TargetFolder,
            $"push origin main");
    }


    public static void RegisterHelp()
    {
    }
}