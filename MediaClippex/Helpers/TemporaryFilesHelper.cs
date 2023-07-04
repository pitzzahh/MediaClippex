using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaClippex.Helpers;

public static class TemporaryFilesHelper
{
    private static List<string> TemporaryFiles { get; set; } = new();
    
    public static void AddTemporaryFile(string path)
    {
        TemporaryFiles.Add(path);
    }
    
    public static void DeleteTemporaryFiles()
    {
        foreach (var temporaryFile in TemporaryFiles.Where(File.Exists))
        {
            File.Delete(temporaryFile);
        }
    }
}