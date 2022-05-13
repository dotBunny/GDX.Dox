// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

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
            translated = translated.Replace("Packages\\com.dotbunny.gdx\\", string.Empty);
            generator.Append(translated);


            File.WriteAllText(GetPath(), generator.ToString());

            File.Copy(teamCityArtifact, Path.Combine(Program.InputDirectory, ".docfx", "reports", "inspection.xml"));

            Output.LogLine("START XLST");
            Output.Log(GetTransformation());
            Output.NextLine();
            Output.LogLine("END XLST");

            Output.LogLine("START MD");
            Output.Log(generator.ToString());
            Output.NextLine();
            Output.LogLine("END MD");

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
        TextGenerator generator = new TextGenerator();
        generator.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        generator.AppendLine("<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" exclude-result-prefixes=\"msxsl\">");
        generator.AppendLine("<xsl:key name=\"ISSUETYPES\" match=\"/Report/Issues/Project/Issue\" use=\"@TypeId\"/>");
        generator.AppendLine("<xsl:output method=\"html\" indent=\"yes\"/>");
        generator.AppendLine("<xsl:template match=\"/\" name=\"TopLevelReport\">");
        generator.AppendLine("<xsl:for-each select=\"/Report/IssueTypes/IssueType\">");
        generator.AppendLine();
        generator.AppendLine("### <xsl:value-of select=\"@Severity\"/>: <xsl:value-of select=\"@Description\"/>");
        generator.AppendLine();
        generator.AppendLine("| File | Line Number | Message |");
        generator.AppendLine("| :--- | :--- | ---- |");
        generator.AppendLine("<xsl:for-each select=\"key('ISSUETYPES', @Id)\">");
        generator.AppendLine(
            "| <xsl:value-of select=\"@File\"/> | <xsl:value-of select=\"@Line\"/> | <xsl:value-of select=\"@Message\"/> |");
        generator.AppendLine("</xsl:for-each>");
        generator.AppendLine("</xsl:for-each>");
        generator.AppendLine("</xsl:template>");
        generator.AppendLine("</xsl:stylesheet>");
        return generator.ToString();
    }
}