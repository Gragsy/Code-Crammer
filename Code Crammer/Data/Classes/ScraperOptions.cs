namespace Code_Crammer.Data
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
    }
}