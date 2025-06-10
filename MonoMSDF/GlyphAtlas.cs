using System.Text.Json.Serialization;

namespace MonoMSDF;

public class Atlas
{
	public string type { get; set; }
	public int distanceRange { get; set; }
	public int distanceRangeMiddle { get; set; }
	public int size { get; set; }
	public int width { get; set; }
	public int height { get; set; }
	public string yOrigin { get; set; }
}

public struct Bounds
{
	public float left { get; set; }
	public float bottom { get; set; }
	public float right { get; set; }
	public float top { get; set; }
}


public class Glyph
{
	public int unicode { get; set; }
	public float advance { get; set; }
	public Bounds planeBounds { get; set; }
	public Bounds atlasBounds { get; set; }
}

public class Metrics
{
	public int emSize { get; set; }
	public float lineHeight { get; set; }
	public float ascender { get; set; }
	public float descender { get; set; }
	public float underlineY { get; set; }
	public float underlineThickness { get; set; }
}

public class GlyphAtlas
{
	public Atlas Atlas { get; set; }
	public Metrics Metrics { get; set; }
	public List<Glyph> Glyphs { get; set; }
	public List<object> Kerning { get; set; }
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(GlyphAtlas))]
internal partial class SerializationModeOptionsContext : JsonSerializerContext
{
	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string ToString()
	{
		return base.ToString();
	}
}