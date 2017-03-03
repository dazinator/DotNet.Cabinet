# DotNet.Cabinet
Cabinet, is a virtual file system API for netstandard platforms, that allows you to create file systems, with optional isolation.

It uses `Microsoft.Extensions.FileProviders.IFileProvider`s for read access to the virutal directory - so you can implement your own 
IFileProviders to integrate read access to file from any source (such as google drive, one drive, etc)

## Getting started

Create a `IFileStorageProvider`. This provides the write operations for the virtual directory / cabinet. I currently provide only one impmentation of this, which is `PhysicalFileStorageProvider`.

In the following case, files will be physically written to `C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/`:

```
            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider("C:/Foo/", partitionId);

```

Next, create a cabinet. This represents the virtual file system. It uses the storage provider we just created for its own writeable storage area.

```

var cabinet = new Cabinet(cabinetStorage);

```

Now, you can create / update and delete files from the cabinet.
To create files, you can pass in any `IFileInfo` which supplies the file name and file content.
For example, `StringFileInfo` let's you create a file by directly supplying it's string content:


```
 var subpath = "/foo/bar/";
 cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

```

There are also:

 - EmbeddedFileInfo
 - MemoryStreamFileInfo
 - WrappedFileInfo (Let's you wrap an existing IFileInfo from elsewhere, but override it's file name or other detail)

As we are using a physical storage provider for this cabinet, with a partion id that is a GUID, the resulting file in the above example, will be placed physically here:

`C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/foo/bar/baz.txt`

In terms of reading the file, the file is exposed via an `IFileProvider`like so:


```

 var existingFile = cabinet.FileProvider.EnsureFile("/foo/bar/baz.txt");
 var contents = existingFile.ReadAllContent();
 Assert.Equal(contents, "super content");

```

Notice, the consumer of the cabinet doesn't care about where the file physically lives - they only care about its subpath / request path.

You may be wondering what the benefit of this is?

## Including files from additional sources

If you would like your cabinet to also expose files from other sources:

```
IFileProvider additionalVirtualFilesProvider  = new CompositeFileProvider(new GoogleDriveFileProvider(options), new OneDriveFileProvider(oneDriveOptions));
var cabinet = new Cabinet(cabinetStorage, additionalVirtualFilesProvider);

```

Now, when you are using this cabinet, it's virtual directory will allow you to access any files provided by the additional `IFileProvider` that we passed in, in this case `GoogleDrive` and `OneDrive` - and thats in addition to the files that are already provided from the underlying Storare provider (in this case a `PhysicalFileStorageProvider`) and
that constitues the complete virtual directory for the cabinet.

## Hooking up with ASP.NET static files

The cabinet exposes it's `IFileProvider` and you can easily hook this into `IHostingEnvironment`'s Content, or WebRoot file provider.
You could also hook it into static files middleware options.

## Isolation

The real benefit starts to show when we want to Write files, and use isolation.

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

Each module is accessing files in isolation, means conflicts wont occur.

The system cabinet might already have a file "/foo.txt" also.

There will be no clashes, because, each seperate cabinet has isolated file storage.

Let's say that Module A also needs read access to system level files.

```
            
            ICabinet moduleACabinet = new Cabinet(moduleAStorage, systemCabinet.FileProvider);

```

Now, Module A's cabinet has files provided by the system cabinet, in it's virtual directory.

Now, let's say the system file cabinet has a file called "/foo.txt", and let's say Module A, sees the file "/foo.txt" in it't virtual directory, and doesn't like the look of it.

Module A can create it's own version of the "/foo.txt" file without effecting the system version. 

```

 moduleACabinet.Storage.CreateFile(new StringFileInfo("super content", "foo.txt"));

```

This is because, behind the scenes, the system file physcally lives at:

```
`C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/foo.txt`
```

Where as the physical storage provider for ModuleA creates the file at:

```
`C:/Foo/cbf0c93a-8840-46ba-921c-b85e81265c81/foo.txt`
```

So now, when module A requests "/foo.txt" via its cabinet, it will get back it's own version of the file, as it's own IFileProvider takes precedence, over other IFileProviders that are part of its virtual directory (in this case the IFileProvider from the system files cabinet.)







