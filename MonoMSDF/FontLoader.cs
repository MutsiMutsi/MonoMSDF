using System.Text.Json;

namespace MonoMSDF
{
	public class FontLoader
	{
		public static GlyphAtlas Load(string path)
		{
			string json = File.ReadAllText(path);
			var result = JsonSerializer.Deserialize(json, SerializationModeOptionsContext.Default.GlyphAtlas);
			if (result == null)
			{
				throw new Exception("Failed to deserialize json text");
			}
			return result;
		}
	}
}
