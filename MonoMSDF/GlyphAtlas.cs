using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace MonoMSDF;

public readonly struct Bounds
{
	public readonly float left;
	public readonly float bottom;
	public readonly float right;
	public readonly float top;

	public Bounds(float left, float bottom, float right, float top)
	{
		this.left = left;
		this.bottom = bottom;
		this.right = right;
		this.top = top;
	}
}

public readonly struct Atlas
{
	public Atlas(string type, int distanceRange, int distanceRangeMiddle, int size, int width, int height, string yOrigin)
	{
		Type = type;
		DistanceRange = distanceRange;
		DistanceRangeMiddle = distanceRangeMiddle;
		Size = size;
		Width = width;
		Height = height;
		YOrigin = yOrigin;
	}

	public readonly string Type;
	public readonly int DistanceRange;
	public readonly int DistanceRangeMiddle;
	public readonly int Size;
	public readonly int Width;
	public readonly int Height;
	public readonly string YOrigin;
}

public class Glyph
{
	public int Unicode { get; set; }
	public float Advance { get; set; }
	public Bounds PlaneBounds { get; set; }
	public Bounds AtlasBounds { get; set; }

	public Vector2[] TextureCoordinates = new Vector2[4];
}

public readonly struct Kerning
{
	public Kerning(int unicode1, int unicode2, float advance)
	{
		Unicode1 = unicode1;
		Unicode2 = unicode2;
		Advance = advance;
	}

	public readonly int Unicode1;
	public readonly int Unicode2;
	public readonly float Advance;
}

public readonly struct Metrics
{
	public Metrics(int emSize, float lineHeight, float ascender, float descender, float underlineY, float underlineThickness)
	{
		EmSize = emSize;
		LineHeight = lineHeight;
		Ascender = ascender;
		Descender = descender;
		UnderlineY = underlineY;
		UnderlineThickness = underlineThickness;
	}

	public readonly int EmSize;
	public readonly float LineHeight;
	public readonly float Ascender;
	public readonly float Descender;
	public readonly float UnderlineY;
	public readonly float UnderlineThickness;
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