﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class IncrementalTests : IncrementalTestBase
    {

        public IncrementalTests() : base(
            Path.Combine(AppContext.BaseDirectory, "TestProjects", "TestSimpleIncrementalApp"),
            "TestSimpleIncrementalApp",
            "Hello World!" + Environment.NewLine)
        {
        }

        [Fact]
        public void TestNoIncrementalFlag()
        {
            var buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);

            buildResult = BuildProject(noIncremental: true);
            Assert.Contains("[Forced Unsafe]", buildResult.StdOut);
        }

        [Fact]
        public void TestRebuildMissingPdb()
        {
            TestDeleteOutputWithExtension("pdb");
        }

        [Fact]
        public void TestRebuildMissingDll()
        {
            TestDeleteOutputWithExtension("dll");
        }

        [Fact]
        public void TestRebuildMissingXml()
        {
            TestDeleteOutputWithExtension("xml");
        }

        [Fact]
        public void TestNoLockFile()
        {

            var buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);

            var lockFile = Path.Combine(TempProjectRoot.Path, "project.lock.json");
            Assert.True(File.Exists(lockFile));

            File.Delete(lockFile);
            Assert.False(File.Exists(lockFile));

            buildResult = BuildProject(expectBuildFailure : true);
            Assert.Contains("does not have a lock file", buildResult.StdOut);
        }

        [Fact]
        public void TestRebuildChangedLockFile()
        {

            var buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);

            var lockFile = Path.Combine(TempProjectRoot.Path, "project.lock.json");
            TouchFile(lockFile);

            buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);
        }

        [Fact]
        public void TestRebuildChangedProjectFile()
        {

            var buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);

            TouchFile(GetProjectFile(MainProject));

            buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);
        }

        // regression for https://github.com/dotnet/cli/issues/965
        [Fact]
        public void TestInputWithSameTimeAsOutputCausesProjectToCompile()
        {
            var buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);

            var outputTimestamp = SetAllOutputItemsToSameTime();

            // set an input to have the same last write time as an output item
            // this should trigger recompilation to account for file systems with second timestamp granularity
            // (an input file that changed within the same second as the previous outputs should trigger a rebuild)
            File.SetLastWriteTime(GetProjectFile(MainProject), outputTimestamp);

            buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);
        }

        private DateTime SetAllOutputItemsToSameTime()
        {
            var now = DateTime.Now;
            foreach (var f in Directory.EnumerateFiles(GetCompilationOutputPath()))
            {
                File.SetLastWriteTime(f, now);
            }
            return now;
        }

        private void TestDeleteOutputWithExtension(string extension)
        {

            var buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);

            Reporter.Verbose.WriteLine($"Files in {GetCompilationOutputPath()}");
            foreach (var file in Directory.EnumerateFiles(GetCompilationOutputPath()))
            {
                Reporter.Verbose.Write($"\t {file}");
            }

            // delete output files with extensions
            foreach (var outputFile in Directory.EnumerateFiles(GetCompilationOutputPath()).Where(f =>
            {
                var fileName = Path.GetFileName(f);
                return fileName.StartsWith(MainProject, StringComparison.OrdinalIgnoreCase) &&
                       fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }))
            {
                Reporter.Output.WriteLine($"Deleted {outputFile}");

                File.Delete(outputFile);
                Assert.False(File.Exists(outputFile));
            }

            // second build; should get rebuilt since we deleted an output item
            buildResult = BuildProject();
            buildResult.Should().HaveCompiledProject(MainProject);
        }
    }
}