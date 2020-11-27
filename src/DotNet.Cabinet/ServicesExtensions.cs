using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet.Cabinets
{
    public static class ServicesExtensions
    {

        public static ICabinet AddPhysicalFileCabinet(
            this IServiceCollection services,
            string rootPath, Guid partitionId, IFileProvider sharedAccessFileProvider = null)
        {
            var cabinet = CreatePhysicalCabinet(rootPath, partitionId, sharedAccessFileProvider);
            services.AddSingleton<ICabinet>(cabinet);
            return cabinet;
        }

        public static Cabinet CreatePhysicalCabinet(string rootPath, Guid partitionId, IFileProvider sharedAccessFileProvider = null)
        {
            var storage = new PhysicalFileStorageProvider(rootPath, partitionId);
            var cabinet = new Cabinet(storage, sharedAccessFileProvider);
            return cabinet;
        }

    }
}
