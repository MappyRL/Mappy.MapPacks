using System;
using System.Collections.Generic;
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

                var upkRes = group.FirstOrDefault(r => r.EndsWith(".upk", System.StringComparison.OrdinalIgnoreCase));
                if (upkRes == null) continue;

                // be harsh about description and image might have to make it less harsh later down the line for now works okay dokie
                var imgRes = group.FirstOrDefault(r =>
                    r.EndsWith(".Image.png", StringComparison.OrdinalIgnoreCase));

                var descRes = group.FirstOrDefault(r =>
                    r.EndsWith(".description.txt", StringComparison.OrdinalIgnoreCase));

                var upkStream = asm.GetManifestResourceStream(upkRes);
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

