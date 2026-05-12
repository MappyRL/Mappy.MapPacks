# Mappy.MapPacks

`Mappy.MapPacks` is the plugin-based map loading system used by the game.
It allows maps to be bundled into standalone assemblies and discovered dynamically at runtime.

This version documents the changes made from the original `Pack.cs` implementation to the newer `PackV1.cs` implementation.

The newer implementation improves:

* Automatic map discovery
* Embedded `.upk` loading
* `.upk.gz` compression support
* Metadata parsing
* Offline support
* Scalability

---

# How It Works

The original implementation worked by:

1. Creating a class library project
2. Referencing `Mappy.MapPacks`
3. Implementing `IMappyMapPack`
4. Embedding `.upk` files directly into the assembly
5. Returning embedded streams directly

The newer `Pack.cs` implementation changed this flow to:

1. Manually registering maps in a dictionary
2. Downloading `.upk` files externally from GitHub Releases
3. Loading metadata and images from embedded resources
4. Returning a `DownloadUrl` instead of an embedded `UpkStream`

The game then loads the assembly and reads all maps exposed by the pack.

---

# Core Interface

```csharp
public interface IMappyMapPack
{
    IEnumerable<MappyPackedMap> GetMaps();
}
```

Every map pack must implement this interface.

---

# MappyPackedMap

Represents a single playable map.

```csharp
public sealed class MappyPackedMap
{
    public string Name { get; init; }

    public Stream UpkStream { get; init; }

    public Stream? ImagePngStream { get; init; }

    public string? Description { get; init; }

    public string? Creator { get; init; }

    public string? Version { get; init; }

    public string? InfoText { get; init; }
}
```

## Properties

| Property         | Description                     |
| ---------------- | ------------------------------- |
| `Name`           | Display name of the map         |
| `UpkStream`      | Required `.upk` map file stream |
| `ImagePngStream` | Optional preview image          |
| `Description`    | Optional text description       |
| `Creator`        | Parsed creator metadata         |
| `Version`        | Parsed version metadata         |
| `InfoText`       | Full raw metadata text          |

---

# Original `Pack.cs`

The original implementation dynamically discovered embedded map resources directly from the assembly.

Maps were loaded through:

```csharp
var resources = asm.GetManifestResourceNames();
```

The original system embedded `.upk` files directly inside the assembly.

## Original System Characteristics

| Feature             | Original `Pack.cs` |
| ------------------- | ------------------ |
| Map Registration    | Automatic          |
| UPK Loading         | Embedded Resource  |
| Compression Support | Yes (`.upk.gz`)    |
| Metadata Parsing    | Dynamic            |
| Version Support     | Yes                |
| Offline Support     | Full               |
| Scalability         | High               |

---

# New GitHub Download Version

The newer implementation manually registers maps and downloads them externally from GitHub Releases.

## GitHub Download System

Instead of embedding `.upk` files directly into the assembly, the newer implementation stores GitHub download links:

```csharp
private static readonly Dictionary<string, string> MapUrls = new()
```

Example:

```csharp
["Aim training"] = "https://github.com/.../Aim_training.upk"
```

Maps are returned with:

```csharp
DownloadUrl = MapUrls[mapName]
```

And:

```csharp
UpkStream = null
```

This means maps are downloaded externally rather than loaded from embedded resources.

---

# Pack Structure

The original `PackV1.cs` implementation automatically discovered embedded resources using a folder convention.

## Original Expected Structure

```text
Maps/
 ├── MyMap/
 │    ├── MyMap.upk
 │    ├── Image.png
 │    ├── description.txt
 │    └── info.txt
 │
 ├── AnotherMap/
 │    ├── AnotherMap.upk.gz
 │    ├── Image.png
 │    ├── description.txt
 │    └── info.txt
```

In the newer `Pack.cs` implementation, `.upk` files are no longer required inside the assembly.

Only metadata resources such as:

* `Image.png`
* `description.txt`
* `info.txt`

need to exist locally.

The actual map files are downloaded externally through GitHub Releases using:

```csharp
DownloadUrl = MapUrls[mapName]
```

This means the pack assembly no longer needs to contain:

```text
<MapName>.upk
```

or:

```text
<MapName>.upk.gz
```

resources.

---

# Resource Discovery

The original `PackV1.cs` implementation scanned all embedded resources and grouped them by the folder directly under `Maps`.

Example resource names:

```text
Mappy.PackTemplate.Maps.MyMap.MyMap.upk
Mappy.PackTemplate.Maps.MyMap.Image.png
Mappy.PackTemplate.Maps.MyMap.description.txt
Mappy.PackTemplate.Maps.MyMap.info.txt
```

The group name (`MyMap`) became the map name exposed to the game.

The newer `Pack.cs` implementation no longer relies on `.upk` resource discovery.

Instead, maps are registered manually:

```csharp
private static readonly Dictionary<string, string> MapUrls = new()
```

The map name is now determined directly from the dictionary key rather than folder grouping.

---

# Required Files

## `.upk` or `.upk.gz`

In the original `Pack.cs` implementation, these files were required because maps were embedded directly into the DLL assembly as embedded resources.

Every `.upk` file had to exist inside the project and be compiled into the DLL itself.

Example:

```text
Maps/MyMap/MyMap.upk
```

This meant the final DLL physically contained all map files.

