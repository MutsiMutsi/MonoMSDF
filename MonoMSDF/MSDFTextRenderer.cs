using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;

namespace MonoMSDF
{

	[StructLayout(LayoutKind.Sequential, Pack = 0)]
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

		public DynamicTextBuffer TextBuffer;

		private int numIndices = 0;
		private float sizeInPixel = 0;

		private readonly VertexFont[] vertices;
		private readonly short[] indices;

		public MSDFTextRenderer(GraphicsDevice graphicsDevice, Effect msdifEffect)
		{
			this.graphicsDevice = graphicsDevice;
			this.msdifEffect = msdifEffect;


			vertices = new VertexFont[1000 * 4];
			indices = new short[1000 * 6];
			TextBuffer = new DynamicTextBuffer(graphicsDevice, 1000);

			// Create sampler state for texture filtering
			samplerState = new SamplerState
			{
				Filter = TextureFilter.Linear,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp
			};
		}

		public bool LoadAtlas(string jsonPath, string pngPath)
		{
			try
			{
				// Load and parse JSON
				glyphAtlas = FontLoader.Load(jsonPath);
				foreach (Glyph glyph in glyphAtlas.Glyphs)
				{
					glyphs[glyph.unicode] = glyph;
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

		public void GenerateGeometry(string text, Vector2 position, float scale, Color fillColor, Color strokeColor)
		{
			sizeInPixel = scale;
			_ = text.Length * 4;
			numIndices = text.Length * 6;

			float dpi = 96.0f;
			float pointSize = scale;
			float actualScale = dpi / 72.0f * pointSize;

			GenerateTextGeometry(text, position, actualScale, vertices, indices, fillColor, strokeColor);
			TextBuffer.UpdateText(vertices, indices);
		}

		public void RenderText(Matrix world, Matrix view, Matrix projection, FontDrawType drawType = FontDrawType.StandardText)
		{
			if (atlasTexture == null || msdifEffect == null)
			{
				return;
			}


			float sizeInEm = glyphAtlas.Atlas.size;
			float pixelRange = glyphAtlas.Atlas.distanceRange;
			float screenPxRange = sizeInPixel / sizeInEm * pixelRange;

			// Set effect parameters
			msdifEffect.Parameters["WorldViewProjection"].SetValue(world * view * projection);
			msdifEffect.Parameters["ScreenPxRange"].SetValue(screenPxRange);
			msdifEffect.Parameters["SpriteTexture"].SetValue(atlasTexture);

			msdifEffect.Parameters["AtlasSize"].SetValue(new Vector2(glyphAtlas.Atlas.width, glyphAtlas.Atlas.height));
			msdifEffect.Parameters["DistanceRange"].SetValue(glyphAtlas.Atlas.distanceRange);


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
			if (TextBuffer.VertexBuffer != null && TextBuffer.IndexBuffer != null && numIndices > 0)
			{
				// Set vertex buffer
				graphicsDevice.SetVertexBuffer(TextBuffer.VertexBuffer);
				graphicsDevice.Indices = TextBuffer.IndexBuffer;

				// Make sure graphics state is setup proper.
				graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
				graphicsDevice.DepthStencilState = DepthStencilState.None;
				graphicsDevice.BlendState = BlendState.NonPremultiplied;
				graphicsDevice.Textures[0] = atlasTexture;

				// Draw indexed primitives
				graphicsDevice.DrawIndexedPrimitives(
					PrimitiveType.TriangleList,
					0, // base vertex
					0, // start index
					numIndices / 3 // primitive count
				);
			}
		}

		private void GenerateTextGeometry(string text, Vector2 position, float scale, VertexFont[] vertices, short[] indices, Color fillColor, Color strokeColor)
		{
			position.Y = -position.Y;
			float x = position.X;
			float y = position.Y - sizeInPixel;

			int vertexIndex = 0;
			int indicesIndex = 0;

			float progress = 0f;
			float stepSize = 1.0f / text.Length;

			foreach (char c in text)
			{
				if (c == '\n')
				{
					x = position.X;
					y -= scale * glyphAtlas.Metrics.lineHeight;
				}

				if (!glyphs.TryGetValue(c, out Glyph glyph))
				{
					continue;
				}

				if (glyph.planeBounds.right == 0)
				{
					x += glyph.advance * scale;
					continue;
				}

				// Calculate glyph quad positions
				float glyphLeft = x + (glyph.planeBounds.left * scale);
				float glyphBottom = y + (glyph.planeBounds.bottom * scale);
				float glyphRight = x + (glyph.planeBounds.right * scale);
				float glyphTop = y + (glyph.planeBounds.top * scale);

				// Calculate texture coordinates
				float texLeft = glyph.atlasBounds.left / glyphAtlas.Atlas.width;
				float texBottom = glyph.atlasBounds.bottom / glyphAtlas.Atlas.height;
				float texRight = glyph.atlasBounds.right / glyphAtlas.Atlas.width;
				float texTop = glyph.atlasBounds.top / glyphAtlas.Atlas.height;

				// Flip Y coordinates if atlas origin is bottom
				if (glyphAtlas.Atlas.yOrigin == "bottom")
				{
					texBottom = 1.0f - texBottom;
					texTop = 1.0f - texTop;
				}

				Color stroke = strokeColor;
				if (c == 'a')
				{
					stroke = Color.Red;
				}

				if (c == 'e')
				{
					stroke = Color.Red;
				}
				vertices[vertexIndex] = new VertexFont(
					new Vector2(glyphLeft, -glyphBottom),
					new Vector2(texLeft, texBottom),
					fillColor * progress,
					stroke
				);
				if (c == 'e')
				{
					stroke = Color.Green;
				}
				vertices[vertexIndex + 1] = new VertexFont(
					new Vector2(glyphRight, -glyphBottom),
					new Vector2(texRight, texBottom),
					fillColor * progress,
					stroke
				);
				if (c == 'e')
				{
					stroke = Color.Blue;
				}
				vertices[vertexIndex + 2] = new VertexFont(
					new Vector2(glyphRight, -glyphTop),
					new Vector2(texRight, texTop),
					fillColor * progress,
					stroke
				);
				vertices[vertexIndex + 3] = new VertexFont(
					new Vector2(glyphLeft, -glyphTop),
					new Vector2(texLeft, texTop),
					fillColor * progress,
					stroke
				);

				// Create indices for two triangles forming a quad
				indices[indicesIndex++] = (short)vertexIndex;
				indices[indicesIndex++] = (short)(vertexIndex + 2);
				indices[indicesIndex++] = (short)(vertexIndex + 1);
				indices[indicesIndex++] = (short)vertexIndex;
				indices[indicesIndex++] = (short)(vertexIndex + 3);
				indices[indicesIndex++] = (short)(vertexIndex + 2);

				vertexIndex += 4;
				x += glyph.advance * scale;

				progress += stepSize;
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
