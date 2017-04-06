# DotNet.Cabinet
Cabinet, is a netstandard library (for dotnet applications) that provides a layered, virtual directory. It provides a virtual directory, in which files can come agnostically from a composite of different sources (anything you can write an `IFileProvider` for, i.e Physical folders, Google Drive, OneDrive etc). These sources (`IFileProvider`s) are layered, so that for example, a file `/foo.txt` that exists in one source, can override the file `/foo.txt` from another source.

The concept of allowing files placed in one location, to override files from another, is a technique often used by Static Site Generators - i.e you might have a `/system/themes` and a `/user/themes` directory, where files placed in the user directory should override any system level files.

For example, you can use `Cabinet` to build a layered virtual directory, consisting of the physical location `C:/User` as a source for the first layer, then `C:/System` as a source for the second layer, and perhaps GoogleDrive as a source for the third layer. Now, when getting the file `/config.json` from the Cabinet, it will actually attempt to resolve the file first, from `C:/User` then from `C:/System` and then lastly it would fall back to `Google Drive`.

Cabinet uses `Microsoft.Extensions.FileProviders.IFileProvider`s as the abstraction for read access to all these sources - so you can implement your own 
`IFileProvider`s to integrate read access to files from any source (such as google drive, one drive, etc). I have some additional `IFileProvider` implementations here that you might also like to use, such as an InMemory provider: https://github.com/dazinator/Dazinator.AspNet.Extensions.FileProviders

## Before Getting started

Cabinet does not just provide a virtual directory as a read only view of files from various sources. You can also modify these files, within the virual directory, or add new files to the virtual directory - without effecting the files that are in the underlying / original sources. The important point here, is that you can modify files and create files in the virtual directory *even where they have originated from readonly sources*. For example the virtual directory might include files from `GoogleDrive` and `OneDrive` (via relevent `IFileProvider` implementations). However, when you modify these files in the virtual directory, or add new files, those changes will not impact the original files in those sources (i.e the files in GoogleDrive, OneDrive will stay the same). Behind the scenes this works because there is a "top level" source that is included in the Cabinet / virtual directory, and to which, new or modified files are written to, and resolved from with a higher precedence.

For example, if your Cabinet / virtual directory has an `IFileProvider` exposing files from onedrive, then when you were to modify this file in the virtual directory, then this will actually create a copy of the file, which will be resolved with a higher precedence than the original. The 


## Getting Started

Firstly, you need to create an `IFileStorageProvider`. This provides a writeable area for the virtual directory, for which any new files, or modifications will be persisted too, effectively overiding the original files from the original source.

In the following case, file changes will be written to `C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/`:

```
            var partitionId = Guid.NewGuid();
            var cabinetStorage = new PhysicalFileStorageProvider("C:/Foo/", partitionId);

```

Next, create the cabinet. This represents the virtual directory itself. 

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

Note: To create files, you can pass in any `IFileInfo`.

For example, `StringFileInfo` let's you supply the file data as a string.

There are also:

 - EmbeddedFileInfo (Let's you provided file data from an embedded resource)
 - MemoryStreamFileInfo (Let's you provide the file data from a MemoryStream)
 - WrappedFileInfo (Let's you wrap an existing IFileInfo from elsewhere, but override it's file name or other detail)

Behind the scenes, the Cabinet's `PhysicalStorageProvider` will have written it here:

`C:/Foo/43bf6778-ff16-41c8-a72c-cd319d84b8bb/foo/bar/baz.txt`

### Reading a File

In terms of reading a file from the virtual directory, you use the cabinets `IFileProvider`, like so:


```

 var existingFile = cabinet.FileProvider.EnsureFile("/foo/bar/baz.txt"); // EnsureFile is a handy extension method.
 var contents = existingFile.ReadAllContent();
 Assert.Equal(contents, "super content");

```

Notice, the consumer of the cabinet doesn't care about where the files are physically sourced from - it only cares about its path within the virtual directory.

## Including files from additional sources into the virtual directory.

If you would like your cabinet to include files from other sources, in its virtual directory:

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
