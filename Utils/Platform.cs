// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace Dox.Utils;

public static class Platform
{
    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public static bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    public static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    public static void NormalizeFolder(DirectoryInfo directory)
    {
        foreach (DirectoryInfo subDir in directory.GetDirectories())
        {
            NormalizeFolder(subDir);
        }

        foreach (FileInfo file in directory.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }

    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            string destination = newPath.Replace(sourcePath, targetPath);
            Output.LogLine($"Copying {newPath} => {destination}");
            File.Copy(newPath, destination, true);
        }
    }
}