The newer `Pack.cs` implementation no longer requires embedded `.upk` files.

Instead, maps are downloaded externally from GitHub Releases.

Example:

```csharp
["Aim training"] = "https://github.com/.../Aim_training.upk"
```

This means `.upk` resources no longer need to exist inside:

```text
Maps/<MapName>/
```

folders.

---

# Optional Files

## `Image.png`

Optional preview image displayed by the game UI.

Expected naming:

```text
Image.png
```

---

## `description.txt`

Optional text description shown in menus or map selection screens.

Expected naming:

```text
description.txt
```

---

## `info.txt`

Optional metadata file.

Used for:

* Creator
* Version
* Additional metadata

Supported formats:

```text
creator = Example
version = 1.0
```

Also supports:

```text
creator: Example
version: 1.0
```

And:

```text
creator Example
version 1.0
```

---

# Metadata Parsing

The newer implementation parses metadata dynamically.

Example:

```csharp
if (key.Equals("creator", StringComparison.OrdinalIgnoreCase))
    creator = value;
else if (key.Equals("version", StringComparison.OrdinalIgnoreCase))
    version = value;
```

Supported separators:

* `=`
* `:`
* `-`
* Space fallback

---

# Example Implementation

```csharp
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using Mappy.MapPacks;

namespace Mappy.PackTemplate
{
    public sealed class Pack : IMappyMapPack
    {
        public IEnumerable<MappyPackedMap> GetMaps()
        {
            var asm = Assembly.GetExecutingAssembly();
            var resources = asm.GetManifestResourceNames();

            var mapGroups = resources
                .Where(r => r.Contains(".Maps."))
                .GroupBy(r =>
                {
                    var parts = r.Split('.');
                    var mapsIndex = System.Array.IndexOf(parts, "Maps");

                    return mapsIndex >= 0 && mapsIndex + 1 < parts.Length
                        ? parts[mapsIndex + 1]
                        : "Unknown";
                });

            foreach (var group in mapGroups)
            {
                var mapName = group.Key;

                var upkRes = group.FirstOrDefault(r =>
                    r.EndsWith(".upk", StringComparison.OrdinalIgnoreCase) ||
                    r.EndsWith(".upk.gz", StringComparison.OrdinalIgnoreCase));

                if (upkRes == null)
                    continue;

                var upkSourceStream = asm.GetManifestResourceStream(upkRes);

                Stream? upkStream = upkSourceStream;

                if (upkSourceStream != null &&
                    upkRes.EndsWith(".upk.gz", StringComparison.OrdinalIgnoreCase))
                {
                    using var gzip = new GZipStream(upkSourceStream, CompressionMode.Decompress);

                    var decompressed = new MemoryStream();

                    gzip.CopyTo(decompressed);
                    decompressed.Position = 0;

                    upkStream = decompressed;
                }

                if (upkStream == null)
                    continue;

                yield return new MappyPackedMap
                {
                    Name = mapName,
                    UpkStream = upkStream
                };
            }
        }
    }
}
```

---

# Embedding Resources

All map files must be marked as:

```xml
Embedded Resource
```

inside the project file or Visual Studio properties.

Example:

```xml
<ItemGroup>
    <EmbeddedResource Include="Maps\\**\\*" />
</ItemGroup>
```

---

# Loading Flow

At runtime:

1. The game loads the map pack assembly
2. Searches for implementations of `IMappyMapPack`
3. Calls `GetMaps()`
4. Scans embedded resources automatically
5. Groups maps dynamically
6. Decompresses `.upk.gz` files if needed
7. Reads all returned `MappyPackedMap` objects
8. Loads the `.upk` streams into the game

---

# Notes

* Maps are discovered entirely through embedded resources
* Multiple maps can exist inside a single pack
* Missing images/descriptions are allowed
* Missing `.upk` files cause the map to be skipped
* `.upk.gz` files are supported
* Metadata parsing is flexible
* Streams are loaded directly from assembly resources
* External GitHub downloads are no longer required

---

# Recommended Naming

## Original `Pack.cs`

```text
Maps/<MapName>/<MapName>.upk
Maps/<MapName>/Image.png
Maps/<MapName>/description.txt
Maps/<MapName>/info.txt
```

Compressed maps:

```text
Maps/<MapName>/<MapName>.upk.gz
```

## New `Pack.cs`

Only metadata resources are required locally:

```text
Maps/<MapName>/Image.png
Maps/<MapName>/description.txt
Maps/<MapName>/info.txt
```

The actual `.upk` file is hosted externally on GitHub Releases.

---

# Summary

The transition from the original embedded `Pack.cs` system to the newer GitHub download version changed the map loading architecture significantly.

The original implementation:

* Automatically discovered maps
* Required `.upk` files to be embedded directly into the DLL
* Loaded maps from assembly resources
* Supported `.upk.gz` compression
* Parsed metadata dynamically
* Worked fully offline

The newer implementation:

* Uses manual map registration
* Downloads maps externally from GitHub Releases
* No longer requires `.upk` files inside the DLL
* Keeps the DLL significantly smaller
* Stores only metadata/images inside the assembly
* Returns `DownloadUrl` instead of embedded `UpkStream`

This reduces assembly size and allows maps to be updated remotely without rebuilding the pack, but introduces a dependency on external downloads and GitHub availability.
