namespace LastBell.Editor
{
    public static class FeedbackV83Migration
    {
        public static void ApplyAndBuild()
        {
            PresentationArtImporter.MigrateExisting();
            ReferenceV81Migration.Apply();
            MvpBuild.Windows();
        }
    }
}
