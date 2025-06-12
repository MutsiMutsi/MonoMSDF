using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoMSDF
{

	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct VertexFont : IVertexType
	{
		public Vector2 Position { get; set; }
		public Vector2 TextureCoordinate { get; set; }
		public Color FillColor { get; set; }
		public Color StrokeColor { get; set; }

		public VertexFont(Vector2 pos, Vector2 tc, Color fillColor, Color strokeColor)
		{
			Position = pos;
			TextureCoordinate = tc;
			FillColor = fillColor;
			StrokeColor = strokeColor;
		}

		public VertexDeclaration VertexDeclaration => new VertexDeclaration(
			new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
			new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
			new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0),
			new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1)
		);
	}

	//TODO: check if padding helps or not, its to align with 16 byte
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct TextInstance
	{
		public Matrix WorldTransform;
		public Vector2 PixelRanges;
		private Vector2 _padding;   // 8 bytes padding

		public TextInstance(Matrix world, float screenPxRange, float distanceRange)
		{
			PixelRanges.X = screenPxRange;
			PixelRanges.Y = distanceRange;
			WorldTransform = world;
		}

		public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
			80, // added due to padding of 8 bytes padding
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
			new VertexElement(64, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5)
		);
	}

	public enum FontDrawType
	{
		StandardText,
		StandardTextWithStroke,
		TinyText,
		TinyTextWithStroke,
	}

	public class MSDFTextRenderer : IDisposable
	{
		private readonly GraphicsDevice graphicsDevice;
		private readonly Effect msdifEffect;

		public Texture2D atlasTexture;
		private readonly SamplerState samplerState;

		private GlyphAtlas glyphAtlas;
		private readonly Dictionary<int, Glyph> glyphs = [];
		Dictionary<(char, char), float> kerningPairs = new();

		public DynamicTextBuffer TextBuffer;

		private float sizeInPixel = 0;
		private int numTriangles = 0;

		private readonly VertexFont[] vertices;
		private readonly short[] indices;

		private DynamicVertexBuffer instanceBuffer;
		private TextInstance[] instances = new TextInstance[1024];
		private int instanceCount = 0;

		public MSDFTextRenderer(GraphicsDevice graphicsDevice, Effect msdifEffect)
		{
			this.graphicsDevice = graphicsDevice;
			this.msdifEffect = msdifEffect;

			vertices = new VertexFont[4096 * 4];
			indices = new short[4096 * 6];
			TextBuffer = new DynamicTextBuffer(graphicsDevice, 4096);

			// Create sampler state for texture filtering
			samplerState = new SamplerState
			{
				Filter = TextureFilter.Linear,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp
			};

			instanceBuffer = new DynamicVertexBuffer(
				graphicsDevice,
				TextInstance.VertexDeclaration,
				instances.Length,
				BufferUsage.WriteOnly
			);
		}

		public void AddTextInstance(Matrix transform, float scale)
		{
			if (instanceCount >= instances.Length)
				Array.Resize(ref instances, instances.Length * 2);

			sizeInPixel = scale;
			float dpi = 96.0f;
			float pointSize = sizeInPixel;
			float actualScale = dpi / 72.0f * pointSize;
			float sizeInEm = glyphAtlas.Atlas.Size;
			float pixelRange = glyphAtlas.Atlas.DistanceRange;
			float screenPxRange = sizeInPixel / sizeInEm * pixelRange;

			instances[instanceCount++] = new TextInstance(transform, screenPxRange, glyphAtlas.Atlas.DistanceRange);
		}

		public bool LoadAtlas(string jsonPath, string pngPath)
		{
			try
			{
				// Load and parse JSON
				glyphAtlas = FontLoader.Load(jsonPath);
				foreach (Glyph glyph in glyphAtlas.Glyphs)
				{
					glyphs[glyph.Unicode] = glyph;
				}

				foreach (var kernEntry in glyphAtlas.Kerning)
				{
					char left = (char)kernEntry.Unicode1;
					char right = (char)kernEntry.Unicode2;
					float advance = (float)kernEntry.Advance;
					kerningPairs[(left, right)] = advance;
				}

				// Load PNG texture using MonoGame's content pipeline or Texture2D.FromFile
				atlasTexture = TextureLoader.LoadTexture(graphicsDevice, pngPath);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to load atlas: {ex.Message}");
				return false;
			}
		}

		public void GenerateGeometry(string text, Color fillColor, Color strokeColor)
		{
			GenerateTextGeometry(text, vertices, indices, fillColor, strokeColor);
			TextBuffer.UpdateText(vertices, indices);
		}

		public void RenderText(Matrix world, Matrix view, Matrix projection, float scale, FontDrawType drawType = FontDrawType.StandardText)
		{
			if (atlasTexture == null || msdifEffect == null)
			{
				return;
			}

			sizeInPixel = scale;

			float dpi = 96.0f;
			float pointSize = scale;
			float actualScale = dpi / 72.0f * pointSize;
			float sizeInEm = glyphAtlas.Atlas.Size;
			float pixelRange = glyphAtlas.Atlas.DistanceRange;
			float screenPxRange = sizeInPixel / sizeInEm * pixelRange;

			// Set effect parameters
			msdifEffect.Parameters["WorldViewProjection"].SetValue(world * view * projection);
			msdifEffect.Parameters["ScreenPxRange"].SetValue(screenPxRange);
			msdifEffect.Parameters["SpriteTexture"].SetValue(atlasTexture);

			msdifEffect.Parameters["AtlasSize"].SetValue(new Vector2(glyphAtlas.Atlas.Width, glyphAtlas.Atlas.Height));
			msdifEffect.Parameters["DistanceRange"].SetValue(glyphAtlas.Atlas.DistanceRange);

			//"MSDFTextWithStroke"
			//"MSDFSmallTextWithStroke"
			string tech = "";

			switch (drawType)
			{
				case FontDrawType.StandardText:
					tech = "MSDFTextRendering";
					break;
				case FontDrawType.StandardTextWithStroke:
					tech = "MSDFTextWithStroke";
					break;
				case FontDrawType.TinyText:
					tech = "MSDFSmallText";
					break;
				case FontDrawType.TinyTextWithStroke:
					tech = "MSDFSmallTextWithStroke";
					break;
			}

			msdifEffect.CurrentTechnique = msdifEffect.Techniques[tech];

			msdifEffect.CurrentTechnique.Passes[0].Apply();
			if (TextBuffer.VertexBuffer != null && TextBuffer.IndexBuffer != null && numTriangles > 0)
			{
				// Set vertex buffer
				graphicsDevice.SetVertexBuffer(TextBuffer.VertexBuffer);
				graphicsDevice.Indices = TextBuffer.IndexBuffer;

				// Make sure graphics state is setup proper.
				graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
				graphicsDevice.DepthStencilState = DepthStencilState.None;
				graphicsDevice.BlendState = BlendState.NonPremultiplied;
				graphicsDevice.Textures[0] = atlasTexture;

				graphicsDevice.RasterizerState = new RasterizerState()
				{
					CullMode = CullMode.None,
					FillMode = FillMode.Solid,
				};

				// Draw indexed primitives
				graphicsDevice.DrawIndexedPrimitives(
					PrimitiveType.TriangleList,
					0, // base vertex
					0, // start index
					numTriangles // primitive count
				);
			}
		}

		public void RenderInstances(Matrix view, Matrix projection, FontDrawType drawType = FontDrawType.StandardText)
		{
			if (atlasTexture == null || msdifEffect == null || instanceCount == 0)
				return;

			msdifEffect.Parameters["WorldViewProjection"].SetValue(view * projection);
			msdifEffect.Parameters["SpriteTexture"].SetValue(atlasTexture);
			msdifEffect.Parameters["AtlasSize"].SetValue(new Vector2(glyphAtlas.Atlas.Width, glyphAtlas.Atlas.Height));

			string tech = drawType switch
			{
				FontDrawType.StandardText => "MSDFTextRendering",
				FontDrawType.StandardTextWithStroke => "MSDFTextWithStroke",
				FontDrawType.TinyText => "MSDFSmallText",
				FontDrawType.TinyTextWithStroke => "MSDFSmallTextWithStroke",
				_ => "MSDFTextRendering"
			};

			msdifEffect.CurrentTechnique = msdifEffect.Techniques[tech];
			msdifEffect.CurrentTechnique.Passes[0].Apply();

			// Make sure graphics state is setup proper.
			graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
			graphicsDevice.DepthStencilState = DepthStencilState.None;
			graphicsDevice.BlendState = BlendState.NonPremultiplied;
			graphicsDevice.Textures[0] = atlasTexture;

			graphicsDevice.SetVertexBuffers(
				new VertexBufferBinding(TextBuffer.VertexBuffer, 0, 0),
				new VertexBufferBinding(instanceBuffer, 0, 1)
			);
			graphicsDevice.Indices = TextBuffer.IndexBuffer;

			instanceBuffer.SetData(instances, 0, instanceCount, SetDataOptions.Discard);

			graphicsDevice.DrawInstancedPrimitives(
				PrimitiveType.TriangleList,
				baseVertex: 0,
				startIndex: 0,
				primitiveCount: numTriangles,
				instanceCount: instanceCount
			);

			instanceCount = 0; // Reset for next frame
		}

		private void GenerateTextGeometry(string text, VertexFont[] vertices, short[] indices, Color fillColor, Color strokeColor)
		{
			numTriangles = 0;

			Vector2 position = Vector2.Zero;
			position.Y = -position.Y;
			float x = position.X;
			float y = position.Y - glyphAtlas.Metrics.Ascender;

			int vertexIndex = 0;
			int indicesIndex = 0;

			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (c == '\n')
				{
					x = position.X;
					y -= glyphAtlas.Metrics.LineHeight;
				}

				if (!glyphs.TryGetValue(c, out Glyph glyph))
				{
					continue;
				}

				if (glyph.PlaneBounds.right == 0)
				{
					x += glyph.Advance;
					continue;
				}

				// Calculate glyph quad positions
				float glyphLeft = x + (glyph.PlaneBounds.left);
				float glyphBottom = y + (glyph.PlaneBounds.bottom);
				float glyphRight = x + (glyph.PlaneBounds.right);
				float glyphTop = y + (glyph.PlaneBounds.top);

				// Calculate texture coordinates
				float texLeft = glyph.AtlasBounds.left / glyphAtlas.Atlas.Width;
				float texBottom = glyph.AtlasBounds.bottom / glyphAtlas.Atlas.Height;
				float texRight = glyph.AtlasBounds.right / glyphAtlas.Atlas.Width;
				float texTop = glyph.AtlasBounds.top / glyphAtlas.Atlas.Height;

				// Flip Y coordinates if atlas origin is bottom
				if (glyphAtlas.Atlas.YOrigin == "bottom")
				{
					texBottom = 1.0f - texBottom;
					texTop = 1.0f - texTop;
				}

				vertices[vertexIndex] = new VertexFont(
					new Vector2(glyphLeft, -glyphBottom),
					new Vector2(texLeft, texBottom),
					fillColor,
					strokeColor
				);
				vertices[vertexIndex + 1] = new VertexFont(
					new Vector2(glyphRight, -glyphBottom),
					new Vector2(texRight, texBottom),
					fillColor,
					strokeColor
				);
				vertices[vertexIndex + 2] = new VertexFont(
					new Vector2(glyphRight, -glyphTop),
					new Vector2(texRight, texTop),
					fillColor,
					strokeColor
				);
				vertices[vertexIndex + 3] = new VertexFont(
					new Vector2(glyphLeft, -glyphTop),
					new Vector2(texLeft, texTop),
					fillColor,
					strokeColor
				);

				// Create indices for two triangles forming a quad
				indices[indicesIndex++] = (short)vertexIndex;
				indices[indicesIndex++] = (short)(vertexIndex + 2);
				indices[indicesIndex++] = (short)(vertexIndex + 1);
				indices[indicesIndex++] = (short)vertexIndex;
				indices[indicesIndex++] = (short)(vertexIndex + 3);
				indices[indicesIndex++] = (short)(vertexIndex + 2);

				vertexIndex += 4;
				numTriangles += 2;

				if (i < text.Length - 2)
				{
					float kern;
					if (kerningPairs.TryGetValue((text[i], text[i + 1]), out kern))
					{
						x += kern;
					}
				}

				x += glyph.Advance;
			}
		}

		public void Dispose()
		{
			atlasTexture?.Dispose();
			samplerState?.Dispose();
			TextBuffer?.Dispose();
		}
	}

	// Supporting class for dynamic text buffer management
	public class DynamicTextBuffer : IDisposable
	{
		private readonly GraphicsDevice graphicsDevice;
		public VertexBuffer VertexBuffer { get; private set; }
		public IndexBuffer IndexBuffer { get; private set; }

		public DynamicTextBuffer(GraphicsDevice graphicsDevice, int maxCharacters)
		{
			this.graphicsDevice = graphicsDevice;

			// Create dynamic vertex buffer
			VertexBuffer = new VertexBuffer(
				graphicsDevice,
				typeof(VertexFont),
				maxCharacters * 4, // 4 vertices per character
				BufferUsage.WriteOnly
			);

			// Create dynamic index buffer
			IndexBuffer = new IndexBuffer(
				graphicsDevice,
				IndexElementSize.SixteenBits,
				maxCharacters * 6, // 6 indices per character (2 triangles)
				BufferUsage.WriteOnly
			);
		}

		public void UpdateText(VertexFont[] vertices, short[] indices)
		{
			if (vertices.Length > 0)
			{
				VertexBuffer.SetData(vertices);
			}

			if (indices.Length > 0)
			{
				IndexBuffer.SetData(indices);
			}
		}

		public void Dispose()
		{
			VertexBuffer?.Dispose();
			IndexBuffer?.Dispose();
		}
	}

	// Utility class for texture loading (you'll need to implement this based on your needs)
	public static class TextureLoader
	{
		public static Texture2D LoadTexture(GraphicsDevice graphicsDevice, string path)
		{
			// Implementation depends on your texture loading strategy
			// You can use Texture2D.FromFile or MonoGame's content pipeline
			return Texture2D.FromFile(graphicsDevice, path);
		}
	}
}
