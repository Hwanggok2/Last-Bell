namespace LastBell.Editor
{
    public static class FeedbackV85Migration
    {
        public static void ApplyAndBuild()
        {
            PresentationArtImporter.MigrateExisting();
            MvpBuild.Windows();
        }
    }
}
