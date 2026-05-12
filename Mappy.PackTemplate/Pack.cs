using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mappy.MapPacks;

namespace Mappy.PackTemplate
{
    public sealed class Pack : IMappyMapPack
    {
        private static readonly Dictionary<string, string> MapUrls = new()
        {
["Aim training"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/Aim_training.upk",
["air dribble warm up"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/air_dribble_warm_up.upk",
["Ever Olympics"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/EverOlympics.upk",
["Knockout Mushroom Kingdom"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/Knockout_Mushroom_Kingdom.upk",
["Mastering Fundamental Dribbles v2 beta"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/Mastering_Fundamental_Dribbles_v2_beta.upk",
["Need Boost"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/Need_Boost.upk",
["orbital stadium"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/orbital_stadium.upk",
["Rings Of Death"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/RINGS_OF_DEATH.upk",
["Speed jump Rings 3"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1/Speed_jump_Rings_3.upk",
["Dribbling Challenge 1"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/LethDribbleChallenge1.upk",
["Minigolf 2"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/minigolf2.upk",
["Kaokor Challenge"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/KaokorChallenge.upk",
["Flappy Bird"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/LethFlappyBird1stP.upk",
["Sidewaller Remastered"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/sidewaller_Fractal.upk",
["Limited Boost Dribble Challenge"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/LimitedBoostDribbleChallenge.upk",
["Air Dribble Hoops"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/TJBrotherAirDribble.upk",
["Tetris"] = "https://github.com/MappyRL/MappyMap-Repo/releases/download/v1.1/LethTetris.upk"

        };

        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        public IEnumerable<MappyPackedMap> GetMaps()
        {
            foreach (var mapName in MapUrls.Keys)
            {
                yield return new MappyPackedMap
                {
                    Name = mapName,
                    UpkStream = null, 
                    ImagePngStream = GetEmbeddedResourceStream(mapName, "image.png"),
                    Description = GetEmbeddedResourceText(mapName, "description.txt"),
                    Creator = GetEmbeddedResourceText(mapName, "creator.txt"),
                    Version = null,
                    InfoText = GetEmbeddedResourceText(mapName, "info.txt"),
                    DownloadUrl = MapUrls[mapName]
                };
            }
        }

        private static Stream? GetEmbeddedResourceStream(string mapName, string fileName)
        {
            try
            {
                var resourceName = GetMapResourceName(mapName, fileName);
                var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    stream.Position = 0; 
                }
                return stream;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetEmbeddedResourceText(string mapName, string fileName)
        {
            try
            {
                using var stream = GetEmbeddedResourceStream(mapName, fileName);
                if (stream == null) return null;
                
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        private static string SanitizeMapName(string mapName)
        {
            return mapName.Replace(" ", "_");
        }

        private static string GetMapResourceName(string mapName, string fileName)
        {
            var sanitizedName = SanitizeMapName(mapName);
            return $"Mappy.PackTemplate.Maps.{sanitizedName}.{fileName}";
        }
    }
}

