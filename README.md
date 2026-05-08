# Mappy.MapPacks

`Mappy.MapPacks` is the plugin-based map loading system used by the game.
It allows maps to be bundled into standalone assemblies and discovered dynamically at runtime.

A map pack exposes one or more maps through the `IMappyMapPack` interface, returning `MappyPackedMap` objects that contain the playable map data and optional metadata.

---

# How It Works

The system works by:

1. Creating a class library project
2. Referencing `Mappy.MapPacks`
3. Implementing `IMappyMapPack`
4. Embedding map resources into the assembly
5. Returning those resources as `MappyPackedMap` objects

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
}
```

## Properties

| Property         | Description                     |
| ---------------- | ------------------------------- |
| `Name`           | Display name of the map         |
| `UpkStream`      | Required `.upk` map file stream |
| `ImagePngStream` | Optional preview image          |
| `Description`    | Optional text description       |

---

# Pack Structure

The template pack automatically discovers embedded resources using a folder convention.

## Expected Structure

```text
Maps/
 ├── MyMap/
 │    ├── MyMap.upk
 │    ├── Image.png
 │    └── description.txt
 │
 ├── AnotherMap/
 │    ├── AnotherMap.upk
 │    ├── Image.png
 │    └── description.txt
```

---

# Resource Discovery

The template loader scans all embedded resources and groups them by the folder directly under `Maps`.

Example resource names:

```text
Mappy.PackTemplate.Maps.MyMap.MyMap.upk
Mappy.PackTemplate.Maps.MyMap.Image.png
Mappy.PackTemplate.Maps.MyMap.description.txt
```

The group name (`MyMap`) becomes the map name exposed to the game.

---

# Required Files

## `.upk`

Required.

This is the actual playable map package.

If no `.upk` file exists for a map group, the map is ignored.

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

# Example Implementation

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mappy.MapPacks;

namespace MyMapPack
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
                    r.EndsWith(".upk", System.StringComparison.OrdinalIgnoreCase));

                if (upkRes == null)
                    continue;

                var imgRes = group.FirstOrDefault(r =>
                    r.EndsWith(".Image.png", System.StringComparison.OrdinalIgnoreCase));

                var descRes = group.FirstOrDefault(r =>
                    r.EndsWith(".description.txt", System.StringComparison.OrdinalIgnoreCase));

                var upkStream = asm.GetManifestResourceStream(upkRes);

                if (upkStream == null)
                    continue;

                Stream? imgStream = null;

                if (imgRes != null)
                    imgStream = asm.GetManifestResourceStream(imgRes);

                string? description = null;

                if (descRes != null)
                {
                    using var s = asm.GetManifestResourceStream(descRes);

                    if (s != null)
                    using (var reader = new StreamReader(s))
                        description = reader.ReadToEnd();
                }

                yield return new MappyPackedMap
                {
                    Name = mapName,
                    UpkStream = upkStream,
                    ImagePngStream = imgStream,
                    Description = description
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
4. Reads all returned `MappyPackedMap` objects
5. Loads the `.upk` streams into the game

---

# Notes

* Maps are discovered entirely through embedded resources
* Multiple maps can exist inside a single pack
* Missing images/descriptions are allowed
* Missing `.upk` files cause the map to be skipped
* Streams are loaded directly from assembly resources

---

# Recommended Naming

For consistency:

```text
Maps/<MapName>/<MapName>.upk
Maps/<MapName>/Image.png
Maps/<MapName>/description.txt
```

Example:

```text
Maps/DM-Deck/DM-Deck.upk
Maps/DM-Deck/Image.png
Maps/DM-Deck/description.txt
```

---

# Summary

`Mappy.MapPacks` provides a lightweight and flexible way to package maps as self-contained assemblies.

Each pack:

* Implements `IMappyMapPack`
* Embeds map resources
* Returns playable map definitions through `GetMaps()`

This makes map distribution simple, portable, and easy for the game to load dynamically.
