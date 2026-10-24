using System.IO;

namespace LastBell
{
    // Selects a namespace only; never creates, deletes, migrates or resets a save.
    public static class SaveDirectorySelector
    {
        public static string Select(string persistentDirectory,string[] arguments,bool dragon092Playtest)
        {
            string directory=dragon092Playtest?Path.Combine(persistentDirectory,"Playtests","dragon-0.9.2"):persistentDirectory;
            if(arguments!=null)for(int i=0;i+1<arguments.Length;i++)
                if(arguments[i]=="-lastbell-save-dir")directory=Path.GetFullPath(arguments[i+1]);
            return directory;
        }
    }
}
