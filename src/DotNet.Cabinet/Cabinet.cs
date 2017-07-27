using Microsoft.Extensions.FileProviders;
using Dazinator.AspNet.Extensions.FileProviders;

namespace DotNet.Cabinets
{
    public class Cabinet : ICabinet
    {

        public Cabinet(IFileStorageProvider fileStorage, IFileProvider additionalFileProvider = null)
        {
            Storage = fileStorage;
            if (additionalFileProvider != null)
            {
                FileProvider = new CompositeFileProvider(fileStorage.FileProvider, additionalFileProvider);
            }
            else
            {
                FileProvider = fileStorage.FileProvider;
            }
            StorageInfo = new StorageInfo(Storage.FileProvider);
        }

        public IFileProvider FileProvider { get; set; }

        public IFileStorageProvider Storage { get; set; }

        public StorageInfo StorageInfo { get; set; }

    }



}