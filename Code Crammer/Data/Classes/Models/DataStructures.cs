#nullable enable

namespace Code_Crammer.Data.Classes.Models
{
    public class ProfileData
    {
        public string SolutionPath { get; set; } = string.Empty;
        public Dictionary<string, bool> OptionStates { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> CheckedFiles { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ExpandedNodes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public class SessionState
    {
        public HashSet<string> CheckedFiles { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ExpandedNodes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public class AppState
    {
        public HashSet<string>? CheckedFiles { get; set; }
        public Dictionary<string, bool> Options { get; set; } = new Dictionary<string, bool>();
        public string? ActionDescription { get; set; }
    }
}