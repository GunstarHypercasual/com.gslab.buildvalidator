// BuildStep.cs
namespace GSLab.BuildValidator
{
    using System;
    using System.Collections.Generic;

    public class BuildStep
    {
        public string Name { get; }
        readonly Func<bool> action;
        public BuildStep(string name, Func<bool> action) { Name = name; this.action = action; }
        public bool Execute(List<(string, bool)> status)
        {
            bool ok = action();
            status.Add((Name, ok));
            return ok;
        }
    }
}