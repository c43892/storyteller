using System;
using System.IO;
using System.IO.Compression;

namespace Recognissimo.Core
{
    /// <summary>
    /// Model source for loading model from zip archive
    /// </summary>
    public class ZipModelSource : IModelSource
    {
        private readonly string _modelEntry;
        private readonly string _zip;

        /// <summary>
        /// Initializes new instance from specified zip archive
        /// </summary>
        /// <param name="zip">Path to the zip archive</param>
        /// <param name="modelEntry">Zip archive model entry</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public ZipModelSource(string zip, string modelEntry = "/")
        {
            if (string.IsNullOrEmpty(zip)) throw new ArgumentNullException($"Zip file path '{zip}' is null or empty");
            if (!File.Exists(zip)) throw new FileNotFoundException($"Zip file '{zip}' does not exist.");

            _zip = Path.GetFullPath(zip);
            _modelEntry = FixEntryName(modelEntry);
            ModelName = _modelEntry == "/" ? Path.GetFileNameWithoutExtension(zip) : Path.GetFileName(modelEntry);
        }

        /// <inheritdoc />
        public string ModelName { get; }

        /// <inheritdoc />
        public void SaveTo(string to)
        {
            ForEveryZipEntry(entry =>
            {
                if (entry.Name.Length == 0 || !entry.FullName.StartsWith(_modelEntry)) return;

                var entryName = entry.FullName.Replace(_modelEntry, "");
                var destinationPath = Path.Combine(to, entryName);

                var dirName = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                if (File.Exists(destinationPath)) File.Delete(destinationPath);

                entry.ExtractToFile(destinationPath);
            });
        }

        private void ForEveryZipEntry(Action<ZipArchiveEntry> action)
        {
            using (var archive = ZipFile.OpenRead(_zip))
            {
                foreach (var entry in archive.Entries) action(entry);
            }
        }

        private static string FixEntryName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "/";
            name = name.Replace('\\', '/');
            if (name[name.Length - 1] != '/')
                name += '/';
            if (name[0] == '/')
                name = name.Substring(1);

            return name;
        }
    }
}