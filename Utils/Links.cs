// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Dox.Utils;

public static class Links
{
    public enum Host
    {
        Local,
        Dev,
        Main
    }

    public static void ValidateLinks(string basePath, Host host = Host.Local)
    {
        int goodLinks = 0;
        int badLinks = 0;
        int gitHubLinks = 0;
        // Ugly LINQ method but best way to do it apparently
        IEnumerable<string> findFiles = Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".htm") || s.EndsWith(".html"));

        string linkBase = "http://localhost:8080/";
        if (host == Host.Dev)
        {
            linkBase = "https://gdx-dev.dotbunny.com/";
        }
        else if (host == Host.Main)
        {
            linkBase = "https://gdx.dotbunny.com/";
        }

        string[] files = findFiles.ToArray();
        int fileCount = files.Length;
        Output.LogLine($"Checking links in {fileCount} files; this will take a while ...");
        List<string> checkedLinks = new List<string>();
        for (int i = 0; i < fileCount; i++)
        {
            string path = files[i];
            if (path == null) continue;

            string content = File.ReadAllText(path);
            DirectoryInfo parent = Directory.GetParent(path);
            string relativePath = string.Empty;
            if (parent != null)
            {
                relativePath = Path.GetRelativePath(basePath, parent.FullName).Replace("\\", "/", StringComparison.InvariantCulture);
                if (!relativePath.EndsWith("/"))
                {
                    relativePath = $"{relativePath}/";
                }
            }
            string[] links = FindLinks(content);
            int linkCount = links.Length;
            if (linkCount > 0)
            {
                for (int j = 0; j < linkCount; j++)
                {
                    string link = links[j];
                    if (string.IsNullOrEmpty(link)) continue;

                    // Lets actually make this link useful
                    if (!link.StartsWith("https://") && !link.StartsWith("http://"))
                    {
                        link = link.StartsWith("/") ? $"{linkBase}{link.Substring(1)}" : $"{linkBase}{relativePath}{link}";
                    }

                    if (checkedLinks.Contains(link)) continue;

                    Task<HttpStatusCode> delay = Commands.Generate.Steps.Host.Get(link, 2);
                    delay.Wait();
                    bool found = ((int)delay.Result >= 200) && ((int)delay.Result <= 299);
                    if (!found)
                    {
                        if (link.Contains("github.com"))
                        {
                            Output.LogLine($"Unable to access {link} first found in {path} ({(int)delay.Result}).");
                            Output.LogLine("\tThis usually is a false positive due to GitHub fighting scraping and returning 404.", ConsoleColor.DarkGray);
                            gitHubLinks++;
                        }
                        else
                        {
                            Output.LogLine($"Unable to access {link} first found in {path}.", ConsoleColor.Red);
                            badLinks++;
                        }
                    }
                    else
                    {
                        goodLinks++;
                    }
                    checkedLinks.Add(link);
                }
            }
        }

        Output.LogLine($"Good Links: {goodLinks}");
        Output.LogLine($"GitHub Links: {gitHubLinks}");
        Output.LogLine($"Bad Links: {badLinks}");
    }

    public static void ModifyContent(string basePath, Host host = Host.Local)
    {
        // Ugly LINQ method but best way to do it apparently
        IEnumerable<string> findFiles = Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".htm") || s.EndsWith(".html") || s.EndsWith(".xml") || s.EndsWith(".json") || s.EndsWith(".yml"));

        string[] files = findFiles.ToArray();
        int fileCount = files.Length;
        Output.LogLine($"Altering {fileCount} files in {host} mode ...");
        for (int i = 0; i < fileCount; i++)
        {
            string path = files[i];
            string fileName = Path.GetFileName(path);
            string content = File.ReadAllText(path);

            // Do change automation page
            if (fileName == "automation.html") continue;

            // Get new content
            string newContent = Replace(content, host);

            // Is there a change?
            if (content != newContent)
            {
                Output.LogLine($"Updated {path}.");
                File.WriteAllText(path, newContent);
            }
        }
    }

    public static string[] FindLinks(string content)
    {
        List<string> links = new List<string>();
        int currentIndex = 0;
        int foundIndex = 0;
        while (foundIndex != -1)
        {
            foundIndex = content.IndexOf("href=\"", currentIndex, StringComparison.InvariantCulture);
            if (foundIndex != -1)
            {
                int endIndex = content.IndexOf("\"", foundIndex + 6, StringComparison.InvariantCulture);

                string rawUri = content.Substring(foundIndex + 6, endIndex - foundIndex - 6);
                if (!rawUri.StartsWith("#") && !rawUri.StartsWith("data:") && !rawUri.StartsWith("mailto:"))
                {
                    // Now we just wanna cleanup inside the URI of additional params
                    int anchorIndex = rawUri.IndexOf("#", StringComparison.InvariantCulture);
                    if (anchorIndex != -1)
                    {
                        rawUri = rawUri.Substring(0, anchorIndex);
                    }

                    int paramIndex = rawUri.IndexOf("?", StringComparison.InvariantCulture);
                    if (paramIndex != -1)
                    {
                        rawUri = rawUri.Substring(0, paramIndex);
                    }

                    if (!links.Contains(rawUri))
                    {
                        links.Add(rawUri);
                    }
                }
                currentIndex = endIndex;
            }
        }
        return links.ToArray();
    }

    public static string Replace(string content, Host host)
    {
        switch (host)
        {
            case Host.Local:
                content = content.Replace("/gdx-dev.dotbunny.com", "/localhost:8080");
                content = content.Replace("/gdx.dotbunny.com", "/localhost:8080");
                // Special case cause our local hosting does not have SSL
                content = content.Replace("https://localhost:8080", "http://localhost:8080");

                // GitHub content links default to dev in this case
                content = content.Replace("https://github.com/dotBunny/GDX/blob/main/",
                    "https://github.com/dotBunny/GDX/blob/dev/");
                return content;
            case Host.Main:
                content = content.Replace("/gdx-dev.dotbunny.com", "/gdx.dotbunny.com");
                content = content.Replace("/localhost:8080", "/gdx.dotbunny.com");
                // GitHub content links default to dev in this case
                content = content.Replace("https://github.com/dotBunny/GDX/blob/dev/",
                    "https://github.com/dotBunny/GDX/blob/main/");
                return content;
            case Host.Dev:
            default:
                content = content.Replace("/gdx.dotbunny.com", "/gdx-dev.dotbunny.com");
                content = content.Replace("/localhost:8080", "/gdx-dev.dotbunny.com");
                content = content.Replace("https://github.com/dotBunny/GDX/blob/main/",
                    "https://github.com/dotBunny/GDX/blob/dev/");
                return content;
        }
    }
}