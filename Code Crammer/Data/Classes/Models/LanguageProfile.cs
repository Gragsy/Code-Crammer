#nullable enable
namespace Code_Crammer.Data.Classes.Models
{
    public class LanguageProfile
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Extensions { get; set; } = new List<string>();

        public string? CommentPattern { get; set; }

        public Dictionary<string, string> DistillPatterns { get; set; } = new Dictionary<string, string>();
    }
}