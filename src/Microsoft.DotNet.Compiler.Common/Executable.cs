﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Files;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;

namespace Microsoft.Dotnet.Cli.Compiler.Common
{
    public class Executable
    {
        private readonly ProjectContext _context;

        private readonly OutputPathInfo _outputPathInfo;

        private readonly LibraryExporter _exporter;

        public Executable(ProjectContext context, OutputPathInfo outputPathInfo, LibraryExporter exporter)
        {
            _context = context;
            _outputPathInfo = outputPathInfo;
            _exporter = exporter;
        }

        public void MakeCompilationOutputRunnable()
        {
            var outputPath = _outputPathInfo.RuntimeOutputPath;

            CopyContentFiles(outputPath);

            ExportRuntimeAssets(outputPath);
        }

        private void ExportRuntimeAssets(string outputPath)
        {
            if (_context.TargetFramework.IsDesktop())
            {
                MakeCompilationOutputRunnableForFullFramework(outputPath);
            }
            else
            {
                MakeCompilationOutputRunnableForCoreCLR(outputPath);
            }
        }

        private void MakeCompilationOutputRunnableForFullFramework(
            string outputPath)
        {
            CopyAllDependencies(outputPath, _exporter);
            GenerateBindingRedirects(_exporter);
        }

        private void MakeCompilationOutputRunnableForCoreCLR(string outputPath)
        {
            WriteDepsFileAndCopyProjectDependencies(_exporter, _context.ProjectFile.Name, outputPath);

            // TODO: Pick a host based on the RID
            CoreHost.CopyTo(outputPath, _context.ProjectFile.Name + Constants.ExeSuffix);
        }

        private void CopyContentFiles(string outputPath)
        {
            var contentFiles = new ContentFiles(_context);
            contentFiles.StructuredCopyTo(outputPath);
        }

        private static void CopyAllDependencies(string outputPath, LibraryExporter exporter)
        {
            exporter
                .GetAllExports()
                .SelectMany(e => e.RuntimeAssets())
                .CopyTo(outputPath);
        }

        private static void WriteDepsFileAndCopyProjectDependencies(
            LibraryExporter exporter,
            string projectFileName,
            string outputPath)
        {
            exporter
                .GetDependencies(LibraryType.Package)
                .WriteDepsTo(Path.Combine(outputPath, projectFileName + FileNameSuffixes.Deps));

            exporter
                .GetAllExports()
                .Where(e => e.Library.Identity.Type == LibraryType.Project)
                .SelectMany(e => e.RuntimeAssets())
                .CopyTo(outputPath);
        }

        public void GenerateBindingRedirects(LibraryExporter exporter)
        {
            var outputName = _outputPathInfo.RuntimeFiles.Assembly;

            var existingConfig = new DirectoryInfo(_context.ProjectDirectory)
                .EnumerateFiles()
                .FirstOrDefault(f => f.Name.Equals("app.config", StringComparison.OrdinalIgnoreCase));

            XDocument baseAppConfig = null;

            if (existingConfig != null)
            {
                using (var fileStream = File.OpenRead(existingConfig.FullName))
                {
                    baseAppConfig = XDocument.Load(fileStream);
                }
            }

            var appConfig = exporter.GetAllExports().GenerateBindingRedirects(baseAppConfig);

            if (appConfig == null) { return; }

            var path = outputName + ".config";
            using (var stream = File.Create(path))
            {
                appConfig.Save(stream);
            }
        }
    }
}
