﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.IO;
#if BuildMaster
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
#elif Otter
using Inedo.Otter.Documentation;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
#endif

namespace Inedo.Extensions.Operations.Files
{
    [DisplayName("Copy Files")]
    [Description("Copies files on a server.")]
    [ScriptAlias("Copy-Files")]
    [ScriptNamespace(Namespaces.Files, PreferUnqualified = true)]
    [Tag(Tags.Files)]
    [Example(@"
# copy all files and all subdirectories beneath it to the target,
# and log each individual file that is copied, and overwrite any files
Copy-Files(
    From: E:\Source,
    To: F:\Target,
    Include: **,
    Verbose: true,
    Overwrite: true
);
")]
    public sealed class CopyFilesOperation : ExecuteOperation
    {
        private int filesCopied;
        private int directoriesCopied;

        [ScriptAlias("Include")]
        [Description(CommonDescriptions.MaskingHelp)]
        public IEnumerable<string> Includes { get; set; }
        [ScriptAlias("Exclude")]
        [Description(CommonDescriptions.MaskingHelp)]
        public IEnumerable<string> Excludes { get; set; }
        [ScriptAlias("From")]
        [Description(CommonDescriptions.SourceDirectory)]
        [PlaceholderText("$WorkingDirectory")]
        [DisplayName("Source directory")]
        public string SourceDirectory { get; set; }
        [Required]
        [ScriptAlias("To")]
        [Description(CommonDescriptions.TargetDirectory)]
        [DisplayName("Target directory")]
        public string TargetDirectory { get; set; }
        [ScriptAlias("Verbose")]
        [DisplayName("Verbose")]
        [Description(CommonDescriptions.VerboseLogging)]
        public bool VerboseLogging { get; set; }
        [ScriptAlias("Overwrite")]
        [Description(CommonDescriptions.Overwrite)]
        public bool Overwrite { get; set; }

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            var sourceDirectory = context.ResolvePath(this.SourceDirectory);
            var targetDirectory = context.ResolvePath(this.TargetDirectory);

            this.LogInformation($"Copying files from {sourceDirectory} to {targetDirectory}...");
            return this.CopyDirectoryAsync(context.Agent.GetService<IFileOperationsExecuter>(), sourceDirectory, targetDirectory, context);
        }

        private async Task CopyDirectoryAsync(IFileOperationsExecuter fileOps, string sourcePath, string targetPath, IOperationExecutionContext context)
        {
            if (!await fileOps.DirectoryExistsAsync(sourcePath).ConfigureAwait(false))
            {
                this.LogWarning($"Source directory {sourcePath} does not exist.");
                return;
            }

            var infos = await fileOps.GetFileSystemInfosAsync(
                sourcePath,
                new MaskingContext(this.Includes, this.Excludes)
            ).ConfigureAwait(false);

            var files = infos.OfType<SlimFileInfo>();

            foreach (var file in files)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var targetFileName = PathEx.Combine(targetPath, file.FullName.Substring(sourcePath.Length).TrimStart('/', '\\'));
                if (this.VerboseLogging)
                    this.LogDebug($"Copying {file.FullName} to {targetFileName}...");

                try
                {
                    if (!this.Overwrite && await fileOps.FileExistsAsync(targetFileName).ConfigureAwait(false))
                    {
                        this.LogError($"Target file {targetFileName} already exists and overwrite is set to false.");
                        continue;
                    }

                    await fileOps.CreateDirectoryAsync(file.DirectoryName).ConfigureAwait(false);
                    await fileOps.CopyFileAsync(file.FullName, targetFileName, this.Overwrite).ConfigureAwait(false);
                    this.filesCopied++;
                }
                catch (Exception ex)
                {
                    this.LogError($"Cannot copy {file.FullName}: {ex.Message}");
                }
            }

            var dirs = infos.OfType<SlimDirectoryInfo>();
            foreach (var dir in dirs)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var targetDir = PathEx.Combine(targetPath, dir.FullName.Substring(sourcePath.Length).TrimStart('/', '\\'));
                if (this.VerboseLogging)
                    this.LogDebug($"Creating directory {targetDir}...");

                await fileOps.CreateDirectoryAsync(targetDir).ConfigureAwait(false);
                this.directoriesCopied++;
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Copy ",
                    new MaskHilite(config[nameof(this.Includes)], config[nameof(this.Excludes)])
                ),
                new RichDescription(
                    "from ",
                    new DirectoryHilite(config[nameof(this.SourceDirectory)]),
                    " to ",
                    new DirectoryHilite(config[nameof(this.TargetDirectory)])
                )
            );
        }
    }
}
