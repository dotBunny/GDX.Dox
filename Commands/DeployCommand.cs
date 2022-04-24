// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.
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
        Output.SectionHeader("Deploy To Pages");

        if (!Program.Args.Has(BranchKey))
        {
            Output.Error($"The branch must be set via argument (--{BranchKey} <main/dev>).", -1, true);
        }

        // Figure out what branch we are working on.
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



        // Populate the git repository URI based on if we are in CI/CD where we can count on SSH, falling back to
        // HTTPS route if were not allowing for user authentication to take place.
        string gitRepository = null;
        if (Program.IsTeamCityAgent)
        {
            gitRepository = TargetBranch switch
            {

                Branch.Dev => "git@github.com:dotBunny/GDX.DevDocs.git",
                Branch.Main => "git@github.com:dotBunny/GDX.MainDocs.git",
                _ => null
            };
        }
        else
        {
            gitRepository = TargetBranch switch
            {
                Branch.Dev => $"https://github.com/dotBunny/GDX.DevDocs",
                Branch.Main => $"https://github.com/dotBunny/GDX.MainDocs",
                _ => null
            };
        }

        // If we dont have a URI we have problems.
        if (gitRepository == null)
        {
            Output.Error("Unable to properly identify Git repository.", -1, true);
        }

        // We do not want any preexisting conflicts
        if (Directory.Exists(TargetFolder))
        {
            Output.LogLine($"Deleting existing content in {TargetFolder} ...");
            DirectoryInfo di = new DirectoryInfo(TargetFolder);
            Platform.NormalizeFolder(di);
            Directory.Delete(TargetFolder, true);
        }

        // Figure out git command to use once.
        string gitCommand = Platform.IsWindows() ? "git.exe" : "git";

        // Checkout
        Output.LogLine($"Cloning repository {gitRepository} into {TargetFolder} ...");
        Git.GetOrUpdate($"{TargetBranch.ToString()} Docs", TargetFolder, gitRepository, null, 1);

        // Cache reference to where the docs are going to be living
        string docsFolder = Path.Combine(TargetFolder, "docs");

        // We need to remove everything, but we also need to preserver the CNAME file to ensure hosting persists.
        bool hasCNAME = false;
        string tempFile = Path.Combine(Program.ProcessDirectory, "cname.tmp");
        string cnameFile = Path.Combine(docsFolder, "CNAME");
        if (File.Exists(cnameFile))
        {
            Output.Log("Backing up CNAME ...");
            File.Copy(cnameFile, tempFile);
            hasCNAME = true;
        }

        Output.LogLine("Deleting existing documentation in repository ...");
        Directory.Delete(docsFolder, true);
        Directory.CreateDirectory(docsFolder);
        if (hasCNAME)
        {
            Output.Log("Restoring CNAME ...");
            File.Copy(tempFile, cnameFile);
        }
        File.Delete(tempFile);

        // Move set in place
        string sourceFolder = Build.GetOutputFolder();
        Output.LogLine($"Copying output ({sourceFolder}) to destination ({docsFolder}) ...");
        Platform.CopyFilesRecursively(Build.GetOutputFolder(), docsFolder);

        Output.LogLine("Checking for differences ...");
        bool statusCheck = Git.HasChanges(TargetFolder);
        if (!statusCheck)
        {
            Cleanup();
            Output.LogLine("No changes found stopping deploy.");
            return;
        }

        // Create commit
        Output.LogLine($"Check for head commit hash ...");
        string repoCommit = Git.GetHeadCommit(Program.InputDirectory).Substring(0, 7);

        Output.LogLine($"Add all new content in {TargetFolder} ...");
        bool addContentCheck = ChildProcess.WaitFor(gitCommand, TargetFolder, "add --all --verbose --renormalize");
        if (!addContentCheck)
        {
            Cleanup();
            Output.Error("Adding output failed to execute cleanly.", -1, true);
        }

        Output.LogLine($"Create commit for difference in {TargetFolder} ...");
        bool commitCreateCheck = ChildProcess.WaitFor(gitCommand, TargetFolder, $"commit -m \"Generated documentation at {repoCommit}.\" --verbose");
        if (!commitCreateCheck)
        {
            Cleanup();
            Output.Error("Creating commit failed to execute cleanly.", -1, true);
        }

        // Pushing automagically happens only on the CI/CD
        if (!Program.IsTeamCityAgent)
        {
            Output.Warning("Not pushing commit as this is not in CI/CD");
            return;
        }

        Output.LogLine("Pushing upstream ...");
        bool pushCheck = ChildProcess.WaitFor(gitCommand, TargetFolder,
            $"push -f origin refs/heads/main --verbose");
        if (!pushCheck)
        {
            Cleanup();
            Output.Error("Pushing to remote failed to execute cleanly.", -1, true);
        }

        Cleanup();
    }

    public static void Cleanup()
    {
        if (Directory.Exists(TargetFolder))
        {
            Output.LogLine("Removing deploying / working directory ...");
            Platform.NormalizeFolder(new DirectoryInfo(TargetFolder));
            Directory.Delete(TargetFolder, true);
        }
    }


    public static void RegisterHelp()
    {
    }
}