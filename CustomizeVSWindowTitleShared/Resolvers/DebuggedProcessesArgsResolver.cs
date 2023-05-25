using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using EnvDTE80;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class DebuggedProcessesArgsResolver : SimpleTagResolver {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagName">"debuggedProcesses", to always show the process name and arguments, and "debuggedProcessesArgs", to show the process name only if there are more than one process, are supported.</param>
        public DebuggedProcessesArgsResolver(string tagName = "debuggedProcesses") : base(tagName: tagName) {
            if (tagName != "debuggedProcesses" && tagName != "debuggedProcessesArgs") throw new ArgumentOutOfRangeException(nameof(tagName));
        }

        public override string Resolve(AvailableInfo info) {
            var list = new List<Tuple<string, string>>();
            // var process = Globals.DTE.Debugger.CurrentProcess; returns null for some reason.
            foreach (Process2 process in Globals.DTE.Debugger.DebuggedProcesses) {
                if (GetCommandLine(process, out var executablePath, out var args) && !string.IsNullOrWhiteSpace(args)) {
                    list.Add(Tuple.Create(executablePath, args));
                }
            }
            if (!list.Any()) return "";
            if (this.TagName == "debuggedProcessesArgs" && list.Count == 1) {
                return list.Single().Item2;
            }
            return string.Join(" & ", list.Select(e => Path.GetFileName(e.Item1) + " " + e.Item2));
        }

        // Define an extension method for type System.Process that returns the command 
        // line via WMI.
        private static bool GetCommandLine(Process2 process, out string executablePath, out string args) {
            executablePath = "";
            args = "";
            if (process == null) return false;
            string cmdLine = null;
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine,ExecutablePath FROM Win32_Process WHERE ProcessId = {process.ProcessID}")) {
                // By definition, the query returns at most 1 match, because the process 
                // is looked up by ID (which is unique by definition).
                var matchEnum = searcher.Get().GetEnumerator();
                if (matchEnum.MoveNext()) // Move to the 1st item.
                {
                    cmdLine = matchEnum.Current["CommandLine"]?.ToString();
                    executablePath = matchEnum.Current["ExecutablePath"]?.ToString();
                }
            }
            if (cmdLine == null || executablePath == null) {
                // Not having found a command line implies 1 of 2 exceptions, which the
                // WMI query masked:
                // An "Access denied" exception due to lack of privileges.
                // A "Cannot process request because the process (<pid>) has exited."
                // exception due to the process having terminated.
                // We provoke the same exception again simply by accessing process.MainModule.
                //var dummy = process.MainModule; // Provoke exception.
                return false;
            }
            args = cmdLine.Replace("\"" + executablePath + "\" ", "");
            return true;
        }
    }
}