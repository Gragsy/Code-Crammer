#nullable enable

using System.ComponentModel;

namespace Code_Crammer.Data.Classes.Models
{
    public class ScraperOptions
    {
        public bool IncludeCode { get; set; }
        public bool IncludeProjectFile { get; set; }
        public bool IncludeResx { get; set; }
        public bool IncludeConfig { get; set; }
        public bool IncludeDesigner { get; set; }
        public bool SanitizeOutput { get; set; }
        public bool IncludeJson { get; set; }
        public bool IncludeFolderLayout { get; set; }
        public bool SquishDesignerFiles { get; set; }
        public bool OpenFolderOnCompletion { get; set; }
        public bool OpenFileOnCompletion { get; set; }
        public bool CopyToClipboard { get; set; }
        public bool IncludeMessage { get; set; }
        public bool RemoveComments { get; set; }
        public bool CreateFile { get; set; }
        public bool IncludeOtherFiles { get; set; }
        public bool ShowPerFileTokens { get; set; }
        public bool DistillProject { get; set; }
        public bool DistillUnused { get; set; }
        public bool DistillUnusedHeaders { get; set; }
        public bool ExcludeProjectSettings { get; set; }
    }

    public enum ScraperOption
    {
        [Description("Code Files")] CodeFiles,
        [Description("Config Files")] ConfigFiles,
        [Description("Designer Files")] DesignerFiles,
        [Description("Include Other Files")] IncludeOtherFiles,
        [Description("Json Files")] JsonFiles,
        [Description("Project Files")] ProjectFiles,
        [Description("Resource Files")] ResourceFiles,
        [Description("Distill Project (Bible Mode)")] DistillProject,
        [Description("Distill Unused")] DistillUnused,
        [Description("Distill Active Projects Only")] DistillUnusedHeaders,
        [Description("Exclude Project Settings")] ExcludeMyProject,
        [Description("Remove Comments")] RemoveComments,
        [Description("Sanitize Files (Recommended)")] SanitizeFiles,
        [Description("Squish Designer Files")] SquishDesignerFiles,
        [Description("Copy To Clipboard")] CopyToClipboard,
        [Description("Create File")] CreateFile,
        [Description("Include Message")] IncludeMessage,
        [Description("Include Project Structure")] IncludeProjectStructure,
        [Description("Open File On Completion")] OpenFileOnCompletion,
        [Description("Open Folder On Completion")] OpenFolderOnCompletion
    }
}