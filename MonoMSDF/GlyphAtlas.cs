using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace MonoMSDF;

public struct Bounds
{
	public float left { get; set; }
	public float bottom { get; set; }
	public float right { get; set; }
	public float top { get; set; }
}

public class Atlas
{
	public string Type { get; set; }
	public int DistanceRange { get; set; }
	public int DistanceRangeMiddle { get; set; }
	public int Size { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public string YOrigin { get; set; }
}

public class Glyph
{
	public int Unicode { get; set; }
	public float Advance { get; set; }
	public Bounds PlaneBounds { get; set; }
	public Bounds AtlasBounds { get; set; }

	public Vector2[] TextureCoordinates { get; set; }
}

public class Kerning
{
	public int Unicode1 { get; set; }
	public int Unicode2 { get; set; }
	public float Advance { get; set; }
}

public class Metrics
{
	public int EmSize { get; set; }
	public float LineHeight { get; set; }
	public float Ascender { get; set; }
	public float Descender { get; set; }
	public float UnderlineY { get; set; }
	public float UnderlineThickness { get; set; }
}

public class GlyphAtlas
{
	public Atlas Atlas { get; set; }
	public Metrics Metrics { get; set; }
	public List<Glyph> Glyphs { get; set; }
	public List<Kerning> Kerning { get; set; }
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