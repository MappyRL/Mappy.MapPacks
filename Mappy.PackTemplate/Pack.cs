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
                    return mapsIndex >= 0 && mapsIndex + 1 < parts.Length ? parts[mapsIndex + 1] : "Unknown";
                });

            foreach (var group in mapGroups)
            {
                var mapName = group.Key;

                var upkRes = group.FirstOrDefault(r =>
                    r.EndsWith(".upk", StringComparison.OrdinalIgnoreCase) ||
                    r.EndsWith(".upk.gz", StringComparison.OrdinalIgnoreCase));
                if (upkRes == null) continue;

                // be harsh about description and image might have to make it less harsh later down the line for now works okay dokie

                var imgRes = group.FirstOrDefault(r =>
                    r.EndsWith(".Image.png", StringComparison.OrdinalIgnoreCase));

                var descRes = group.FirstOrDefault(r =>
                    r.EndsWith(".description.txt", StringComparison.OrdinalIgnoreCase));

                var infoRes = group.FirstOrDefault(r =>
                    r.EndsWith(".info.txt", StringComparison.OrdinalIgnoreCase));

                var upkSourceStream = asm.GetManifestResourceStream(upkRes);
                Stream? upkStream = upkSourceStream;
                if (upkSourceStream != null && upkRes.EndsWith(".upk.gz", StringComparison.OrdinalIgnoreCase))
                {
//True Gambling with this compression and decompression hopefully it works
                    using var gzip = new GZipStream(upkSourceStream, CompressionMode.Decompress);
                    var decompressed = new MemoryStream();
                    gzip.CopyTo(decompressed);
                    decompressed.Position = 0;
                    upkStream = decompressed;
                }
                if (upkStream == null) continue;

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

                string? infoText = null;
                if (infoRes != null)
                {
                    using var s = asm.GetManifestResourceStream(infoRes);
                    if (s != null)
                    using (var reader = new StreamReader(s))
                        infoText = reader.ReadToEnd();
                }

                string? creator = null;
                string? version = null;
                if (!string.IsNullOrWhiteSpace(infoText))
                {
                    foreach (var rawLine in infoText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                    {
                        var line = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var separators = new[] { '=', ':', '-' };
                        var key = string.Empty;
                        var value = string.Empty;
                        var matched = false;
                        foreach (var separator in separators)
                        {
                            var idx = line.IndexOf(separator);
                            if (idx <= 0 || idx >= line.Length - 1)
                                continue;

                            key = line[..idx].Trim();
                            value = line[(idx + 1)..].Trim();
                            matched = true;
                            break;
                        }

                        if (!matched)
                        {
                            var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                key = parts[0].Trim();
                                value = parts[1].Trim();
                                matched = true;
                            }
                        }

                        if (!matched || string.IsNullOrWhiteSpace(value))
                            continue;

                        if (key.Equals("creator", StringComparison.OrdinalIgnoreCase))
                            creator = value;
                        else if (key.Equals("version", StringComparison.OrdinalIgnoreCase))
                            version = value;
                    }
                }

                yield return new MappyPackedMap
                {
                    Name = mapName,
                    UpkStream = upkStream,
                    ImagePngStream = imgStream,
                    Description = description,
                    Creator = creator,
                    Version = version,
                    InfoText = infoText
                };
            }
        }
    }
}

