namespace Code_Crammer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Code_Crammer.Data.Forms_Classes.frmMain());
        }
    }
}