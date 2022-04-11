using System.IO;
using System.IO.Compression;

namespace Recognissimo.Utils
{
    public static class Zip
    {
        private static string FixEntryName(string name)
        {
            // canonical/path/
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            
            name = name.Replace('\\', '/');
            
            if (name[name.Length - 1] != '/')
            {
                name += '/';
            }

            if (name[0] == '/')
            {
                name = name.Substring(1);
            }

            return name;
        }

        public static void ExtractZipFolder(string zip, string what, string to)
        {
            using (var archive = ZipFile.OpenRead(zip))
            {
                what = FixEntryName(what);
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.Length == 0 || !entry.FullName.StartsWith(what))
                    {
                        continue;
                    }

                    var entryName = entry.FullName.Replace(what, "");
                    var destinationPath = Path.Combine(to, entryName);

                    var dirName = Path.GetDirectoryName(destinationPath);

                    if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }

                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }

                    entry.ExtractToFile(destinationPath);
                }
            }
        }

        public static long GetEntryLastWriteTime(string zip, string entryName)
        {
            entryName = FixEntryName(entryName);

            using (var archive = ZipFile.OpenRead(zip))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.Equals(entryName))
                    {
                        return entry.LastWriteTime.ToUnixTimeMilliseconds();
                    }
                }
            }

            return -1;
        }
    }
}