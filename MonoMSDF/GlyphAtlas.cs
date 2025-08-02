using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace MonoMSDF;

public readonly record struct Bounds(float Left, float Bottom, float Right, float Top);
public readonly record struct Atlas(string Type, int DistanceRange, int DistanceRangeMiddle, int Size, int Width, int Height, string YOrigin);

public readonly record struct Glyph(int Unicode, float Advance, Bounds PlaneBounds, Bounds AtlasBounds, Vector2[] TextureCoordinates);

public readonly record struct Kerning(int Unicode1, int Unicode2, float Advance);
public readonly record struct Metrics(int EmSize, float LineHeight, float Ascender, float Descender, float UnderlineY, float UnderlineThickness);

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