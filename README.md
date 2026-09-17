# Jolly Roger's Directory Sync

A small Windows desktop utility for one-way, mirror-style synchronization of a source
directory to a destination directory.

## What it does

Point it at a **source** and a **destination** folder and it makes the destination match
the source:

- Copies new or changed files (based on last-write time) from source to destination.
- Deletes files and subdirectories in the destination that no longer exist in the source.
- Recurses into subdirectories, processing several in parallel.
- Checks that the destination has enough free space before starting.
- Refuses to run if the source and destination are the same directory, or if one is
  nested inside the other.

This is a one-way mirror: the destination is made to look like the source. Nothing is
ever written back to the source.

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or the .NET 10 Desktop Runtime to
  just run a published build)

## Building and running

```
dotnet build DirectorySync.sln
dotnet run --project DirectorySync
```

Or open `DirectorySync.sln` in Visual Studio and press F5.

## Using the app

1. **Select Source** / **Select Destination** — pick the two folders, or type/paste the
   paths directly into the text boxes.
2. **Test Mode** — when checked, the app logs everything it *would* do without copying,
   deleting, or creating anything. Useful for a dry run before trusting it with real data.
3. **Syncronize Directories** — starts the sync. A progress bar tracks file count, and the
   log pane reports each copy/delete/skip as it happens.
4. **Cancel** — requests that a running sync stop after finishing its current directory.
5. **Clear Log** — clears the log pane and resets the progress bar.

## Notes and limitations

- Comparison is based on file last-write time only, not content/hash.
- Windows `MAX_PATH` (260 characters) limits are checked and logged for destination file
  paths that exceed it.
- The free-space check is a conservative estimate (total source size vs. destination free
  space + current destination usage), not an exact accounting of the incremental copy.
