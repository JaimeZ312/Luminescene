using System.IO;
using UnityEditor;

/// <summary>
/// Supply common useful usages for working with file directory. 
/// </summary>
public static class DirectoryUtils
{
    public static string ReplaceBackWithForwardSlashes(string s)
    {
        return s.Replace("\\", "/");
    }

    public static void EnsureFolderExists(string folder, bool refreshAfterCreation = false)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);

            if(refreshAfterCreation)
                AssetDatabase.Refresh();
        }
    }
}
