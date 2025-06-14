using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoMSDF
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct VertexFont : IVertexType
	{
		public Vector2 Position;
		public Vector2 TextureCoordinate;
		public Color FillColor;
		public Color StrokeColor;
		public float GeometryID;

		public VertexFont(Vector2 pos, Vector2 tc, Color fillColor, Color strokeColor, float geometryId)
		{
			Position = pos;
			TextureCoordinate = tc;
			FillColor = fillColor;
			StrokeColor = strokeColor;
			GeometryID = geometryId;
		}

		public VertexDeclaration VertexDeclaration => new(
			new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
			new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
			new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0),
			new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1),
			new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 7)
		);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct TextInstance
	{
		public Matrix WorldTransform;      // 64 bytes
		public Vector2 PixelRanges;        // 8 bytes  
		public float InstanceID;           // 4 bytes

		public TextInstance(Matrix world, float screenPxRange, float distanceRange, float instanceId)
		{
			PixelRanges.X = screenPxRange;
			PixelRanges.Y = distanceRange;
			WorldTransform = world;
			InstanceID = instanceId;
		}

		public static readonly VertexDeclaration VertexDeclaration = new(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
			new VertexElement(64, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5),
			new VertexElement(72, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 6)
		);
	}

	public enum FontDrawType
	{
		StandardText,
		StandardTextWithStroke,
		TinyText,
		TinyTextWithStroke,
	}

	public struct TextGeometryHandle
	{
		public int GeometryID;
		public BufferRange BufferRange;

		public TextGeometryHandle(int geometryID, int vertexOffset, int vertexCount, int indexOffset, int indexCount)
		{
			GeometryID = geometryID;
			BufferRange = new BufferRange(vertexOffset, vertexCount, indexOffset, indexCount);
		}

		public TextGeometryHandle(int geometryID, BufferRange range)
		{
			GeometryID = geometryID;
			BufferRange = range;
		}
	}

	public struct BufferRange
	{
		public int VertexOffset;
		public int VertexCount;
		public int IndexOffset;
		public int IndexCount;

		public BufferRange(int vertexOffset, int vertexCount, int indexOffset, int indexCount)
		{
			VertexOffset = vertexOffset;
			VertexCount = vertexCount;
			IndexOffset = indexOffset;
			IndexCount = indexCount;
		}
	}

	public class BufferAllocator
	{
		public List<BufferRange> FreeRanges = new(); // Sorted by VertexOffset
		private int nextVertexOffset = 0;
		private int nextIndexOffset = 0;
		private DynamicTextBuffer _buffer;

		public BufferAllocator(int initialVertexCapacity, int initialIndexCapacity, DynamicTextBuffer buffer)
		{
			FreeRanges.Add(new BufferRange(0, initialVertexCapacity, 0, initialIndexCapacity));
			this._buffer = buffer;
		}

		public BufferRange Allocate(int vertexCount)
		{
			int indexCount = (vertexCount / 4) * 6;

			for (int i = 0; i < FreeRanges.Count; i++)
			{
				var range = FreeRanges[i];
				if (range.VertexCount >= vertexCount && range.IndexCount >= indexCount)
				{
					// Split the range
					var allocated = new BufferRange(range.VertexOffset, vertexCount, range.IndexOffset, indexCount);

					// Adjust the remaining free range
					range.VertexOffset += vertexCount;
					range.IndexOffset += indexCount;
					range.VertexCount -= vertexCount;
					range.IndexCount -= indexCount;

					if (range.VertexCount == 0 || range.IndexCount == 0)
						FreeRanges.RemoveAt(i);
					else
						FreeRanges[i] = range;

					return allocated;
				}
			}

			//Detect if we're running out of space.
			var lastBuf = FreeRanges[FreeRanges.Count - 1];
			if (nextVertexOffset + vertexCount > lastBuf.VertexOffset + lastBuf.VertexCount)
			{
				Debug.WriteLine($"Old VBO size {_buffer.Vertices.Length}");
				_buffer.ResizeVBO();
				lastBuf.VertexCount += _buffer.Vertices.Length / 2;
				lastBuf.IndexCount += _buffer.Indices.Length / 2;
				FreeRanges[FreeRanges.Count - 1] = lastBuf;

				Debug.WriteLine($"{nextVertexOffset + vertexCount} more than {lastBuf.VertexOffset + lastBuf.VertexCount}");
				Debug.WriteLine($"New VBO size {_buffer.Vertices.Length}");
			}

			// Fallback: append at end
			var fallback = new BufferRange(nextVertexOffset, vertexCount, nextIndexOffset, indexCount);
			nextVertexOffset += vertexCount;
			nextIndexOffset += indexCount;

			return fallback;
		}

		public void Free(BufferRange range)
		{
			// Merge with adjacent free blocks if needed
			FreeRanges.Add(range);
			FreeRanges.Sort((a, b) => a.VertexOffset.CompareTo(b.VertexOffset));

			// Optional: merge adjacent ranges (simple linear pass)
			for (int i = 0; i < FreeRanges.Count - 1;)
			{
				var current = FreeRanges[i];
				var next = FreeRanges[i + 1];

				if (current.VertexOffset + current.VertexCount == next.VertexOffset &&
					current.IndexOffset + current.IndexCount == next.IndexOffset)
				{
					// Merge
					current.VertexCount += next.VertexCount;
					current.IndexCount += next.IndexCount;
					FreeRanges[i] = current;
					FreeRanges.RemoveAt(i + 1);
				}
				else
				{
					i++;
				}
			}
		}
	}

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
		private int geometryCount = 1;

		public MSDFTextRenderer(GraphicsDevice graphicsDevice, Effect msdifEffect)
		{
			this.graphicsDevice = graphicsDevice;
			this.msdifEffect = msdifEffect;

			TextBuffer = new DynamicTextBuffer(graphicsDevice, 64);

			// Create sampler state for texture filtering
			samplerState = new SamplerState
			{
				Filter = TextureFilter.Linear,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp
			};
		}

		public void AddTextInstance(Matrix transform, float scale, int geometryIndex)
		{
			//Resize instances if we need to.
			if (instanceCount >= TextBuffer.Instances.Length)
			{
				TextBuffer.ResizeInstanceBuffer();
			}

			float sizeInPixel = scale;
			float dpi = 96.0f;
			float pointSize = sizeInPixel;
			_ = dpi / 72.0f * pointSize;
			float sizeInEm = glyphAtlas.Atlas.Size;
			float pixelRange = glyphAtlas.Atlas.DistanceRange;
			float screenPxRange = sizeInPixel / sizeInEm * pixelRange;

			TextBuffer.Instances[instanceCount] = new TextInstance(transform, screenPxRange, glyphAtlas.Atlas.DistanceRange, geometryIndex);
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
				atlasTexture = TextureLoader.LoadTexture(graphicsDevice, pngPath);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to load atlas: {ex.Message}");
				return false;
			}
		}

		public TextGeometryHandle GenerateGeometry(string text, Color fillColor, Color strokeColor)
		{
			int renderableCharCount = 0;
			for (int i = 0; i < text.Length; i++)
			{
				renderableCharCount += (text[i] != ' ' && text[i] != '\n') ? 1 : 0;
			}
			var bufferRange = TextBuffer.Allocator.Allocate(renderableCharCount * 4);

			GenerateTextGeometry(
				geometryCount, text,
				bufferRange.VertexOffset, bufferRange.IndexOffset,
				fillColor, strokeColor
			);

			TextBuffer.UpdateText(bufferRange.VertexOffset, bufferRange.VertexCount, bufferRange.IndexOffset, bufferRange.IndexCount);

			geometryCount++;

			return new TextGeometryHandle(
				geometryCount - 1,
				bufferRange
			);
		}

		public TextGeometryHandle ReplaceGeometry(TextGeometryHandle handle, string text, Color fillColor, Color strokeColor)
		{
			int numChars = 0;
			//This is a rough estimate
			if (handle.BufferRange.VertexCount < text.Length * 4)
			{
				//Now we actually count renderable characters, which means skipping \n and whitespace.
				int renderableCharCount = 0;
				for (int i = 0; i < text.Length; i++)
				{
					renderableCharCount += (text[i] != ' ' && text[i] != '\n') ? 1 : 0;
				}

				//We assume we cant grow where we are now, so we will append a new geo.
				if (handle.BufferRange.VertexCount < renderableCharCount * 4)
				{
					TextBuffer.Allocator.Free(handle.BufferRange);
					TextBuffer.InvalidateRange(handle.BufferRange.VertexOffset, handle.BufferRange.VertexCount);

					var bufferRange = TextBuffer.Allocator.Allocate(renderableCharCount * 4);

					numChars = GenerateTextGeometry(
						handle.GeometryID, text,
						bufferRange.VertexOffset, bufferRange.IndexOffset,
						fillColor, strokeColor
					);

					TextBuffer.UpdateText(bufferRange.VertexOffset, numChars * 4, bufferRange.IndexOffset, numChars * 6);

					return new TextGeometryHandle(
						handle.GeometryID,
						bufferRange.VertexOffset,
						renderableCharCount * 4,
						bufferRange.IndexOffset,
						renderableCharCount * 6
					);
				}
			}

			numChars = GenerateTextGeometry(
				handle.GeometryID, text,
				handle.BufferRange.VertexOffset, handle.BufferRange.IndexOffset,
				fillColor, strokeColor
			);

			int lengthDelta = numChars * 4 - handle.BufferRange.VertexCount;
			if (lengthDelta < 0)
			{
				//Invalidate geometryid for the remaining verts.
				int vertStart = handle.BufferRange.VertexOffset + handle.BufferRange.VertexCount + lengthDelta;
				int vertLength = -lengthDelta;

				int indexStart = handle.BufferRange.IndexOffset + (handle.BufferRange.VertexCount / 4 * 6) + (lengthDelta / 4 * 6);
				int indexLength = -(lengthDelta / 4 * 6);

				TextBuffer.Allocator.Free(new BufferRange(vertStart, vertLength, indexStart, indexLength));
				TextBuffer.InvalidateRange(vertStart, vertLength);
			}

			handle.BufferRange.VertexCount = numChars * 4;
			handle.BufferRange.IndexCount = numChars * 6;

			TextBuffer.UpdateText(handle.BufferRange.VertexOffset, numChars * 4, handle.BufferRange.IndexOffset, numChars * 6);

			return new TextGeometryHandle(
				handle.GeometryID,
				handle.BufferRange.VertexOffset,
				numChars * 4,
				handle.BufferRange.IndexOffset,
				numChars * 6
			);
		}

		public void RenderInstances(Matrix view, Matrix projection, FontDrawType drawType = FontDrawType.StandardText)
		{
			if (atlasTexture == null || msdifEffect == null || instanceCount == 0)
			{
				return;
			}

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
			graphicsDevice.SamplerStates[0] = samplerState;

			graphicsDevice.SetVertexBuffers(TextBuffer.VertexBufferBindings);
			graphicsDevice.Indices = TextBuffer.IndexBuffer;

			TextBuffer.InstanceBuffer.SetData(TextBuffer.Instances, 0, instanceCount, SetDataOptions.Discard);

			graphicsDevice.DrawInstancedPrimitives(
				PrimitiveType.TriangleList,
				baseVertex: 0,
				startIndex: 0,
				primitiveCount: TextBuffer.Vertices.Length / 3,
				instanceCount: instanceCount
			);

			instanceCount = 0; // Reset for next frame
			return;
		}

		private int GenerateTextGeometry(
			int geometryId,
			string text,
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

				float glyphLeft = x + glyph.PlaneBounds.left;
				float glyphBottom = y + glyph.PlaneBounds.bottom;
				float glyphRight = x + glyph.PlaneBounds.right;
				float glyphTop = y + glyph.PlaneBounds.top;

				float texLeft = glyph.AtlasBounds.left / glyphAtlas.Atlas.Width;
				float texBottom = glyph.AtlasBounds.bottom / glyphAtlas.Atlas.Height;
				float texRight = glyph.AtlasBounds.right / glyphAtlas.Atlas.Width;
				float texTop = glyph.AtlasBounds.top / glyphAtlas.Atlas.Height;

				if (glyphAtlas.Atlas.YOrigin == "bottom")
				{
					texBottom = 1.0f - texBottom;
					texTop = 1.0f - texTop;
				}

				TextBuffer.Vertices[vertexIndex] = new VertexFont(new Vector2(glyphLeft, -glyphBottom), new Vector2(texLeft, texBottom), fillColor, strokeColor, geometryId);
				TextBuffer.Vertices[vertexIndex + 1] = new VertexFont(new Vector2(glyphRight, -glyphBottom), new Vector2(texRight, texBottom), fillColor, strokeColor, geometryId);
				TextBuffer.Vertices[vertexIndex + 2] = new VertexFont(new Vector2(glyphRight, -glyphTop), new Vector2(texRight, texTop), fillColor, strokeColor, geometryId);
				TextBuffer.Vertices[vertexIndex + 3] = new VertexFont(new Vector2(glyphLeft, -glyphTop), new Vector2(texLeft, texTop), fillColor, strokeColor, geometryId);

				vertexIndex += 4;

				if (i < text.Length - 2 && kerningPairs.TryGetValue((text[i], text[i + 1]), out float kern))
				{
					x += kern;
				}

				x += glyph.Advance;
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

	// Supporting class for dynamic text buffer management
	public class DynamicTextBuffer : IDisposable
	{
		public int MaxCharacters;
		private readonly GraphicsDevice graphicsDevice;
		public DynamicVertexBuffer VertexBuffer;
		public DynamicIndexBuffer IndexBuffer;
		public DynamicVertexBuffer InstanceBuffer;
		public VertexBufferBinding[] VertexBufferBindings;

		public VertexFont[] Vertices;
		public short[] Indices;
		public TextInstance[] Instances;

		private readonly int vertexFontStride;
		public BufferAllocator Allocator;

		public DynamicTextBuffer(GraphicsDevice graphicsDevice, int maxCharacters)
		{
			MaxCharacters = maxCharacters;
			vertexFontStride = Marshal.SizeOf<VertexFont>();

			this.graphicsDevice = graphicsDevice;

			Vertices = new VertexFont[MaxCharacters * 4];
			Indices = new short[MaxCharacters * 6];
			Instances = new TextInstance[16];

			// Create dynamic vertex buffer
			VertexBuffer = new DynamicVertexBuffer(
				graphicsDevice,
				typeof(VertexFont),
				MaxCharacters * 4, // 4 vertices per character
				BufferUsage.WriteOnly
			);

			// Create dynamic index buffer
			IndexBuffer = new DynamicIndexBuffer(
				graphicsDevice,
				IndexElementSize.SixteenBits,
				MaxCharacters * 6, // 6 indices per character (2 triangles)
				BufferUsage.WriteOnly
			);

			// Create a dynamic instance buffer
			InstanceBuffer = new DynamicVertexBuffer(
				graphicsDevice,
				TextInstance.VertexDeclaration,
				Instances.Length,
				BufferUsage.WriteOnly
			);

			VertexBufferBindings = new VertexBufferBinding[2];
			VertexBufferBindings[0] = new VertexBufferBinding(VertexBuffer, 0, 0);
			VertexBufferBindings[1] = new VertexBufferBinding(InstanceBuffer, 0, 1);

			Allocator = new BufferAllocator(Vertices.Length, Indices.Length, this);

			GenerateQuadIndices();
		}

		public void UpdateText(int vertexStartIndex, int vertexCount, int indexStartIndex, int indexCount)
		{
			// Vertex buffer
			int vertexOffsetBytes = vertexStartIndex * vertexFontStride;

			VertexBuffer.SetData(
				vertexOffsetBytes,
				Vertices,
				vertexStartIndex,
				vertexCount,
				vertexFontStride,
				SetDataOptions.None
			);

			// Index buffer
			int indexOffsetBytes = indexStartIndex * sizeof(ushort);

			IndexBuffer.SetData(
				indexOffsetBytes,
				Indices,
				indexStartIndex,
				indexCount,
				SetDataOptions.None
			);
		}

		public unsafe void InvalidateRange(int startIndex, int count)
		{
			int offsetInBytes = startIndex * vertexFontStride;

			// Zero the target range in-place
			fixed (VertexFont* ptr = &Vertices[startIndex])
			{
				Unsafe.InitBlock(ptr, 0, (uint)(count * vertexFontStride));
			}

			// Push the zeroed portion directly to the GPU
			VertexBuffer.SetData(offsetInBytes, Vertices, startIndex, count, vertexFontStride, SetDataOptions.None);
		}

		public void ResizeVBO()
		{
			MaxCharacters *= 2;

			Array.Resize(ref Vertices, Vertices.Length * 2);
			Array.Resize(ref Indices, Indices.Length * 2);

			// Create dynamic vertex buffer
			VertexBuffer = new DynamicVertexBuffer(
				graphicsDevice,
				typeof(VertexFont),
				Vertices.Length, // 4 vertices per character
				BufferUsage.WriteOnly
			);

			// Create dynamic index buffer
			IndexBuffer = new DynamicIndexBuffer(
				graphicsDevice,
				IndexElementSize.SixteenBits,
				Indices.Length, // 6 indices per character (2 triangles)
				BufferUsage.WriteOnly
			);

			GenerateQuadIndices();

			VertexBufferBindings[0] = new VertexBufferBinding(VertexBuffer, 0, 0);
		}

		public void GenerateQuadIndices()
		{
			int indicesIndex = 0;
			int vertexIndex = 0;

			for (int i = 0; i < MaxCharacters; i++)
			{
				Indices[indicesIndex++] = (short)(vertexIndex + 0);
				Indices[indicesIndex++] = (short)(vertexIndex + 2);
				Indices[indicesIndex++] = (short)(vertexIndex + 1);

				Indices[indicesIndex++] = (short)(vertexIndex + 0);
				Indices[indicesIndex++] = (short)(vertexIndex + 3);
				Indices[indicesIndex++] = (short)(vertexIndex + 2);

				vertexIndex += 4;
			}
		}

		public void ResizeInstanceBuffer()
		{
			Array.Resize(ref Instances, Instances.Length * 2);

			InstanceBuffer = new DynamicVertexBuffer(
				graphicsDevice,
				TextInstance.VertexDeclaration,
				Instances.Length,
				BufferUsage.WriteOnly
			);
			VertexBufferBindings[1] = new VertexBufferBinding(InstanceBuffer, 0, 1);
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
