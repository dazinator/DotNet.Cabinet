﻿using System;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace DotNet.Cabinets
{
    public interface IFileStorageProvider
    {
        IFileProvider FileProvider { get; set; }
        Guid? PartitionId { get; set; }

        /// <summary>
        /// Creates a new file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="directory"></param>
        void CreateFile(IFileInfo file, string directory = "/");

        /// <summary>
        /// Opens the file stream for write access. If the same file is currently being read from then this may fail.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="writeAction"></param>
        void OpenWrite(string path, Action<Stream> writeAction);

        /// <summary>
        /// Allows you to modify a files existing contents, whilst ensuring the file is not read or modified by anything else whilst the change is happening.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="replacementFunction"></param>
        void ReplaceFileContents(string filePath, Func<String, string> replacementFunction);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="replacementFunction"></param>
        void DeleteFile(string filePath);
    }
}