﻿using System;
using System.Diagnostics;
using System.CommandLine;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace ManagedCodeGen
{
    public class corediff
    {
        private static string asmTool = "mcgdiff";
        
        public class Config
        {
            private ArgumentSyntax syntaxResult;
            private string baseExe = null;
            private string diffExe = null;
            private string outputPath = null;
            private string tag = null;
            private string platformPath = null;
            private string testPath = null;
            
            public Config(string[] args) {

                syntaxResult = ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.DefineOption("b|base", ref baseExe, "The base compiler exe.");
                    syntax.DefineOption("d|diff", ref diffExe, "The diff compiler exe.");
                    syntax.DefineOption("o|output", ref outputPath, "The output path.");
                    syntax.DefineOption("t|tag", ref tag, "Name of root in output directory.  Allows for many sets of output.");
                    syntax.DefineOption("core_root", ref platformPath, "Path to test CORE_ROOT.");
                    syntax.DefineOption("test_root", ref testPath, "Path to test tree");
                });
                
                // Run validation code on parsed input to ensure we have a sensible scenario.
                
                validate();
            }
            
            void validate() {
                if (platformPath == null) {
                    syntaxResult.ReportError("Specifiy --core_root <path>");
                }
                
                if (testPath == null) {
                    syntaxResult.ReportError("Specify --test_root <path>");
                }
                
                if (outputPath == null) {
                    syntaxResult.ReportError("Specify --output <path>");
                }
                
                if ((baseExe == null) && (diffExe == null)) {
                    syntaxResult.ReportError("--base <path> or --diff <path> or both must be specified.");
                }
            }
            
            public string CoreRoot { get { return platformPath; } }
            public string TestRoot { get { return testPath; } }
            public string PlatformPath { get { return platformPath; } }
            public string BaseExecutable { get { return baseExe; } }
            public bool HasBaseExeutable { get { return (baseExe != null); } }
            public string DiffExecutable { get { return diffExe; } }
            public bool HasDiffExecutable { get { return (diffExe != null); } }
            public string OutputPath { get { return outputPath; } }
            public string Tag { get { return tag; } }
            public bool HasTag { get { return (tag != null); } }
        }
 
        private static string[] testDirectories = 
        {
            "Interop",
            "JIT"
        };
        
        private static string[] frameworkAssemblies = 
        {
            "mscorlib.dll",		
            "System.dll",		
            "System.Core.dll",		
            "System.Runtime.dll",		
            "System.Runtime.Extensions.dll",		
            "System.Runtime.Handles.dll",		
            "System.Runtime.InteropServices.dll",		
            "System.Runtime.InteropServices.PInvoke.dll",		
            "System.Runtime.InteropServices.RuntimeInformation.dll",		             "System.Runtime.Numerics.dll",		
            "System.Runtime.Serialization.Primitives.dll",		
            "Microsoft.CodeAnalysis.dll",		
            "Microsoft.CodeAnalysis.CSharp.dll",		
            "System.Collections.dll",		
            "System.Collections.Immutable.dll",		
            "System.Collections.ni.dll",		
            "System.Collections.NonGeneric.dll",		
            "System.Collections.Specialized.dll",		
            "System.ComponentModel.dll",		
            "System.Console.dll",		
            "System.Numerics.Vectors.dll",		
            "System.Text.Encoding.dll",		
            "System.Text.Encoding.Extensions.dll",		
            "System.Text.RegularExpressions.dll",		
            "System.Xml.dll",		
            "System.Xml.Linq.dll",		
            "System.Xml.ReaderWriter.dll",		
            "System.Xml.XDocument.dll",		
            "System.Xml.XmlDocument.dll",		
            "System.Xml.XmlSerializer.dll" 
        };
        
        public static int Main(string[] args)
        {
            Config config = new Config(args);
            
            Console.WriteLine("Beginning diff of {0}!", config.TestRoot);
            
            // Add each framework assembly to commandArgs
            
            // Create subjob that runs mcgdiff, which should be in path, with the 
            // relevent coreclr assemblies/paths.
            
            string frameworkArgs = String.Join(" ", frameworkAssemblies);
            string testArgs = String.Join(" ", testDirectories);
            
            
            List<string> commandArgs = new List<string>();
            
            // Set up CoreRoot
            commandArgs.Add("--platform");
            commandArgs.Add(config.CoreRoot);
            
            if (config.HasBaseExeutable) {
                commandArgs.Add("--base");  
                commandArgs.Add(config.BaseExecutable);
            }
            
            if (config.HasDiffExecutable) {
                commandArgs.Add("--diff");
                commandArgs.Add(config.DiffExecutable);
            }
            
            if (config.HasTag) {
                commandArgs.Add("--tag");
                commandArgs.Add(config.Tag);
            }

            // Set up full framework paths
            foreach (var assembly in frameworkAssemblies) {
                string coreRoot = config.CoreRoot;
                string fullPathAssembly = Path.Combine(coreRoot, assembly);
                
                if (!File.Exists(fullPathAssembly)) {
                    Console.WriteLine("can't find {0}", fullPathAssembly);
                    continue;
                }
                
                commandArgs.Add(fullPathAssembly);
            }
            
            foreach (var dir in testDirectories) {
                string testRoot = config.TestRoot;
                string fullPathDir = Path.Combine(testRoot, dir);
                
                if (!Directory.Exists(fullPathDir)) {
                    Console.WriteLine("can't find {0}", fullPathDir);
                    continue;
                }
                
                commandArgs.Add(fullPathDir);
            } 
            
            Command diffCmd = Command.Create(
                        asmTool, 
                        commandArgs);
                        
            CommandResult result = diffCmd.Execute();
            
            if (result.ExitCode != 0) {
                Console.WriteLine("Returned with {0} failures", result.ExitCode);
            }
            
            return result.ExitCode;
        }
    }
}
