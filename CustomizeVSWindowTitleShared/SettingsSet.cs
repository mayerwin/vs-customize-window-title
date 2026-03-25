using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ErwinMayerLabs.RenameVSWindowTitle
{
    public class SettingsSet
    {
        // Original FullPath of solution (null if no solution loaded)
        public string SolutionFilePath;
        // Original FileName of solution (null if no solution loaded)
        public string SolutionFileName;
        
        // Apply overrides for Paths, Paths is null for solution override
        public List<string> Paths; 

        public int? ClosestParentDepth;
        public int? FarthestParentDepth;
        public string AppendedString;

        // solution name (file name part or override value)
        public string SolutionName;

        public string PatternIfRunningMode;
        public string PatternIfBreakMode;
        public string PatternIfDesignMode;

        public string GitWorkingDirectory;

        // if s is not null, override d
        private void merge<T>(ref T d, T s)
        {
            if (s!=null) d=s;
        }

        public void Merge(SettingsSet s)
        {
            // merge all overridable values
            merge(ref SolutionName, s.SolutionName);
            merge(ref ClosestParentDepth, s.ClosestParentDepth);
            merge(ref FarthestParentDepth, s.FarthestParentDepth);
            merge(ref AppendedString, s.AppendedString);
            merge(ref PatternIfRunningMode, s.PatternIfRunningMode);
            merge(ref PatternIfBreakMode, s.PatternIfBreakMode);
            merge(ref PatternIfDesignMode, s.PatternIfDesignMode);
            merge(ref GitWorkingDirectory, s.GitWorkingDirectory);
        }
    }
}
