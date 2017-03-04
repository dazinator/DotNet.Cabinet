# DotNet.Cabinet
Cabinet, is a virtual file system API for netstandard platforms, that allows you to create file systems, with optional isolation.

It uses `Microsoft.Extensions.FileProviders.IFileProvider`s for read access to the virutal directory - so you can implement your own 
IFileProviders to integrate read access to file from any source (such as google drive, one drive, etc). I have some additional IFileProvider implementations here that you can also use: https://github.com/dazinator/Dazinator.AspNet.Extensions.FileProviders

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


```
 var subpath = "/foo/bar/";
 cabinet.Storage.CreateFile(new StringFileInfo("super content", "baz.txt"), fileDir);

```

The virtual directory of the cabinet now looks like:

```
/foo/bar/baz.txt
```

To create files, you can pass in any `IFileInfo`. An `IFileInfo` is a standard microsoft abstraction for providing read only access to file information.

For example, `StringFileInfo` let's you create a file by directly supplying it's string content:

There are also:

 - EmbeddedFileInfo
 - MemoryStreamFileInfo
 - WrappedFileInfo (Let's you wrap an existing IFileInfo from elsewhere, but override it's file name or other detail)

Behind the scenes, the PhysicalStorageProvider will have written it here:

`C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/foo/bar/baz.txt`

In terms of getting read access to files from the cabinets virual directory, you use the cabinets `IFileProvider` for that, like so:


```

 var existingFile = cabinet.FileProvider.EnsureFile("/foo/bar/baz.txt"); // EnsureFile is a handy extension method.
 var contents = existingFile.ReadAllContent();
 Assert.Equal(contents, "super content");

```

Notice, the consumer of the cabinet doesn't care about where the file physically lives - it only cares about its subpath / request path within the virtual directory.

You may be wondering what the benefit of this is?

## Including files from additional sources into the virtual directory.

If you would like your cabinet to also include other files into its virtual directory:

```
IFileProvider otherSourcesFileProvider  = new CompositeFileProvider(new GoogleDriveFileProvider(options), new OneDriveFileProvider(oneDriveOptions));
var cabinet = new Cabinet(cabinetStorage, otherSourcesFileProvider);

```

Now, when you are using this cabinet, it's virtual directory will consist of the additional files provider from the `IFileProvider` that we passed in. So for example, if you have a "/foo/bar.txt" on google drive, and a "baz.txt" on onedrive, and you also used the cabinet to create a new file "/bat.txt" - then the virtual directory for the cabinet would look like this:

```
/bat.txt
/foo/bar.txt
/baz.txt
```

Where `bat.txt` comes from the IFileProvider that sits over the cabinets physical storage provider - i.e `C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/bat.txt`and the other files are sourced from the additional `IFileProviders` for google drive and one drive.

## Hooking up with ASP.NET static files

The cabinet exposes it's `IFileProvider` and you can easily hook this into `IHostingEnvironment`'s Content, or WebRoot file provider.
You could also hook it into static files middleware options.

## Isolation

The real benefit starts to show when we want to start using isolation.

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
The system cabinet might already have a file "/foo.txt" also.

Each module is accessing files in isolation, which means no conflicts occur.
There will be no clashes, because, each seperate cabinet has isolated file storage.

Let's say that Module A also needs read access to system level files though.

```
            
            ICabinet moduleACabinet = new Cabinet(moduleAStorage, systemCabinet.FileProvider);

```

Now, Module A's cabinet has files provided by the system cabinet, in it's virtual directory.

Now, let's say the system file cabinet has a file called "/foo.txt", and let's say Module A wants to amend it.
 
The virtual directory for module A would look like this:

```
/foo.txt <-- comes from system cabinet IFileProvider

```

Module A can create it's own version of this file, but it cannot modify the system version, unless it accesses the file using the system cabinet.

So module A can create its own version like so:

```
 moduleACabinet.Storage.CreateFile(new StringFileInfo("super content", "foo.txt"));
 ```
 
 Now it's vitual directory looks like this:

```
/foo.txt <-- comes from module A's physical storage
/foo.txt <-- comes from system cabinet IFileProvider

```

Because the file from module A's physical storage is higher in precedence, it essentially overrides the file thats resolvable via the 
systems IFileProvider within the virtual directory.

This is because, behind the scenes, the system file physcally lives at:

```
`C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/foo.txt`
```

Where as the physical storage provider for ModuleA will create the overriding file here:

```
`C:/Foo/cbf0c93a-8840-46ba-921c-b85e81265c81/foo.txt`
```

So now, when module A requests "/foo.txt" via its cabinet, it will get back it's own version of the file.

This offers a useful safety feature in that, Module A cannot delete system level files, only override them.

## StorageInfo

Some helpful methods are provided so that you can query total file size of cabinets.

```
            var storageInfo = cabinet.StorageInfo;
            long size = storageInfo.CalculateUsedStorageSize();
            var readable = StorageInfo.GetBytesReadable(size); // returns "24 B"
```

You can also query total file size, using filter patterns (using a glob pattern supported by [DotNet.Glob](https://github.com/dazinator/DotNet.Glob)) like so:

```
            var storageInfo = cabinet.StorageInfo;
            long size = storageInfo.CalculateUsedStorageSize("**/*some?[2-5].txt");
            var readable = StorageInfo.GetBytesReadable(size); // returns readable version of size, i.e "24 B"
```

You can pass in a combination of multiple include and exclude patterns when doing this.
