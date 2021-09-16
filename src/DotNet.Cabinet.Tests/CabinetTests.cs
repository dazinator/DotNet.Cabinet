using Dazinator.Extensions.FileProviders;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace DotNet.Cabinets.Tests
{
    public class CabinetTests
    {
        [Fact]
        public void Can_Add_File_From_FileInfo()
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

            // Ensure we can read the file to verify it was created.
            var existingFile = cabinet.FileProvider.EnsureFile(fileDir + "baz.txt");
            var contents = existingFile.ReadAllContent();
            Assert.Equal("super content", contents);

        }

        [Fact]
        public void Can_Add_File_From_FileInfo_And_Null_Directory()
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            string fileDir = null;
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

            // Ensure we can read the file to verify it was created.
            var existingFile = cabinet.FileProvider.EnsureFile("baz.txt");
            var contents = existingFile.ReadAllContent();
            Assert.Equal(contents, "super content");

        }

        [Fact]
        public void Cannot_Add_File_From_Null_FileInfo()
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";

            Assert.Throws<ArgumentNullException>(
              () => cabinet.Storage.CreateFile(null, fileDir));
        }

        [Fact]
        public void Cannot_Add_File_From_DirectoryFileInfo()
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";

            Assert.Throws<ArgumentOutOfRangeException>(
              () => cabinet.Storage.CreateFile(new DirectoryFileInfo("baz"), fileDir));

        }

        [Theory]
        [InlineData("../")]
        [InlineData("/../some/")]
        [InlineData("/some/../../")]
        public void Cannot_Add_File_Above_Root(string dir)
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);

            Assert.Throws<ArgumentException>(
                () => cabinet.Storage.CreateFile(new StringFileInfo("hi there", "baz.txt"), dir));

        }


        [Fact]
        public void Can_Read_From_File_And_Write_To_File()
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

            // Now ma
            var filePath = fileDir + "baz.txt";
            var existingFile = cabinet.FileProvider.EnsureFile(filePath);


            cabinet.Storage.ReplaceFileContents(filePath, (content) =>
            {
                return content + " read";
            });

            //// read each line of content from the file, and write it back with " read" appended".
            //using (var readStream = new StreamReader(existingFile.CreateReadStream()))
            //{
            //    cabinet.Storage.OpenWrite(filePath, (writeStream) =>
            //    {
            //        using (var writer = new StreamWriter(writeStream))
            //        {
            //            while (!readStream.EndOfStream)
            //            {
            //                var readLine = readStream.ReadLine();
            //                writer.WriteLine(readLine + " read");
            //            }

            //            writer.Flush();
            //        }
            //    });

            //}

            var updatedFile = cabinet.FileProvider.EnsureFile(filePath);

            // make sure the contents have been amended.
            var contents = updatedFile.ReadAllContent();
            Assert.Equal(contents, "super content read");

        }

        [Theory]
        [InlineData("/foo/bar/")]
        [InlineData("/.testFiles")]      
        public void Can_Override_File(string fileDir)
        {

            var partitionId = Guid.NewGuid();

            string currentDir = GetCurrentDir();

            // system
            var systemFilesPartionId = Guid.NewGuid();
            var systemStorage = new PhysicalFileStorageProvider(currentDir, partitionId);
            ICabinet system = new Cabinet(systemStorage);

            // create a system file /foo/bar/baz.txt
           // var fileDir = "/foo/bar/";
            system.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

            // Module A has access to system, and so can override system.
            var moduleAPartitionId = Guid.NewGuid();
            var moduleAStorage = new PhysicalFileStorageProvider(currentDir, moduleAPartitionId);
            ICabinet moduleA = new Cabinet(moduleAStorage, system.FileProvider);

            var filePath = fileDir.TrimEnd('/') + '/' + "baz.txt";
            var systemFile = moduleA.FileProvider.EnsureFile(filePath);
            var contents = systemFile.ReadAllContent();
            Assert.Equal("super content", contents);

            // now override it for moduleA.
            moduleA.Storage.CreateFile(new StringFileInfo("MODULE A OVERRIDE", "baz.txt"), fileDir);

            var updatedFile = moduleA.FileProvider.EnsureFile(filePath);
            contents = updatedFile.ReadAllContent();
            Assert.Equal("MODULE A OVERRIDE", contents);

            // make sure system module unmodified.
            updatedFile = system.FileProvider.EnsureFile(filePath);
            contents = updatedFile.ReadAllContent();
            Assert.Equal("super content", contents);

            // clean up.
            moduleA.Storage.DeleteFile(Path.Combine(fileDir, "baz.txt"));
            system.Storage.DeleteFile(Path.Combine(fileDir, "baz.txt"));
            //var systemFile = moduleACabinet.FileProvider.GetFileInfo("/foo/bar/baz.txt");
        }

        //[Fact]
        //public void Can_Override_PhysicalFileProvider_File()
        //{

        //    Guid partitionId = Guid.NewGuid();
        //    string currentDir = GetCurrentDir();

        //    PhysicalFileProvider systemFiles = new PhysicalFileProvider(currentDir);

        //    // Module A has access to system, and so can override system.
        //    Guid moduleAPartitionId = new Guid("cbf0c93a-8840-46ba-921c-b85e81265c81");

        //    var dir = Path.Combine(currentDir, ".testFiles");
        //    if(!Directory.Exists(dir))
        //    {
        //        System.IO.Directory.CreateDirectory(dir);
        //    }
          
        //    var writer = System.IO.File.CreateText(Path.Combine(dir, "Test.txt"));
        //    writer.Write("From Disk");
        //    writer.Close();

        //    PhysicalFileStorageProvider moduleAStorage = new PhysicalFileStorageProvider(currentDir, moduleAPartitionId);
        //    ICabinet moduleA = new Cabinet(moduleAStorage, systemFiles);
            
        //    var filePath = "/.testFiles/Test.txt";
        //    IFileInfo systemFile = moduleA.FileProvider.EnsureFile(filePath);
        //    string contents = systemFile.ReadAllContent();
        //    Assert.Equal(contents, "From Disk");

        //    // now override it for moduleA.
        //    moduleA.Storage.CreateFile(new StringFileInfo("MODULE A OVERRIDE", "Test.txt"), "/.testFiles");

        //    IFileInfo updatedFile = moduleA.FileProvider.EnsureFile(filePath);
        //    contents = updatedFile.ReadAllContent();
        //    Assert.Equal(contents, "MODULE A OVERRIDE");

        //    // make sure system module unmodified.
        //    updatedFile = systemFiles.EnsureFile(filePath);
        //    contents = updatedFile.ReadAllContent();
        //    Assert.Equal(contents, "From Disk");
        //}

        [Fact]
        public void Cannot_OpenFile_For_Write_Whilst_Being_Read()
        {

            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

            // Now ma
            var filePath = fileDir + "baz.txt";
            var existingFile = cabinet.FileProvider.EnsureFile(filePath);


            // read each line of content from the file, and write it back with " read" appended".
            using (var readStream = new StreamReader(existingFile.CreateReadStream()))
            {
                Assert.Throws<IOException>(() =>
                {
                    cabinet.Storage.OpenWrite(filePath, (writeStream) =>
                    {
                        using (var writer = new StreamWriter(writeStream))
                        {
                            while (!readStream.EndOfStream)
                            {
                                var readLine = readStream.ReadLine();
                                writer.WriteLine(readLine + " read");
                            }
                            writer.Flush();
                        }
                    });
                });

            }

        }

        [Fact]
        public void Can_Delete_File()
        {
            // Arrange
            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);


            // Act
            cabinet.Storage.DeleteFile(fileDir + "baz.txt");


            // Assert
            Assert.Throws<FileNotFoundException>(() =>
            {
                var existingFile = cabinet.FileProvider.EnsureFile(fileDir + "baz.txt");
            });


        }

        [Fact]
        public void Can_Get_Storage_Info()
        {
            // Arrange
            string currentDir = GetCurrentDir();

            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider(currentDir, partitionId);

            var cabinet = new Cabinet(cabinetStorage);
            var fileDir = "/foo/bar/";
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

            var storageInfo = cabinet.StorageInfo;
            var size = storageInfo.CalculateUsedStorageSize();

            // add another file            
            cabinet.Storage.CreateFile(new StringFileInfo("super content", "bat.txt"), fileDir);

            // get size
            var newSize = storageInfo.CalculateUsedStorageSize();

            Assert.Equal(size * 2, newSize);
            var readable = StorageInfo.GetBytesReadable(newSize);


        }


        private string GetCurrentDir()
        {
            var typeInfo = typeof(CabinetTests).GetTypeInfo();
            var currentDir = typeInfo.Assembly.Location;
            currentDir = Path.GetDirectoryName(currentDir);
            return currentDir;
        }
    }
}
