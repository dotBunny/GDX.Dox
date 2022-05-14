// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Xml;
using System.Xml.Xsl;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps.TeamCity;

public class CodeInspection : StepBase
{
    //Staging\ResharperInspection.xml
    public const string Key = "code-inspection";
    public const string Title = "Code Inspection";
    const string k_LinkStartTag = "___STARTLINK___";
    const string k_LinkEndTag = "___ENDLINK___";
    const string k_DescriptionStartTag = "___STARTDESC___";
    const string k_DescriptionEndTag = "___ENDDESC___";

    static string GetPath()
    {
        return Path.Combine(Program.InputDirectory, ".docfx", "reports", "inspection.md");
    }

    /// <inheritdoc />
    public override void Clean()
    {
        File.WriteAllText(GetPath(), $"---\n_disableContribution: true\n---\n# {Title}\n\nTo be generated.");
        Output.LogLine("Reset inspection report.");
    }

    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Code Inspection";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        if (!Program.IsTeamCityAgent)
        {
            Output.LogLine("Skipping. Not running inside of CI/CD.");
            return;
        }

        string teamCityArtifact =
            Path.GetFullPath(Path.Combine(Program.ProcessDirectory, "..", "..", "..", "..", "Staging",
                "ResharperInspection.xml"));
        if (File.Exists(teamCityArtifact))
        {
            // Build transformation
            string tempPath = Path.GetTempFileName();
            XslTransform transform = new();
            StringReader transformReader = new(GetTransformation());
            XmlReader reader = XmlReader.Create(transformReader);
            transform.Load(reader);
            transform.Transform(teamCityArtifact, tempPath);
            transformReader.Dispose();

            TextGenerator generator = new TextGenerator();
            generator.AppendLine("---");
            generator.AppendLine("_disableContribution: true");
            generator.AppendLine("---");
            generator.AppendLine("# Code Inspection Report");
            generator.AppendLine();
            generator.AppendLine("[XML](/reports/inspection.xml)");
            generator.AppendLine();
            generator.AppendLine();

            string translated = File.ReadAllText(tempPath);

            // Long paths
            translated = translated.Replace("Packages\\com.dotbunny.gdx\\", string.Empty);

            // Line Endings
            translated = translated.Replace("|\r\n\r\n|", "|\r\n|");
            translated = translated.Replace("|\n\n|", "|\n|");

            // Make links
            Output.LogLine("Creating hyperlinks ...");
            int currentIndex = 0;
            int foundIndex = 0;
            int startLength = k_LinkStartTag.Length;
            int endLength = k_LinkEndTag.Length;
            while (foundIndex != -1)
            {
                foundIndex = translated.IndexOf(k_LinkStartTag, currentIndex, StringComparison.Ordinal);
                if (foundIndex != -1)
                {
                    int endIndex = translated.IndexOf(k_LinkEndTag, foundIndex + startLength, StringComparison.Ordinal);
                    if (endIndex != -1)
                    {
                        string foundLink = translated.Substring(foundIndex + startLength,
                            endIndex - (foundIndex + startLength));
                        string[] splitLink = foundLink.Split(':');

                        translated = translated.Replace($"{k_LinkStartTag}{foundLink}{k_LinkEndTag}",
                            $"[{Path.GetFileName(splitLink[0])}:{splitLink[1]}](https://github.com/dotBunny/GDX/blob/main/{splitLink[0].Replace("\\", "/")}#L{splitLink[1]} \"{foundLink}\")");
                        currentIndex = endIndex + endLength;
                    }
                    else
                    {
                        currentIndex = foundIndex + startLength;
                    }

                }
            }

            // Catch keywords
            Output.LogLine("Building type blocks ...");
            currentIndex = 0;
            foundIndex = 0;
            startLength = k_DescriptionStartTag.Length;
            endLength = k_DescriptionEndTag.Length;
            while (foundIndex != -1)
            {
                foundIndex = translated.IndexOf(k_DescriptionStartTag, currentIndex, StringComparison.Ordinal);
                if (foundIndex != -1)
                {
                    int endIndex = translated.IndexOf(k_DescriptionEndTag, foundIndex + endLength, StringComparison.Ordinal);
                    if (endIndex != -1)
                    {
                        string foundDescription = translated.Substring(foundIndex + startLength,
                            endIndex - (foundIndex + startLength));

                        translated = translated.Replace($"{k_DescriptionStartTag}{foundDescription}{k_DescriptionEndTag}",
                            foundDescription.Replace("'", "`"));

                        currentIndex = endIndex + endLength;
                    }
                    else
                    {
                        currentIndex = foundIndex + startLength;
                    }

                }
            }


            generator.Append(translated);


            File.WriteAllText(GetPath(), generator.ToString());

            File.Copy(teamCityArtifact, Path.Combine(Program.InputDirectory, ".docfx", "reports", "inspection.xml"));
            File.Delete(tempPath);
            Output.LogLine("Built code inspection markdown.");
        }
        else
        {
            Output.Error($"Unable to find code inspection artifacts at {teamCityArtifact}.", -1, true);
        }
    }

    string GetTransformation()
    {
        TextGenerator generator = new();
        generator.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        generator.AppendLine("<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" exclude-result-prefixes=\"msxsl\">");
        generator.AppendLine("<xsl:key name=\"ISSUETYPES\" match=\"/Report/Issues/Project/Issue\" use=\"@TypeId\"/>");
        generator.AppendLine("<xsl:output method=\"html\" indent=\"yes\"/>");
        generator.AppendLine("<xsl:template match=\"/\" name=\"TopLevelReport\">");

        GenerateTable(generator, "Errors", "ERROR");
        GenerateTable(generator, "Warnings", "WARNING");
        GenerateTable(generator, "Suggestions", "SUGGESTION");
        GenerateTable(generator, "Hints", "HINT");

        generator.AppendLine("</xsl:template>");
        generator.AppendLine("</xsl:stylesheet>");
        return generator.ToString();
    }

    void GenerateTable(TextGenerator generator, string sectionTitle, string severity)
    {
        // Define the data were working on
        generator.AppendLine($"<xsl:variable name=\"allItems{severity}\"  select=\"/Report/IssueTypes/IssueType[@Severity='{severity}']\" />");

        // Do we actually have any?
        generator.AppendLine($"<xsl:if test=\"count($allItems{severity}) &gt; 0\">");

        // Build table
        generator.AppendLine($"## {sectionTitle}");
        generator.AppendLine($"<xsl:for-each select=\"$allItems{severity}\">");
        generator.AppendLine();
        generator.AppendLine("### <xsl:value-of select=\"@Description\"/>");
        generator.AppendLine();
        generator.AppendLine("| File | Message |");
        generator.AppendLine("| :--- | ---- |");
        generator.AppendLine("<xsl:for-each select=\"key('ISSUETYPES', @Id)\">");
        generator.AppendLine(
            $"| {k_LinkStartTag}<xsl:value-of select=\"@File\"/>:<xsl:value-of select=\"@Line\"/>{k_LinkEndTag} | {k_DescriptionStartTag}<xsl:value-of select=\"@Message\"/>{k_DescriptionEndTag} |");
        generator.AppendLine("</xsl:for-each>");
        generator.AppendLine("</xsl:for-each>");

        generator.AppendLine("</xsl:if>");
    }
}