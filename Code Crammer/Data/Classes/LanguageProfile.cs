#nullable disable

namespace Code_Crammer.Data
{
    public class LanguageProfile
    {
        public string Name { get; set; }
        public List<string> Extensions { get; set; } = new List<string>();
        public string CommentPattern { get; set; }
        public Dictionary<string, string> DistillPatterns { get; set; } = new Dictionary<string, string>();
    }
}