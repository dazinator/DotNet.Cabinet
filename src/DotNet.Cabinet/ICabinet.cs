using Microsoft.Extensions.FileProviders;

namespace DotNet.Cabinets
{
    public interface ICabinet
    {
        /// <summary>
        /// Provides read access to all files available to the cabinet.
        /// </summary>
        IFileProvider FileProvider { get; set; }

        
        IFileStorageProvider Storage { get; set; }
    }
}