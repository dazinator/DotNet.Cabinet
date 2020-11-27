using Dazinator.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet.Cabinets
{
    public class StorageInfo
    {


        private readonly IFileProvider _storageFileProvider;

        public StorageInfo(IFileProvider storageFileProvider)
        {
            _storageFileProvider = storageFileProvider;
        }

        public long CalculateUsedStorageSize()
        {
            var allFiles = _storageFileProvider.Search("**/*");
            return SumFileSize(allFiles);
        }

        public long CalculateUsedStorageSize(params string[] includePatterns)
        {
            var allFiles = _storageFileProvider.Search(includePatterns);
            return SumFileSize(allFiles);
        }

        public long CalculateUsedStorageSize(string[] includePatterns, params string[] excludePatterns)
        {
            var allFiles = _storageFileProvider.Search(includePatterns, excludePatterns);
            return SumFileSize(allFiles);

        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        public static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }

        private long SumFileSize(IEnumerable<Tuple<string, IFileInfo>> allFiles)
        {
            long totalSize = 0;
            foreach (var file in allFiles)
            {
                if (!file.Item2.IsDirectory && file.Item2.Exists)
                {
                    totalSize = totalSize + file.Item2.Length;
                }
            }
            return totalSize;
        }



    }
}
