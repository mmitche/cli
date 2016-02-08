// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class OutputPathCalculator
    {
        private const string ObjDirectoryName = "obj";
        private const string BinDirectoryName = "bin";

        private readonly Project _project;
        private readonly NuGetFramework _framework;

        private readonly string _runtimePath;
        private readonly RuntimeOutputFiles _runtimeFiles;
        private readonly CompilationOutputFiles _compilationFiles;

        public string CompilationOutputPath { get; }

        public string IntermediateOutputDirectoryPath { get; }

        public string RuntimeOutputPath
        {
            get
            {
                if (_runtimePath == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot get runtime output path for {nameof(OutputPathCalculator)} with no runtime set");
                }
                return _runtimePath;
            }
        }

        public OutputPathCalculator(
            Project project,
            NuGetFramework framework,
            string runtimeIdentifier,
            string configuration,
            string solutionRootPath,
            string buildBasePath,
            string outputPath)
        {
            _project = project;
            _framework = framework;

            string resolvedBuildBasePath;
            if (string.IsNullOrEmpty(buildBasePath))
            {
                resolvedBuildBasePath = _project.ProjectDirectory;
            }
            else
            {
                if (string.IsNullOrEmpty(solutionRootPath))
                {
                    resolvedBuildBasePath = Path.Combine(buildBasePath, project.Name);
                }
                else
                {
                    resolvedBuildBasePath = _project.ProjectDirectory.Replace(solutionRootPath, buildBasePath);
                }
            }

            CompilationOutputPath = PathUtility.EnsureTrailingSlash(Path.Combine(resolvedBuildBasePath,
                BinDirectoryName,
                configuration,
                _framework.GetShortFolderName()));

            if (string.IsNullOrEmpty(outputPath))
            {
                if (!string.IsNullOrEmpty(runtimeIdentifier))
                {
                    _runtimePath = PathUtility.EnsureTrailingSlash(Path.Combine(CompilationOutputPath, runtimeIdentifier));
                }
            }
            else
            {
                _runtimePath = PathUtility.EnsureTrailingSlash(Path.GetFullPath(outputPath));
            }

            IntermediateOutputDirectoryPath = PathUtility.EnsureTrailingSlash(Path.Combine(
                resolvedBuildBasePath,
                ObjDirectoryName,
                configuration,
                _framework.GetTwoDigitShortFolderName()));

            _compilationFiles = new CompilationOutputFiles(CompilationOutputPath, project, configuration, framework);
            if (_runtimePath != null)
            {
                _runtimeFiles = new RuntimeOutputFiles(_runtimePath, project, configuration, framework);
            }
        }

        public CompilationOutputFiles GetCompilationFiles()
        {
            return _compilationFiles;
        }

        public RuntimeOutputFiles GetRuntimeFiles()
        {
            if (_runtimeFiles == null)
            {
                throw new InvalidOperationException(
                    $"Cannot get runtime output files for {nameof(OutputPathCalculator)} with no runtime set");
            }
            return _runtimeFiles;
        }
    }
}
