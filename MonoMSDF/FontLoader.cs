using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;

namespace MonoMSDF
{
	[Obsolete("Use MSDFFontLoadFrom")]
	public static class FontLoader
	{
		public static GlyphAtlas Load(string path)
		{
            using var jsonFile = File.OpenRead(path);
            return Load(jsonFile);
        }
		public static GlyphAtlas Load(Stream stream)
		{
			var result = JsonSerializer.Deserialize(stream, SerializationModeOptionsContext.Default.GlyphAtlas);
			if (result == null)
			{
				throw new Exception("Failed to deserialize json text");
			}
			return result;
		}
		public static Texture2D LoadTexture(GraphicsDevice graphicsDevice, string path)
		{
			// Implementation depends on your texture loading strategy
			// You can use Texture2D.FromFile or MonoGame's content pipeline
			return Texture2D.FromFile(graphicsDevice, path);
		}
	}
}
