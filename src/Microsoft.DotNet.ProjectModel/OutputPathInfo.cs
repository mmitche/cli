// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class OutputPathInfo
    {
        private const string ObjDirectoryName = "obj";
        private const string BinDirectoryName = "bin";

        private readonly string _runtimePath;
        private readonly RuntimeOutputFiles _runtimeFiles;

        public string CompilationOutputPath { get; }

        public string IntermediateOutputDirectoryPath { get; }

        public string RuntimeOutputPath
        {
            get
            {
                if (_runtimePath == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot get runtime output path for {nameof(OutputPathInfo)} with no runtime set");
                }
                return _runtimePath;
            }
        }

        public CompilationOutputFiles CompilationFiles { get; }

        public RuntimeOutputFiles RuntimeFiles
        {
            get
            {
                if (_runtimeFiles == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot get runtime output files for {nameof(OutputPathInfo)} with no runtime set");
                }
                return _runtimeFiles;
            }
        }

        public OutputPathInfo(
            Project project,
            NuGetFramework framework,
            string runtimeIdentifier,
            string configuration,
            string solutionRootPath,
            string buildBasePath,
            string outputPath)
        {
            string resolvedBuildBasePath;
            if (string.IsNullOrEmpty(buildBasePath))
            {
                resolvedBuildBasePath = project.ProjectDirectory;
            }
            else
            {
                if (string.IsNullOrEmpty(solutionRootPath))
                {
                    resolvedBuildBasePath = Path.Combine(buildBasePath, project.Name);
                }
                else
                {
                    resolvedBuildBasePath = project.ProjectDirectory.Replace(solutionRootPath, buildBasePath);
                }
            }

            CompilationOutputPath = PathUtility.EnsureTrailingSlash(Path.Combine(resolvedBuildBasePath,
                BinDirectoryName,
                configuration,
                framework.GetShortFolderName()));

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
                framework.GetTwoDigitShortFolderName()));

            CompilationFiles = new CompilationOutputFiles(CompilationOutputPath, project, configuration, framework);
            if (_runtimePath != null)
            {
                _runtimeFiles = new RuntimeOutputFiles(_runtimePath, project, configuration, framework);
            }
        }
    }
}
