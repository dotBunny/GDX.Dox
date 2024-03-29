﻿// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps;

// ReSharper disable once ClassNeverInstantiated.Global
public class XmlDocs : StepBase
{
    public const string DefinesKey = "defines";

    //TODO: these update the xml in teh main repo? should that be pushed if changes?

    public const string Key = "msbuild";
    public static string Defines = "GDX_LICENSED;GDX_ADDRESSABLES;GDX_PLATFORMS;GDX_VISUALSCRIPTING";

    public XmlDocs()
    {
        Program.Args.RegisterHelp("XML Documentation", $"{DefinesKey} <value>",
            $"\t\tDefined constants used when compiling the XML docs.\n\t\t\t\t[default] {Defines}");
    }

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "XML API Documentation";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        // Defines
        Program.GetParameter(DefinesKey, Defines, out Defines);
        Output.Value("XmlDocs.Defines", Defines);

        string gdxProjectPath =
            Path.GetFullPath(Path.Combine(Program.InputDirectory, "..", "..", "GDX.csproj"));
        string gdxXmlPath = Path.Combine(Program.InputDirectory, ".docfx", "GDX.xml");

        if (!File.Exists(gdxProjectPath))
        {
            Output.Warning("Skipping building the project as no project file was found.");
            return;
        }

        if (Build(gdxProjectPath, gdxXmlPath))
        {
            FixXmlDocs(gdxXmlPath);
        }

        // Were not gonna do editor for now
        // string gdxEditorProjectPath = Path.GetFullPath(Path.Combine(Config.InputDirectory, "..", "..", "GDX.Editor.csproj"));
        // string gdxEditorXmlPath = Path.Combine(Config.InputDirectory, ".docfx", "GDX.Editor.xml");
        //
        // if (Build(gdxEditorProjectPath, gdxEditorXmlPath))
        // {
        //     FixXmlDocs(gdxEditorXmlPath);
        // }
    }

    bool Build(string projectPath, string outputPath)
    {
        string[] defines = Defines.Split(';');

        // Ugly
        string[] originalFileContent = File.ReadAllLines(projectPath);
        string[] projectFileContent = new string[originalFileContent.Length];
        Array.Copy(originalFileContent, projectFileContent, originalFileContent.Length);

        int projectLength = projectFileContent.Length;
        for (int i = 0; i < projectLength; i++)
        {
            string line = projectFileContent[i].Trim();
            if (line.StartsWith("<DefineConstants>"))
            {
                List<string> defineCache = new();
                defineCache.AddRange(line.Substring(17, projectLength - 35).Split(";"));
                foreach (string define in defines)
                {
                    if (!defineCache.Contains(define))
                    {
                        defineCache.Add(define);
                    }
                }

                StringBuilder defineLine = new();
                foreach (string define in defineCache)
                {
                    defineLine.Append($"{define};");
                }

                string newLine = $"<DefineConstants>{defineLine}</DefineConstants>";
                projectFileContent[i] = newLine;
                break;
            }
        }

        // Write modified project file
        File.WriteAllLines(projectPath, projectFileContent);

        bool execute = ChildProcess.WaitFor(
            "dotnet",
            Directory.GetParent(projectPath)?.FullName,
            $"msbuild -property:Configuration=Debug;GenerateDocumentation=true;WarningLevel=0;DocumentationFile={outputPath} {projectPath}");

        // Restore file
        File.WriteAllLines(projectPath, originalFileContent);

        return execute;
    }

    void FixXmlDocs(string path)
    {
        if (!File.Exists(path))
        {
            Output.LogLine($"XML file does not exist at {path}.");
            return;
        }

        Output.LogLine($"Fixing XML in {path} for Bolt/VisualScripting compatibility.");

        string content = File.ReadAllText(path);
        bool found = true;
        while (found)
        {
            // Fix: <see cref="T:GDX.Collections.Pooling.ListManagedPool" />
            int crefIndexStart = content.IndexOf("<see cref=\"", StringComparison.Ordinal);
            if (crefIndexStart != -1)
            {
                int crefIndexEnd = content.IndexOf(">", crefIndexStart, StringComparison.Ordinal) + 1;
                string cref = content.Substring(crefIndexStart, crefIndexEnd - crefIndexStart);
                string newCref = cref.Substring(11, cref.IndexOf("\"", 11, StringComparison.Ordinal) - 11);
                if (newCref.StartsWith("T:") || newCref.StartsWith("M:") || newCref.StartsWith("F:"))
                {
                    newCref = newCref.Substring(2);
                }

                content = content.Replace(cref, newCref);
            }

            // Fix: <paramref name="pool"/>
            int paramIndexStart = content.IndexOf("<paramref name=\"", StringComparison.Ordinal);
            if (paramIndexStart != -1)
            {
                int paramIndexEnd = content.IndexOf(">", paramIndexStart, StringComparison.Ordinal) + 1;
                string param = content.Substring(paramIndexStart, paramIndexEnd - paramIndexStart);
                string newParam = param.Substring(16, param.IndexOf("\"", 16, StringComparison.Ordinal) - 16);
                if (newParam.StartsWith("T:") || newParam.StartsWith("M:") || newParam.StartsWith("F:"))
                {
                    newParam = newParam.Substring(2);
                }

                content = content.Replace(param, newParam);
            }

            // Fix: <see langword="true" />
            int langwordIndexStart = content.IndexOf("<see langword=\"", StringComparison.Ordinal);
            if (langwordIndexStart != -1)
            {
                int langwordIndexEnd = content.IndexOf(">", langwordIndexStart, StringComparison.Ordinal) + 1;
                string langword = content.Substring(langwordIndexStart, langwordIndexEnd - langwordIndexStart);
                string newLangword = langword.Substring(15, langword.IndexOf("\"", 15, StringComparison.Ordinal) - 15);
                if (newLangword.StartsWith("T:") || newLangword.StartsWith("M:") || newLangword.StartsWith("F:"))
                {
                    newLangword = newLangword.Substring(2);
                }

                content = content.Replace(langword, newLangword);
            }

            found = !(crefIndexStart == -1 && paramIndexStart == -1 && langwordIndexStart == -1);
        }

        File.WriteAllText(path, content);
    }
}