<img src="docs/icon.png" width="96" align="right" alt="">

# Disk Atlas

Windows disk space analyzer that maps an entire NTFS volume in seconds by parsing the Master File Table directly instead of walking directories.

## Why it is fast

A conventional folder scan asks the file system about one directory at a time, which costs a seek for every folder on the volume. Disk Atlas reads the `$MFT` instead — the table NTFS keeps of every file on the volume — as one mostly contiguous stream, and rebuilds the directory tree from the parent references it finds there.

Getting that right means handling a few things the file system normally hides:

- **Update sequence fixups.** NTFS overwrites the last two bytes of every sector in a record with a sequence number so torn writes can be detected. The real bytes live in the update sequence array and have to be put back before anything in the record can be trusted.
- **Run lists.** Non resident attributes describe their extents as chained runs where each offset is signed and relative to the previous one. This is also how the `$MFT` describes its own location.
- **Sizes that are not the obvious ones.** Compressed and sparse files occupy less on disk than their logical size, resident files live inside their own record and occupy no clusters at all, and a file with several hard links appears once in the table, so its data is counted once.

## Status

Phase 1 of three. What works today:

- [x] NTFS boot sector, `$MFT` run list and FILE record parsing
- [x] Directory tree rebuilt from parent references, with sizes rolled up
- [x] Folder tree and contents browser, sorted largest first
- [ ] Treemap visualization
- [ ] Duplicate finder using staged hashing
- [ ] Snapshot comparison, to show what has grown since last time

## Building

Needs the .NET 9 SDK. Visual Studio 2022 17.8 or newer can open `DiskAtlas.sln` directly; the form has a normal designer file, so it opens in the Windows Forms designer.

```
dotnet build DiskAtlas.sln
dotnet run --project src/DiskAtlas
```

## Tests

The parsers are checked against synthetic NTFS structures, including a record whose
fixups are deliberately broken:

```
dotnet run --project tests/DiskAtlas.Tests
```

## Administrator rights

Reading the Master File Table needs a raw volume handle, which Windows only grants to an elevated process. The application starts unelevated so it stays easy to debug, and offers to restart itself when a scan begins.

## Layout

```
src/DiskAtlas/Ntfs/        boot sector, run lists, FILE records, table walker
src/DiskAtlas/Scanning/    tree building and the scan entry point
src/DiskAtlas/Model/       volumes, raw records, the finished tree
src/DiskAtlas/Native/      the Win32 calls needed for raw volume access
tests/DiskAtlas.Tests/     parser checks against synthetic structures
```

## License

MIT, see [LICENSE](LICENSE).
