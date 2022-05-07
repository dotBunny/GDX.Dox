// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Dox.Utils;

public static class Unity
{
    public static string GetLatest(string targetVersion = null)
    {
        Output.LogLine(targetVersion != null ? $"Finding Unity ({targetVersion}) ..." : $"Finding Unity (Latest) ...");

        // This is a very specific thing to our hardware, we use this environment variable on build machines
        // to tell where editors are installed.
        string installLocation = Environment.GetEnvironmentVariable("unityEditors");
        string installLaunch = Environment.GetEnvironmentVariable("unityLaunch");
        if (!string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(installLaunch))
        {
            Output.LogLine("Found environment variable hint ...");
            if (targetVersion == null)
            {
                if (Directory.Exists(installLocation))
                {
                    string[] installs = Directory.GetDirectories(installLocation, "*", SearchOption.TopDirectoryOnly);
                    if (installs.Length > 0)
                    {
                        string executable = Path.Combine(installs[installs.Length - 1], installLaunch);
                        if (File.Exists(executable))
                        {
                            Output.LogLine($"Found Hinted Unity @ {executable}.");
                            return executable;
                        }
                    }
                }
            }
            else
            {
                string targetBaseFolder = Path.Combine(installLocation, targetVersion);
                if (Directory.Exists(targetBaseFolder))
                {
                    string executable = Path.Combine(targetBaseFolder, installLaunch);
                    if (File.Exists(executable))
                    {
                        Output.LogLine($"Found Hinted Unity @ {executable}.");
                        return executable;
                    }
                }
            }
        }

        // Default Locations
        string editorsPath = null;
        string launchPath = null;

        if (OperatingSystem.IsWindows())
        {
            editorsPath = "C:\\Program Files\\Unity\\Hub\\Editor";
            launchPath = "Editor\\Unity.exe";
        }
        else if (OperatingSystem.IsMacOS())
        {
            editorsPath = "/Applications/Unity";
            launchPath = "Unity.app";
        }
        else if (OperatingSystem.IsLinux())
        {
            editorsPath = "/opt/unity";
            launchPath = "unity";
        }

        if (editorsPath != null)
        {
            if (targetVersion != null)
            {
                string versionedHubPath = Path.Combine(editorsPath, targetVersion);
                if (Directory.Exists(versionedHubPath))
                {
                    string versionedHubExecutable = Path.Combine(versionedHubPath, launchPath);
                    if (File.Exists(versionedHubExecutable))
                    {
                        Output.LogLine($"Found Hub Unity @ {versionedHubExecutable}.");
                        return versionedHubExecutable;
                    }
                }
            }
            else
            {
                if (Directory.Exists(editorsPath))
                {
                    string[] installs = Directory.GetDirectories(editorsPath, "*", SearchOption.TopDirectoryOnly);
                    if (installs.Length > 0)
                    {
                        string versionedHubExecutable = Path.Combine(installs[installs.Length - 1], launchPath);
                        if (File.Exists(versionedHubExecutable))
                        {
                            Output.LogLine($"Found Hub Unity @ {versionedHubExecutable}.");
                            return versionedHubExecutable;
                        }
                    }
                }
            }

        }

        return null;
    }
}