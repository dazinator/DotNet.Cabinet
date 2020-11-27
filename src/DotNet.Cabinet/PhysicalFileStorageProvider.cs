using System;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DotNet.Cabinets
{
    public class PhysicalFileStorageProvider : IFileStorageProvider
    {

        public PhysicalFileStorageProvider(string rootDir, Guid? partitionId)
        {
            Initialise(rootDir, partitionId);
        }

        public Guid? PartitionId { get; set; }

        public IFileProvider FileProvider { get; set; }

        public string RootPath { get; set; }

        private void Initialise(string rootDir, Guid? partitionId)
        {
            if (!System.IO.Directory.Exists(rootDir))
            {
                throw new System.IO.DirectoryNotFoundException(rootDir);
            }

            var cabinetPath = partitionId == null ? rootDir : System.IO.Path.Combine(rootDir, partitionId.ToString());
            if (!System.IO.Directory.Exists(cabinetPath))
            {
                System.IO.Directory.CreateDirectory(cabinetPath);
            }

            PartitionId = partitionId;
            RootPath = cabinetPath;
            FileProvider = new PhysicalFileProvider(cabinetPath);

        }     

        public void ReplaceFileContents(string filePath, Func<String, string> replacementFunction)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            PathString subPath = new PathString(filePath);
            string physicalPath = GetPhysicalPath(subPath);

            // Create the file if it doesn't exist.
            if (!File.Exists(physicalPath))
            {
                throw new InvalidOperationException("File doesn't exist.");
                //var newContents = replacementFunction(string.Empty);
                //var fileName = Path.GetFileName(physicalPath);
                //var dir = Path.GetDirectoryName(physicalPath);
                //var newFile = new StringFileInfo(newContents, fileName);
                //CreateFile(new StringFileInfo(newContents), dir);
                //return;
            }

            // Overwrite existing file.
            using (FileStream fileStream = new FileStream(
                    physicalPath, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None))
            {
                StreamReader streamReader = new StreamReader(fileStream);
                string currentContents = streamReader.ReadToEnd();
                var newContents = replacementFunction(currentContents);
                fileStream.SetLength(0);
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(newContents);
                    // writer.Close();
                }

            }
        }

        public async Task CreateFileAsync(IFileInfo file, string dir = "/")
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (file.IsDirectory)
            {
                throw new ArgumentOutOfRangeException("file");
            }

            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = "/";
            }

            PathString subPath = new PathString(dir);
            subPath = subPath + "/" + file.Name;

            string fullPhysciaPath = GetPhysicalPath(subPath);
            var directory = Path.GetDirectoryName(fullPhysciaPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = File.Create(fullPhysciaPath))
            {
                using (var inputStream = file.CreateReadStream())
                {
                    if (inputStream.CanSeek)
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                    }
                   await inputStream.CopyToAsync(fileStream);
                }
            }
        }


        public void CreateFile(IFileInfo file, string dir = "/")
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (file.IsDirectory)
            {
                throw new ArgumentOutOfRangeException("file");
            }

            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = "/";
            }

            PathString subPath = new PathString(dir);
            subPath = subPath + "/" + file.Name;


            string fullPhysciaPath = GetPhysicalPath(subPath);
            var directory = Path.GetDirectoryName(fullPhysciaPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = File.Create(fullPhysciaPath))
            {
                using (var inputStream = file.CreateReadStream())
                {
                    if (inputStream.CanSeek)
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                    }
                    inputStream.CopyTo(fileStream);
                }
            }
        }

        private static readonly char[] _trimStartChars = new char[] { '/' };

        private string GetPhysicalPath(string path)
        {
            path = path.TrimStart('/').Replace('/', '\\');

            var targetPath = Path.Combine(RootPath, path);

            bool isUnderRoot = EnsureTargetIsUnderRoot(targetPath);
            if (!isUnderRoot)
            {
                throw new ArgumentException(path + " is outside of root.");
            }

            //   var fullPath = System.IO.Path.Combine(RootPath, path.Replace('/', '\\'));
            return targetPath;
        }

        private bool EnsureTargetIsUnderRoot(string targetPath)
        {
            DirectoryInfo root = new DirectoryInfo(RootPath);
            DirectoryInfo target = new DirectoryInfo(targetPath);
            while (target.Parent != null)
            {
                if (target.Parent.FullName == root.FullName)
                {
                    return true;
                }
                else
                {
                    target = target.Parent;
                }
            }

            return false;

        }

        public void OpenWrite(string path, Action<Stream> writeAction)
        {
            if (writeAction == null)
            {
                throw new ArgumentNullException(nameof(writeAction));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            PathString subPath = new PathString(path);
            string physicalPath = GetPhysicalPath(subPath);

            using (var stream = File.OpenWrite(physicalPath))
            {
                writeAction(stream);
            }

        }

        public async Task OpenWriteAsync(string path, Func<Stream, Task> writeAsync)
        {
            if (writeAsync == null)
            {
                throw new ArgumentNullException(nameof(writeAsync));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            PathString subPath = new PathString(path);
            string physicalPath = GetPhysicalPath(subPath);

            using (var stream = File.OpenWrite(physicalPath))
            {
                await writeAsync(stream);
            }

        }


        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            PathString subPath = new PathString(filePath);
            string physicalPath = GetPhysicalPath(subPath);

            File.Delete(physicalPath);


        }

    }



}