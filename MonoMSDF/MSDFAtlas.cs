using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonoMSDF
{
    public sealed class MSDFAtlas : IDisposable
    {
        internal MSDFAtlas() { }

        internal Texture2D _Texture;
        public Texture2D Texture => _Texture;

        internal GlyphAtlas _Metadata;
        public GlyphAtlas Metadata => _Metadata;

        public void Dispose()
        {
            _Texture.Dispose();
        }

        public static MSDFAtlas LoadFrom(GraphicsDevice gDevice, string texturePath, string metadataPath)
        {
            using var texStream = File.OpenRead(texturePath);
            using var metaStream = File.OpenRead(metadataPath);
            return LoadFrom(gDevice, texStream, metaStream);
        }
        public static MSDFAtlas LoadFrom(GraphicsDevice gDevice, string texturePath, Stream metadataStream)
        {
            using var texStream = File.OpenRead(texturePath);
            return LoadFrom(gDevice, texStream, metadataStream);
        }
        public static MSDFAtlas LoadFrom(GraphicsDevice gDevice, Stream textureStream, string metadataPath)
        {
            using var metaStream = File.OpenRead(metadataPath);
            return LoadFrom(gDevice, textureStream, metaStream);
        }
        public static MSDFAtlas LoadFrom(GraphicsDevice gDevice, Stream textureStream, Stream metadataStream)
        {
            var instance = new MSDFAtlas();
            instance._Metadata = JsonSerializer.Deserialize(metadataStream, SerializationModeOptionsContext.Default.GlyphAtlas);
            if (instance.Metadata == null)
            {
                throw new Exception("Failed to deserialize json text");
            }
            instance._Texture = Texture2D.FromStream(gDevice, textureStream);

            return instance;
        }
    }
}
