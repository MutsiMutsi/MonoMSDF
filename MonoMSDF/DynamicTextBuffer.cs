using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MonoMSDF
{
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
		public List<BufferRange> FreeRanges = new();


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

			FreeRanges.Add(new BufferRange(0, Vertices.Length, 0, Indices.Length));

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

			//Copy data over.
			VertexBuffer.SetData(
				0,
				Vertices,
				0,
				Vertices.Length / 2,
				vertexFontStride,
				SetDataOptions.NoOverwrite
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

			IndexBuffer.SetData(
				0,
				Indices,
				0,
				MaxCharacters * 6,
				SetDataOptions.None
			);
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

			ResizeVBO();
			Free(new(Vertices.Length / 2, Vertices.Length / 2, Indices.Length / 2, Indices.Length / 2));
			return Allocate(vertexCount);
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

		public void Dispose()
		{
			VertexBuffer?.Dispose();
			IndexBuffer?.Dispose();
		}
	}
}
