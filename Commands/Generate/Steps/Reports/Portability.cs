// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.Utils;
using HtmlAgilityPack;
using System.Linq;
using System.Text;

namespace Dox.Commands.Generate.Steps.Reports;

// ReSharper disable once ClassNeverInstantiated.Global
public class Portability : StepBase
{
    public const string Key = "portability";

    public override string[] GetRequiredStepIdentifiers()
    {
        // Generates project
        return new[] { XmlDocs.Key };
    }

    /// <inheritdoc />
    public override void Clean()
    {
        if (File.Exists(GetDgmlPath()))
        {
            File.Delete(GetDgmlPath());
        }
    }

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "API Portability";
    }

    string GetMarkdownPath()
    {
        return Path.Combine(Program.InputDirectory, ".docfx", "reports", "portability.md");
    }

    string GetDgmlPath()
    {
        return Path.Combine(Program.InputDirectory, ".docfx", "reports", "portability.dgml");
    }


    /// <inheritdoc />
    public override void Execute()
    {
        string gdxLibraryPath =
            Path.GetFullPath(Path.Combine(Program.InputDirectory, "..", "..", "Library", "ScriptAssemblies",
                "GDX.dll"));

        if (!File.Exists(gdxLibraryPath))
        {
            Output.Warning($"Unable to find GDX library at {gdxLibraryPath}");
            return;
        }

        string gdxEditorLibraryPath =
            Path.GetFullPath(Path.Combine(Program.InputDirectory, "..", "..", "Library", "ScriptAssemblies",
                "GDX.Editor.dll"));

        if (!File.Exists(gdxLibraryPath))
        {
            Output.Warning($"Unable to find GDX library at {gdxLibraryPath}");
            return;
        }

        string tempFile = $"{Path.GetTempFileName()}.html";

        bool htmlExecute = ChildProcess.WaitFor(
            Path.Combine(ApiPort.InstallPath, "ApiPort.exe"),
            ApiPort.InstallPath,
            $"analyze -f {gdxLibraryPath} -f {gdxEditorLibraryPath} -o {tempFile} -r html");
        bool dgmlExecute = ChildProcess.WaitFor(
            Path.Combine(ApiPort.InstallPath, "ApiPort.exe"),
            ApiPort.InstallPath,
            $"analyze -f {gdxLibraryPath} -f {gdxEditorLibraryPath} -o {GetDgmlPath()} -r dgml");


        // Manipulate HTML to something that we can change
        if (File.Exists(tempFile))
        {
            Output.LogLine($"Converting {tempFile} to Markdown.");
            HtmlAgilityPack.HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(File.ReadAllText(tempFile));
            //File.WriteAllText(Path.Combine(Program.InputDirectory, "test.html"), File.ReadAllText(tempFile));
            TextGenerator generator = new TextGenerator();
            generator.AppendLine("---");
            generator.AppendLine("_disableContribution: true");
            generator.AppendLine("---");


            HtmlNode firstHeader = doc.DocumentNode.Descendants("h1").First();
            generator.AppendLine($"# {firstHeader.InnerHtml}");
            generator.AppendLine();
            generator.AppendLine("[DGML](/reports/portability.dgml)");
            generator.AppendLine();

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//table[@id='Portability Summary']//tr"); //|th|td|a[@href]
            int rowIndex = 0;
            StringBuilder rowBuilder = new();
            foreach (HtmlNode row in nodes)
            {
                rowBuilder.Clear();
                rowIndex += 1;
                if (rowIndex == 1)
                {
                    StringBuilder headerBuilder = new();
                    foreach (HtmlNode col in row.SelectNodes("th"))
                    {
                        rowBuilder.Append(col.InnerText.Trim());
                        rowBuilder.Append(" | ");
                        headerBuilder.Append("---- |");
                    }

                    generator.AppendLine(rowBuilder.ToString().Trim().TrimEnd('|').Trim());
                    generator.AppendLine(headerBuilder.ToString().Trim().TrimEnd('|').Trim());
                }
                else
                {
                    foreach (HtmlNode col in row.SelectNodes("td|th/a[@href]"))
                    {
                        rowBuilder.Append(col.InnerText.Trim());
                        rowBuilder.Append(" | ");
                    }

                    generator.AppendLine(rowBuilder.ToString().Trim().TrimEnd('|').Trim());
                }
            }

            File.WriteAllText(GetMarkdownPath(), generator.ToString());
            File.Delete(tempFile);
        }

        if (!htmlExecute || !dgmlExecute)
        {
            Output.Error("ApiPort did not execute successfully.", -1, true);
        }
    }
}