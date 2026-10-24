namespace LastBell.Editor
{
    public static class FeedbackV84Migration
    {
        public static void ApplyAndBuild()
        {
            PresentationArtImporter.MigrateExisting();
            ReferenceV81Migration.Apply();
            MvpBuild.Windows();
        }
    }
}
