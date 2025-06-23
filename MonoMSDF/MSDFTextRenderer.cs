using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace MonoMSDF
{
	public class MSDFTextRenderer : IDisposable
	{
		private readonly GraphicsDevice graphicsDevice;
		private readonly Effect msdifEffect;

		public Texture2D atlasTexture;
		private readonly SamplerState samplerState;

		private GlyphAtlas glyphAtlas;
		private readonly Dictionary<int, Glyph> glyphs = [];
		private readonly Dictionary<(char, char), float> kerningPairs = [];

		public DynamicTextBuffer TextBuffer;

		private int instanceCount = 0;
		private int geometryCount = 0;
		private int oneshotCount = 0;

		List<(BufferRange range, int lifeTime)> oneShotBuffers = [];

		EffectParameter matrixParameter;
		Dictionary<FontDrawType, EffectTechnique> drawTechniques = [];

		public TextStylizer Stylizer = new TextStylizer();
		private List<(GlyphStyle style, int length)> activeStyles = [];

		[StructLayout(LayoutKind.Sequential)]
		public struct DrawGlyph
		{
			public char Character;
			public ushort StyleIndex;
			public ushort StyleLength;

			public DrawGlyph(char character, ushort styleIndex, ushort styleLength)
			{
				Character = character;
				StyleIndex = styleIndex;
				StyleLength = styleLength;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		ref struct ProcessedText
		{
			public ReadOnlySpan<DrawGlyph> RenderableChars;
			public int RenderableCharCount;

			public ProcessedText(ReadOnlySpan<DrawGlyph> renderableChars, int renderableCharCount)
			{
				RenderableChars = renderableChars;
				RenderableCharCount = renderableCharCount;
			}
		}

		public MSDFTextRenderer(GraphicsDevice graphicsDevice, ContentManager contentManager)
		{
			this.graphicsDevice = graphicsDevice;
			this.msdifEffect = contentManager.Load<Effect>("msdf_effect");

			TextBuffer = new DynamicTextBuffer(graphicsDevice, 64);

			// Create sampler state for texture filtering
			samplerState = new SamplerState
			{
				Filter = TextureFilter.Linear,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp
			};

			drawTechniques[FontDrawType.StandardText] = msdifEffect.Techniques["MSDFTextRendering"];
			drawTechniques[FontDrawType.StandardTextWithStroke] = msdifEffect.Techniques["MSDFTextWithStroke"];
			drawTechniques[FontDrawType.TinyText] = msdifEffect.Techniques["MSDFSmallText"];
			drawTechniques[FontDrawType.TinyTextWithStroke] = msdifEffect.Techniques["MSDFTextRendering"];

			matrixParameter = msdifEffect.Parameters["WorldViewProjection"];
		}

		private bool isWriteableChar(char c)
		{
			return (c != ' ' && c != '\n' && c != '|');
		}

		// Preprocessing method that does all parsing upfront
		private ProcessedText preprocessText(ReadOnlySpan<char> text, ref Span<DrawGlyph> tempBuffer)
		{
			int writeIndex = 0;
			int nextEndTagIndex = -1;
			int nextEndTagLength = 0;

			for (int i = 0; i < text.Length; i++)
			{
				ushort currentStyleIndex = 0;
				ushort currentStyleLength = 0;

				char c = text[i];

				if (i == nextEndTagIndex)
				{
					i += nextEndTagLength;
					c = text[i];

					nextEndTagIndex = -1;
					nextEndTagLength = 0;
				}

				// Handle style tags
				if (c == '|')
				{
					if (i == text.Length - 1)
					{
						continue;
					}
					int skipSize = 1;

					if (Stylizer.WordMatcher.TryMatchWord(text, i + 1, out currentStyleLength, out currentStyleIndex))
					{

					}
					else if (Stylizer.TagParser.TryParseTag(text, i + 1, out ushort tagLength, out currentStyleIndex, out ushort startTagLength, out ushort endTagLength))
					{
						nextEndTagIndex = i + tagLength - endTagLength + 1;
						nextEndTagLength = endTagLength;
						skipSize += startTagLength;

						currentStyleLength = (ushort)(tagLength - startTagLength - endTagLength);
					}

					i += skipSize;
					c = text[i];
				}

				// Skip non-renderable characters but still process newlines for layout
				if (c == ' ' || c == '\n')
				{
					// Add whitespace/newline characters but they won't contribute to buffer allocation
					tempBuffer[writeIndex] = new DrawGlyph(c, 0, 0);
					writeIndex++;
					continue;
				}

				// Only add characters that exist in the glyph atlas
				if (!glyphs.TryGetValue(c, out Glyph glyph))
				{
					continue;
				}

				// Add renderable character with current styling
				tempBuffer[writeIndex] = new DrawGlyph(c, currentStyleIndex, currentStyleLength);
				writeIndex++;
			}

			// Count only truly renderable characters (not whitespace/newlines)
			int renderableCount = 0;
			for (int i = 0; i < writeIndex; i++)
			{
				if (isWriteableChar(tempBuffer[i].Character))
				{
					renderableCount++;
				}
			}

			return new ProcessedText(tempBuffer.Slice(0, writeIndex), renderableCount);
		}

		public void AddTextInstance(Vector2 position, float scale, BufferRange buffer)
		{
			Matrix transform = Matrix.CreateScale(scale) * Matrix.CreateTranslation(position.X, position.Y, 0);
			AddTextInstance(transform, scale, buffer);
		}

		public void AddTextInstance(Matrix transform, float scale, BufferRange buffer)
		{
			//Resize instances if we need to.
			if (instanceCount >= TextBuffer.Instances.Length)
			{
				TextBuffer.ResizeInstanceBuffer();
			}

			float sizeInPixel = scale;
			float pointSize = sizeInPixel;
			float sizeInEm = glyphAtlas.Atlas.Size;
			float pixelRange = glyphAtlas.Atlas.DistanceRange;
			float screenPxRange = sizeInPixel / sizeInEm * pixelRange;

			TextBuffer.Instances[instanceCount] = new TextInstance(transform, screenPxRange, glyphAtlas.Atlas.DistanceRange, buffer.VertexOffset, buffer.VertexOffset + buffer.VertexCount);
			instanceCount++;
		}

		public bool LoadAtlas(string jsonPath, string pngPath)
		{
			try
			{
				// Load and parse JSON
				glyphAtlas = FontLoader.Load(jsonPath);
				foreach (Glyph glyph in glyphAtlas.Glyphs)
				{

					//TODO: Is it worth putting these floats in a rectangle or 2x Vector2 instead of just keeping them as floats?
					//x and y position will have to be added at runtime either way.
					//float glyphLeft = x + glyph.PlaneBounds.left;
					//float glyphBottom = y + glyph.PlaneBounds.bottom;
					//float glyphRight = x + glyph.PlaneBounds.right;
					//float glyphTop = y + glyph.PlaneBounds.top;

					float texLeft = glyph.AtlasBounds.left / glyphAtlas.Atlas.Width;
					float texBottom = glyph.AtlasBounds.bottom / glyphAtlas.Atlas.Height;
					float texRight = glyph.AtlasBounds.right / glyphAtlas.Atlas.Width;
					float texTop = glyph.AtlasBounds.top / glyphAtlas.Atlas.Height;

					if (glyphAtlas.Atlas.YOrigin == "bottom")
					{
						texBottom = 1.0f - texBottom;
						texTop = 1.0f - texTop;
					}

					glyph.TextureCoordinates = new Vector2[4];
					glyph.TextureCoordinates[0] = new Vector2(texLeft, texBottom);
					glyph.TextureCoordinates[1] = new Vector2(texRight, texBottom);
					glyph.TextureCoordinates[2] = new Vector2(texRight, texTop);
					glyph.TextureCoordinates[3] = new Vector2(texLeft, texTop);

					glyphs[glyph.Unicode] = glyph;
				}

				foreach (Kerning kernEntry in glyphAtlas.Kerning)
				{
					char left = (char)kernEntry.Unicode1;
					char right = (char)kernEntry.Unicode2;
					float advance = (float)kernEntry.Advance;
					kerningPairs[(left, right)] = advance;
				}

				// Load PNG texture using MonoGame's content pipeline or Texture2D.FromFile
				atlasTexture = FontLoader.LoadTexture(graphicsDevice, pngPath);

				//Set the params
				msdifEffect.Parameters["SpriteTexture"].SetValue(atlasTexture);
				msdifEffect.Parameters["AtlasSize"].SetValue(new Vector2(glyphAtlas.Atlas.Width, glyphAtlas.Atlas.Height));

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to load atlas: {ex.Message}");
				return false;
			}
		}

		public TextGeometryHandle GenerateGeometry(ReadOnlySpan<char> text, Color fillColor, Color strokeColor)
		{
			geometryCount++;

			// Use stackalloc for temporary storage - no heap allocations
			// Assuming reasonable max text length, adjust as needed
			Span<DrawGlyph> tempBuffer = stackalloc DrawGlyph[text.Length];
			var processedText = preprocessText(text, ref tempBuffer);
			var bufferRange = TextBuffer.Allocate(processedText.RenderableCharCount * 4);

			GenerateTextGeometry(
				processedText.RenderableChars,
				bufferRange.VertexOffset, bufferRange.IndexOffset,
				fillColor, strokeColor
			);

			TextBuffer.UpdateText(bufferRange.VertexOffset, bufferRange.VertexCount);

			return new TextGeometryHandle(
				bufferRange
			);
		}

		public TextGeometryHandle ReplaceGeometry(TextGeometryHandle handle, ReadOnlySpan<char> text, Color fillColor, Color strokeColor)
		{
			// Use stackalloc for temporary storage - no heap allocations
			// Assuming reasonable max text length, adjust as needed
			Span<DrawGlyph> tempBuffer = stackalloc DrawGlyph[text.Length];
			var processedText = preprocessText(text, ref tempBuffer);

			//We assume we cant grow where we are now, so we will append a new geo.
			if (handle.BufferRange.VertexCount < processedText.RenderableCharCount * 4)
			{
				TextBuffer.Free(handle.BufferRange);
				//TextBuffer.InvalidateRange(handle.BufferRange.VertexOffset, handle.BufferRange.VertexCount);

				var bufferRange = TextBuffer.Allocate(processedText.RenderableCharCount * 4);

				GenerateTextGeometry(
				   processedText.RenderableChars,
				   bufferRange.VertexOffset, bufferRange.IndexOffset,
				   fillColor, strokeColor
			   );

				TextBuffer.UpdateText(bufferRange.VertexOffset, processedText.RenderableCharCount * 4);

				return new TextGeometryHandle(
					bufferRange
				);
			}

			GenerateTextGeometry(
				processedText.RenderableChars,
				handle.BufferRange.VertexOffset, handle.BufferRange.IndexOffset,
				fillColor, strokeColor
			);

			int lengthDelta = processedText.RenderableCharCount * 4 - handle.BufferRange.VertexCount;
			if (lengthDelta < 0)
			{
				//Invalidate geometryid for the remaining verts.
				int vertStart = handle.BufferRange.VertexOffset + handle.BufferRange.VertexCount + lengthDelta;
				int vertLength = -lengthDelta;

				int indexStart = handle.BufferRange.IndexOffset + (handle.BufferRange.VertexCount / 4 * 6) + (lengthDelta / 4 * 6);
				int indexLength = -(lengthDelta / 4 * 6);

				TextBuffer.Free(new BufferRange(vertStart, vertLength, indexStart, indexLength));
				//TextBuffer.InvalidateRange(vertStart, vertLength);
			}

			handle.BufferRange.VertexCount = processedText.RenderableCharCount * 4;
			handle.BufferRange.IndexCount = processedText.RenderableCharCount * 6;

			TextBuffer.UpdateText(handle.BufferRange.VertexOffset, processedText.RenderableCharCount * 4);

			return new TextGeometryHandle(
				handle.BufferRange.VertexOffset,
				processedText.RenderableCharCount * 4,
				handle.BufferRange.IndexOffset,
				processedText.RenderableCharCount * 6
			);
		}

		public void OneShotText(ReadOnlySpan<char> text, Vector2 position, float sizePx, Color fillColor, Color strokeColor)
		{
			oneshotCount++;

			// Use stackalloc for temporary storage - no heap allocations
			// Assuming reasonable max text length, adjust as needed
			Span<DrawGlyph> tempBuffer = stackalloc DrawGlyph[text.Length];
			var processedText = preprocessText(text, ref tempBuffer);
			var bufferRange = TextBuffer.Allocate(processedText.RenderableCharCount * 4);

			GenerateTextGeometry(
				processedText.RenderableChars,
				bufferRange.VertexOffset, bufferRange.IndexOffset,
				fillColor, strokeColor
			);

			TextBuffer.UpdateText(bufferRange.VertexOffset, bufferRange.VertexCount);
			AddTextInstance(Matrix.CreateScale(sizePx) * Matrix.CreateTranslation(position.X, position.Y, 0f), sizePx, bufferRange);
			oneShotBuffers.Add(new(bufferRange, 1));
		}

		public void RenderInstances(Matrix view, Matrix projection, FontDrawType drawType = FontDrawType.StandardText)
		{
			if (atlasTexture == null || msdifEffect == null || instanceCount == 0)
			{
				return;
			}

			Vector2 xBasis = new Vector2(view.M11, view.M12);
			msdifEffect.Parameters["ZoomLevel"].SetValue(xBasis.Length());

			matrixParameter.SetValue(view * projection);
			msdifEffect.CurrentTechnique = drawTechniques[drawType];
			msdifEffect.CurrentTechnique.Passes[0].Apply();

			// Make sure graphics state is setup proper.
			graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
			graphicsDevice.DepthStencilState = DepthStencilState.None;
			graphicsDevice.BlendState = BlendState.NonPremultiplied;
			graphicsDevice.Textures[0] = atlasTexture;
			graphicsDevice.SamplerStates[0] = samplerState;

			graphicsDevice.Indices = TextBuffer.IndexBuffer;
			TextBuffer.InstanceBuffer.SetData(TextBuffer.Instances, 0, instanceCount, SetDataOptions.NoOverwrite);

			graphicsDevice.SetVertexBuffers(TextBuffer.VertexBufferBindings);

			graphicsDevice.DrawInstancedPrimitives(
				PrimitiveType.TriangleList,
				baseVertex: 0,
				startIndex: 0,
				primitiveCount: TextBuffer.Vertices.Length / 2,
				instanceCount: instanceCount
			);

			//reset for the next frame
			instanceCount = 0;
			oneshotCount = 0;

			//free up the one shot geometry.
			for (int i = 0; i < oneShotBuffers.Count; i++)
			{
				TextBuffer.Free(oneShotBuffers[i].range);
				oneShotBuffers.RemoveAt(i);
				i--;
			}

			return;
		}

		private Color[] fillColorsCache = new Color[4];
		private Color[] strokeColorsCache = new Color[4];

		private int GenerateTextGeometry(
			ReadOnlySpan<DrawGlyph> text,
			int vertexOffset,
			int indexOffset,
			Color fillColor,
			Color strokeColor)
		{
			Vector2 position = Vector2.Zero;
			position.Y = -position.Y;
			float x = position.X;
			float y = position.Y - glyphAtlas.Metrics.Ascender;

			int vertexIndex = vertexOffset;
			int numChars = 0;

			for (int i = 0; i < text.Length; i++)
			{
				DrawGlyph g = text[i];
				char c = g.Character;
				if (c == '\n')
				{
					x = position.X;
					y -= glyphAtlas.Metrics.LineHeight;
				}

				var glyph = glyphs[c];
				if (glyph.PlaneBounds.right == 0)
				{
					x += glyph.Advance;
					continue;
				}

				if (g.StyleLength > 0)
				{
					var newStyle = Stylizer.Styles[g.StyleIndex];
					activeStyles.Add(new(newStyle, g.StyleLength));
				}

				float spacing = 0.0f;
				for (int fc = 0; fc < 4; fc++)
				{
					fillColorsCache[fc] = fillColor;
					strokeColorsCache[fc] = strokeColor;
				}

				for (int j = 0; j < activeStyles.Count; j++)
				{
					if (activeStyles[j].length <= 0)
					{
						activeStyles.RemoveAt(j);
						j--;
						continue;
					}

					var style = activeStyles[j];
					style.length--;
					activeStyles[j] = style;

					//Overwrite based on style
					spacing += style.style.Spacing;

					for (int fc = 0; fc < style.style.Fill?.Length; fc++)
					{
						fillColorsCache[fc] = style.style.Fill[fc];
					}
					for (int fc = 0; fc < style.style.Stroke?.Length; fc++)
					{
						strokeColorsCache[fc] = style.style.Stroke[fc];
					}
				}

				float glyphLeft = x + glyph.PlaneBounds.left;
				float glyphBottom = y + glyph.PlaneBounds.bottom;
				float glyphRight = x + glyph.PlaneBounds.right;
				float glyphTop = y + glyph.PlaneBounds.top;

				TextBuffer.Vertices[vertexIndex] = new VertexFont(new Vector2(glyphLeft, -glyphBottom), glyph.TextureCoordinates[0], fillColorsCache[0], strokeColorsCache[0]);
				TextBuffer.Vertices[vertexIndex + 1] = new VertexFont(new Vector2(glyphRight, -glyphBottom), glyph.TextureCoordinates[1], fillColorsCache[1], strokeColorsCache[1]);
				TextBuffer.Vertices[vertexIndex + 2] = new VertexFont(new Vector2(glyphRight, -glyphTop), glyph.TextureCoordinates[2], fillColorsCache[2], strokeColorsCache[2]);
				TextBuffer.Vertices[vertexIndex + 3] = new VertexFont(new Vector2(glyphLeft, -glyphTop), glyph.TextureCoordinates[3], fillColorsCache[3], strokeColorsCache[3]);

				vertexIndex += 4;

				if (i < text.Length - 2 && kerningPairs.TryGetValue((c, text[i + 1].Character), out float kern))
				{
					x += kern;
				}

				x += glyph.Advance + spacing;
				numChars++;
			}

			return numChars;
		}

		public void Dispose()
		{
			atlasTexture?.Dispose();
			samplerState?.Dispose();
			TextBuffer?.Dispose();
		}
	}
}
