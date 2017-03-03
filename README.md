# DotNet.Cabinet
Cabinet, is a virtual file system API for netstandard platforms, that allows you to create file systems, with optional isolation.

It uses `Microsoft.Extensions.FileProviders.IFileProvider`s for read access to the virutal directory - so you can implement your own 
IFileProviders to integrate read access to file from any source (such as google drive, one drive, etc)

## Getting started

Create a storage. This defines where files will be physically written too.
In this case, files will be written to `C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/`:

```
            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider("C:/Foo/", partitionId);

```

Next, create a cabinet that uses that storage provider.

```

var cabinet = new Cabinet(cabinetStorage);

```

Now, you can create / update and delete files from the cabinet.
To create files, you can pass in any `IFileInfo` which supplies the file name and content information.
For example, StringFileInfo let's you create a file by directly supplying it's string content:


```
 var subpath = "/foo/bar/";
 cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

```

There are also:

 - EmbeddedFileInfo
 - MemoryStreamFileInfo
 - WrappedFileInfo

As we are using physical storage, with a partion id that is a GUID, the resulting file in the above example, will be placed physically here:

`C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/foo/bar/baz.txt`

In terms of reading the file, the file is exposed via an `IFileProvider`like so:


```

 var existingFile = cabinet.FileProvider.EnsureFile("/foo/bar/baz.txt");
 var contents = existingFile.ReadAllContent();
 Assert.Equal(contents, "super content");

```

You may be wondering what the benefit of this is?

## Including files from additional sources

If you would like your cabinet to also expose files from other sources:

```
IFileProvider additionalVirtualFilesProvider  = new CompositeFileProvider(new GoogleDriveFileProvider(options), new OneDriveFileProvider(oneDriveOptions));
var cabinet = new Cabinet(cabinetStorage, additionalVirtualFilesProvider);

```

Now, when you are using the cabinet, it will have read access to any files provided by the additional `IFileProvider,
those files will be unified with the files from the `cabinetStorage` (in this case a `PhysicalFileStorageProvider`) and
that constitues the virtual directory for the cabinet.

## Hooking up with ASP.NET static files

The cabinet exposes it's `IFileProvider` and you can easily hook this into `IHostingEnvironment`'s Content, or WebRoot file provider.
You could also hook it into static files middleware options.

## Isolation

Let's say you are building a modular system, where you want:

- System Level Files
- Module A level Files
- Module B level Files.


You can achieve this with cabinet by:

```

            var rootPhysicalStoragePath = "C:/Cabinet/";
            
            var systemFilesPartionId = new Guid("43bf6778-ff16-41c8-a72c-cd319d84b8bb");
            var systemStorage = new PhysicalFileStorageProvider(rootPhysicalStoragePath, partitionId);
            ICabinet systemCabinet = new Cabinet(systemStorage);
            
            var moduleAPartitionId = new Guid("cbf0c93a-8840-46ba-921c-b85e81265c81");
            var moduleAStorage = new PhysicalFileStorageProvider(rootPhysicalStoragePath, moduleAPartitionId);
            ICabinet moduleACabinet = new Cabinet(moduleAStorage);
                        
            var moduleBPartitionId = new Guid("124b086f-e2fa-4998-a5a0-ee6d5dee6efe");
            var moduleBStorage = new PhysicalFileStorageProvider(rootPhysicalStoragePath, moduleBPartitionId);
            ICabinet moduleBCabinet = new Cabinet(moduleBStorage);

```

Now, module A can create a file called "/foo.txt" using the `moduleACabinet`.
Module B can create a file also called "/foo.txt" using the `moduleBCabinet`

The system cabinet might already have a file "/foo.txt"

There will be no clashes, because, each seperate cabinet has isolated file storage.

Let's say that Module A also needs read access to the system files.

```
            
            ICabinet moduleACabinet = new Cabinet(moduleAStorage, systemCabinet.FileProvider);

```

Now, let's say the system file cabinet has a file called "/foo.txt". 
Let's say Module A, doesn't like the look of it.

Module A can create it's own version of the "/foo.txt" file without effecting the system version. 

```

 moduleACabinet.Storage.CreateFile(new StringFileInfo("super content", "foo.txt"));

```

Now when module A requests "/foo.txt" via its cabinet, it will get back it's own version (from its partition) rather than the system
level version from the system cabinet.







