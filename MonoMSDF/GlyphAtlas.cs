using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace MonoMSDF;

public readonly record struct QuadUV(Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft);
/*{
	public Vector2 TopLeft;
	public Vector2 TopRight;
	public Vector2 BottomRight;
	public Vector2 BottomLeft;

	
	{
		TopLeft = topLeft;
		TopRight = topRight;
		BottomRight = bottomRight;
		BottomLeft = bottomLeft;
	}

	public Vector2 this[int index]
	{
		get => index switch
		{
			0 => TopLeft,
			1 => TopRight,
			2 => BottomRight,
			3 => BottomLeft,
			_ => throw new IndexOutOfRangeException()
		};
		set
		{
			switch (index)
			{
				case 0: TopLeft = value; break;
				case 1: TopRight = value; break;
				case 2: BottomRight = value; break;
				case 3: BottomLeft = value; break;
				default: throw new IndexOutOfRangeException();
			}
		}
	}
}*/

public readonly record struct Bounds(float Left, float Bottom, float Right, float Top);
public readonly record struct Atlas(string Type, float DistanceRange, int DistanceRangeMiddle, int Size, int Width, int Height, string YOrigin);

public readonly record struct Glyph(int Unicode, float Advance, Bounds PlaneBounds, Bounds AtlasBounds, QuadUV TextureCoordinates);

public readonly record struct Kerning(int Unicode1, int Unicode2, float Advance);
public readonly record struct Metrics(int EmSize, float LineHeight, float Ascender, float Descender, float UnderlineY, float UnderlineThickness);

public class GlyphAtlas
{
	public Atlas Atlas { get; set; }
	public Metrics Metrics { get; set; }
	public List<Glyph> Glyphs { get; set; } = [];
	public List<Kerning> Kerning { get; set; } = [];
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