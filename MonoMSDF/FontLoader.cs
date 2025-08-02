using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;

namespace MonoMSDF
{
	public static class FontLoader
	{
		public static GlyphAtlas Load(string path)
		{
			string json = File.ReadAllText(path);
			var result = JsonSerializer.Deserialize(json, SerializationModeOptionsContext.Default.GlyphAtlas);
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
