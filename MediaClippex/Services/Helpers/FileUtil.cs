using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaClippex.Services.Helpers;

public static class FileUtil
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

    public static bool Copy(string source, string destination)
    {
        try
        {
            if (File.Exists(source))
            {
                File.Copy(source, destination, true);
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }
}