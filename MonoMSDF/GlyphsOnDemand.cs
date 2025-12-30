using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpMSDF.Atlas;
using SharpMSDF.Core;
using SharpMSDF.IO;
using Typography.OpenFont;

namespace MonoMSDF;

public class GlyphsOnDemand
{
	private const float miterLimit = 1.0f;
	private const float maxCornerAngle = 3f;

	private readonly GraphicsDevice GraphicsDevice;
	private readonly Typeface font;

	private readonly FontGeometry fontGeometry;
	private readonly Texture2DAtlasStorage textureStorage;
	private readonly DynamicAtlas<ImmediateAtlasGenerator<float, Texture2DAtlasStorage>> myDynamicAtlas;
	public Texture2D AtlasTexture => myDynamicAtlas.Generator.Storage.RenderTarget;

	private float fontSize = 48;
	private float distanceRange = 8f;

	Typeface altFont;

	public GlyphsOnDemand(GlyphAtlas atlas, GraphicsDevice gd, string fontPath, float fontSize, float distanceRange)
	{
		this.fontSize = fontSize;
		this.distanceRange = distanceRange;

		GraphicsDevice = gd;
		font = FontImporter.LoadFont(fontPath);

		textureStorage = new Texture2DAtlasStorage(GraphicsDevice, (int)(fontSize * 1.5f + distanceRange));
		textureStorage.Init(128, 128);

		myDynamicAtlas = new()
		{
			Generator = new(3, GlyphGenerators.Msdf, textureStorage),
			Packer = new(),
		};

		fontGeometry = new FontGeometry();
	}

	private readonly List<Shape> shapes = [];
	private readonly List<GlyphGeometry> geometry = [];

	private readonly HashSet<char> usedChars = [];

	public void AddGlyphs(ref GlyphAtlas atlas, ReadOnlySpan<char> chars, bool calculateKernings)
	{
		AddGlyphs(ref atlas, font, chars, calculateKernings);
	}

	public void AddGlyphs(ref GlyphAtlas atlas, Typeface useFont, ReadOnlySpan<char> chars, bool calculateKernings)
	{
		if (font == null)
		{
			throw new Exception("No font loaded!");
		}

		shapes.Clear();
		geometry.Clear();

		Span<char> actualChars = stackalloc char[chars.Length];
		int actualCount = 0;

		bool isSymbol = false;

		for (int i = 0; i < chars.Length; i++)
		{
			if (!usedChars.Contains(chars[i]))
			{
				Shape shape = FontImporter.LoadGlyph(font, chars[i], FontCoordinateScaling.None);

				if (shape == null && altFont != null)
				{
					shape = FontImporter.LoadGlyph(altFont, chars[i], FontCoordinateScaling.None);
					if (shape != null)
					{
						isSymbol = true;
					}
				}

				if (shape != null)
				{
					shapes.Add(shape);
					geometry.Add(GeometryPool.Rent());
					_ = usedChars.Add(chars[i]);
					actualChars[actualCount] = chars[i];
					actualCount++;
				}
			}
		}

		if (shapes.Count == 0)
		{
			return;
		}

		actualCount = fontGeometry.LoadCharset(shapes, font, 1.0f, actualChars[..actualCount], geometry, true);

		if (calculateKernings)
		{
			_ = fontGeometry.LoadKerning(usedChars, font);
		}

		ulong seed = 0;

		for (int i = 0; i < actualCount; i++)
		{
			//EdgeColorings.InkTrap(shapes[i], maxCornerAngle, ref seed);
			geometry[i].WrapBox(shapes[i], new() { Scale = fontSize, Range = new(distanceRange / fontSize), MiterLimit = miterLimit });
		}

		myDynamicAtlas.Add(shapes, geometry);

		atlas.Metrics = new Metrics((int)fontGeometry.Metrics.EmSize, fontGeometry.Metrics.LineHeight, fontGeometry.Metrics.AscenderY, fontGeometry.Metrics.DescenderY, fontGeometry.Metrics.UnderlineY, 0.0f);
		atlas.Kerning.Clear();

		for (int i = 0; i < fontGeometry.Kerning.Count; i++)
		{
			KeyValuePair<(ushort, ushort), float> kp = fontGeometry.Kerning.ElementAt(i);
			atlas.Kerning.Add(new Kerning(kp.Key.Item1, kp.Key.Item2, (float)kp.Value));
		}

		foreach (GlyphGeometry g in geometry)
		{
			g.GetQuadPlaneBounds(out float pl, out float pb, out float pr, out float pt);
			g.GetQuadAtlasBounds(out float al, out float ab, out float ar, out float at);

			float texLeft = (float)al;
			float texBottom = (float)ab;
			float texRight = (float)ar;
			float texTop = (float)at;

			if (isSymbol)
			{
				pb += atlas.Metrics.UnderlineY;
				pt += atlas.Metrics.UnderlineY;
			}

			//if (isSymbol)
			//{
			//	// Desired top and bottom based purely on your font’s usable line area
			//	float desiredTop = atlas.Metrics.Ascender;
			//	float desiredBottom = atlas.Metrics.Descender;
			//	float desiredHeight = desiredTop - desiredBottom;

			//	// Current bounds
			//	float iconTop = pt;
			//	float iconBottom = pb;
			//	float iconHeight = iconTop - iconBottom;

			//	if (iconHeight > 0.0001f)
			//	{
			//		// Scale required to make icon match vertical space
			//		float scale = desiredHeight / iconHeight;

			//		// Recompute new height while preserving center position
			//		float center = (iconTop + iconBottom) * 0.5f;

			//		float newHalfHeight = (iconHeight * scale) * 0.5f;

			//		iconTop = center + newHalfHeight;
			//		iconBottom = center - newHalfHeight;

			//		// Now move vertically so it matches desired region
			//		// Align centers
			//		float desiredCenter = (desiredTop + desiredBottom) * 0.5f;
			//		float currentCenter = (iconTop + iconBottom) * 0.5f;
			//		float offset = desiredCenter - currentCenter;

			//		iconTop += offset;
			//		iconBottom += offset;
			//	}

			//	// Write back adjusted values
			//	pt = iconTop;
			//	pb = iconBottom;
			//}

			atlas.Glyphs.Add(
			new Glyph(
				g.GetCodepoint, (float)g.GetAdvance(),
				new MonoMSDF.Bounds((float)pl, (float)pb, (float)pr, (float)pt),
				new MonoMSDF.Bounds((float)al, (float)ab, (float)ar, (float)at),
				new QuadUV(
					new Vector2(texLeft, texBottom),
					new Vector2(texRight, texBottom),
					new Vector2(texRight, texTop),
					new Vector2(texLeft, texTop)
				)
			)
		);
		}

		atlas.Atlas = new Atlas("msdf", distanceRange, 0, fontSize, AtlasTexture.Width, AtlasTexture.Height, "not_used");

		for (int i = 0; i < shapes.Count; i++)
		{
			ShapePool.Return(shapes[i]);
			GeometryPool.Return(geometry[i]);
		}
	}

	public void AddFont(string fontPath)
	{
		altFont = FontImporter.LoadFont(fontPath);
	}
}