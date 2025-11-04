using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Core.IO;
using Vintagestory.API.Common;

namespace CakeBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }

    public class BuildContext : FrostingContext
    {
        public const string ProjectName = "AttributeRenderingLibrary";
        public string BuildConfiguration { get; }
        public string Version { get; }
        public string VersionRaw { get; }
        public string Name { get; }
        public bool SkipJsonValidation { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            BuildConfiguration = context.Argument("configuration", "Release");
            SkipJsonValidation = context.Argument("skipJsonValidation", false);
            var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{ProjectName}/modinfo.json");
            Version = $"v{modInfo.Version}";
            VersionRaw = modInfo.Version;
            Name = ProjectName;
        }
    }

    [TaskName("ValidateJson")]
    public sealed class ValidateJsonTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (context.SkipJsonValidation)
            {
                return;
            }
            var jsonFiles = context.GetFiles($"../{BuildContext.ProjectName}/assets/**/*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(file.FullPath);
                    JToken.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                }
            }
        }
    }

    [TaskName("Build")]
    [IsDependentOn(typeof(ValidateJsonTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetClean($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
                new DotNetCleanSettings
                {
                    Configuration = context.BuildConfiguration
                });


            context.DotNetPublish($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
                new DotNetPublishSettings
                {
                    Configuration = context.BuildConfiguration
                });
        }
    }

    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists("../Releases");
            context.CleanDirectory("../Releases");
            context.EnsureDirectoryExists($"../Releases/{context.Name}");
            context.CopyFiles($"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*", $"../Releases/{context.Name}");
            if (context.DirectoryExists($"../{BuildContext.ProjectName}/assets"))
            {
                context.CopyDirectory($"../{BuildContext.ProjectName}/assets", $"../Releases/{context.Name}/assets");
            }
            context.CopyFile($"../{BuildContext.ProjectName}/modinfo.json", $"../Releases/{context.Name}/modinfo.json");
            if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
            {
                context.CopyFile($"../{BuildContext.ProjectName}/modicon.png", $"../Releases/{context.Name}/modicon.png");
            }
            context.Zip($"../Releases/{context.Name}", $"../Releases/{context.Name}-{context.Version}.zip");
        }
    }
    
    [TaskName("PackNupkg")]
    public sealed class PackNupkgTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var version = context.VersionRaw;
            var projectDir = context.MakeAbsolute(
                context.Environment.WorkingDirectory
                    .Combine("..")
                    .Combine(BuildContext.ProjectName));
            var artifactsDir = context.MakeAbsolute(
                projectDir
                    .Combine("bin")
                    .Combine(context.BuildConfiguration)
                    .Combine("Mods")
                    .Combine("mod"));

            var dotnet = context.Tools.Resolve("dotnet") ?? "dotnet";
            var packArgs = new ProcessArgumentBuilder()
                .Append("pack")
                .AppendQuoted(projectDir.Combine(BuildContext.ProjectName + ".csproj").FullPath)
                .Append("-c Release")
                .AppendSwitch("-o", " ", artifactsDir.FullPath.Replace('\\', '/'))
                .Append($"-p:PackageVersion={version}");

            var packExit = context.StartProcess(dotnet, new ProcessSettings { Arguments = packArgs });
            if (packExit != 0)
                throw new Exception($"dotnet pack failed (exit {packExit}).");
        }
    }
    
    [TaskName("Release")]
    [IsDependentOn(typeof(PackNupkgTask))]
    public sealed class ReleaseTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var version = context.VersionRaw;
            var projectDir = context.MakeAbsolute(
                context.Environment.WorkingDirectory
                    .Combine("..")
                    .Combine(BuildContext.ProjectName));
            var artifactsDir = context.MakeAbsolute(
                projectDir
                    .Combine("bin")
                    .Combine(context.BuildConfiguration)
                    .Combine("Mods")
                    .Combine("mod"));

            var dotnet = context.Tools.Resolve("dotnet") ?? "dotnet";

            // Find all .nupkg produced and filter by actual <version> inside the .nuspec
            var nupkgs = context.GetFiles($"{artifactsDir}/*.nupkg")
                .Where(p => !p.FullPath.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var toPush = nupkgs
                .Select(p => new { Path = p, Meta = ReadIdVersionFromNupkg(p) })
                .Where(x => x.Meta.Version != null && string.Equals(x.Meta.Version, version, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!toPush.Any())
                throw new Exception($"No .nupkg with version {version} found in {artifactsDir}.");

            var apiKey = context.EnvironmentVariable("NUGET_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("NUGET_API_KEY is not set.");

            var source = context.EnvironmentVariable("NUGET_SOURCE");
            if (string.IsNullOrWhiteSpace(source))
                source = "https://api.nuget.org/v3/index.json";

            // Push main packages
            foreach (var item in toPush)
            {
                var args = new ProcessArgumentBuilder()
                    .Append("nuget push")
                    .AppendQuoted(item.Path.FullPath.Replace('\\', '/'))
                    .AppendSwitch("--api-key", " ", apiKey)
                    .AppendSwitch("--source", " ", source)
                    .Append("--skip-duplicate");

                var exit = context.StartProcess(dotnet, new ProcessSettings { Arguments = args });
                if (exit != 0)
                    throw new Exception($"Failed to push {item.Path.GetFilename()} (exit {exit}).");

                // Push matching .snupkg if present
                var snupkg = FilePath.FromString(System.IO.Path.ChangeExtension(item.Path.FullPath, ".snupkg"));
                if (context.FileExists(snupkg))
                {
                    var symArgs = new ProcessArgumentBuilder()
                        .Append("nuget push")
                        .AppendQuoted(snupkg.FullPath.Replace('\\', '/'))
                        .AppendSwitch("--api-key", " ", apiKey)
                        .AppendSwitch("--source", " ", source)
                        .Append("--skip-duplicate");

                    var symExit = context.StartProcess(dotnet, new ProcessSettings { Arguments = symArgs });
                    if (symExit != 0)
                        throw new Exception($"Failed to push symbols {snupkg.GetFilename()} (exit {symExit}).");
                }

                context.Information($"Published {item.Meta.PackageId} {item.Meta.Version} to NuGet.");
            }
        }

        // Reads <id> and <version> from the .nupkg's .nuspec (no guessing!)
        private static (string PackageId, string Version) ReadIdVersionFromNupkg(FilePath nupkg)
        {
            using var zip = ZipFile.OpenRead(nupkg.FullPath);
            var nuspec = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                        ?? throw new Exception($"No .nuspec in {nupkg.GetFilename()}");

            using var s = nuspec.Open();
            var doc = XDocument.Load(s);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            var meta = doc.Descendants(ns + "metadata").FirstOrDefault() ?? doc.Root;

            string id = meta.Element(ns + "id")?.Value?.Trim()
                      ?? doc.Descendants("id").FirstOrDefault()?.Value?.Trim();

            string ver = meta.Element(ns + "version")?.Value?.Trim()
                       ?? doc.Descendants("version").FirstOrDefault()?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ver))
                throw new Exception($"Could not read id/version from {nupkg.GetFilename()}");

            return (id, ver);
        }
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask
    {
    }
}