using System.Collections.Generic;
using System.IO;

namespace Mappy.MapPacks
{

    public interface IMappyMapPack
    {
        IEnumerable<MappyPackedMap> GetMaps();
    }

    public sealed class MappyPackedMap
    {
        public string Name { get; init; } = string.Empty;

 
        public Stream UpkStream { get; init; } = Stream.Null;


        public Stream? ImagePngStream { get; init; }


        public string? Description { get; init; }


        public string? Creator { get; init; }


        public string? Version { get; init; }


        public string? InfoText { get; init; }


        public string? DownloadUrl { get; init; }
    }
}

