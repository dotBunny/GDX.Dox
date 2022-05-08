// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dox.Commands.Generate;
using Dox.Commands.Generate.Steps.Unpack;
using Dox.Utils;

namespace Dox.Commands.Generate.Steps;

// ReSharper disable once ClassNeverInstantiated.Global
public class Host : StepBase
{
    public const string Key = "host";

    /// <inheritdoc />
    public override string GetIdentifier()
    {
        return Key;
    }

    /// <inheritdoc />
    public override string GetHeader()
    {
        return "Host";
    }

    /// <inheritdoc />
    public override void Execute()
    {
        if (Program.IsTeamCityAgent)
        {
            Output.LogLine("Hosting was skipped due to running in TeamCity");
            return;
        }

        string docfxJsonPath = Path.GetFullPath(Path.Combine(Program.InputDirectory, ".docfx", "docfx.json"));
        if (!File.Exists(docfxJsonPath))
        {
            Output.Error($"Unable to find required docfx.json at {docfxJsonPath}.", -1, true);
        }

        string contentFolder = Path.GetFullPath(Path.Combine(Program.InputDirectory, ".docfx", "_site"));
        if (!Directory.Exists(contentFolder))
        {
            Output.Error("Content folder does not exist", -1, true);
        }

        // Find and replace all links with localhost:8080
        Links.ModifyContent(contentFolder, Links.Host.Local);

        int execute = ChildProcess.Spawn(
            Path.Combine(DocFx.InstallPath, "docfx.exe"),
            DocFx.InstallPath,
            $"{docfxJsonPath}  --serve");

        if (execute == -1)
        {
            Output.Error("An error occured while building the documentation.", -1, true);
        }

        Console.WriteLine(
            $"\nDocumentation should become available at:\n\nhttp://localhost:8080/\n\nYou will need to CTRL+Click that link, or navigate there in a browser.\nClosing the spawned window (PID:{execute}) will stop the hosting.");

        GenerateCommand.IsHosting = true;

        // We now need to wait for the server to startup, after this we are going to have to replace things again as it will alter them :(
        bool isServerLive = false;
        while (!isServerLive)
        {
            Output.LogLine("Waiting for server response ...");
            Task<HttpStatusCode> delay = Get("http://localhost:8080/index.html");
            delay.Wait();
            Output.LogLine(delay.Result.ToString());


            isServerLive = ((int)delay.Result >= 200) && ((int)delay.Result <= 299);
        }

        // Find and replace all links with localhost:8080 (AGAIN)
        Links.ModifyContent(contentFolder, Links.Host.Local);

        // Link Validation
        Links.ValidateLinks(contentFolder);

    }

    public static async Task<HttpStatusCode> Get(string address, int timeoutSeconds = 5)
    {
        HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        HttpResponseMessage result = new HttpResponseMessage();
        HttpRequestMessage request = new HttpRequestMessage
        {
            RequestUri = new Uri(address),
            Method = HttpMethod.Get
        };
        try
        {
            result = await httpClient.SendAsync(request);
        }
        catch (Exception)
        {
            result.StatusCode = HttpStatusCode.BadGateway;
        }
        return result.StatusCode;
    }
}