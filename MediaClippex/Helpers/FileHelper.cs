using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaClippex.Helpers;

public static class FileHelper
{
    public static void Delete(string? path, bool checkIfExists = true)
    {
        if (checkIfExists && path != null && File.Exists(path)) File.Delete(path);
    }

    public static void DeleteBatch(IEnumerable<string> paths, bool checkIfExists = true)
    {
        foreach (var path in paths) Delete(path, checkIfExists);
    }

    public static string FixFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars()
            .Aggregate(fileName, (current, invalidChar) => current.Replace(invalidChar, '_'));
    }
}