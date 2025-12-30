using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoMSDF
{
	public class DynamicTextBuffer : IDisposable
	{
		public List<BufferRange> FreeRanges = new List<BufferRange>();

		private readonly object lockObject = new object();
		private volatile bool disposed = false;

		public int MaxCharacters { get; private set; }
		private readonly GraphicsDevice graphicsDevice;
		public DynamicVertexBuffer VertexBuffer { get; private set; }
		public DynamicIndexBuffer IndexBuffer { get; private set; }
		public DynamicVertexBuffer InstanceBuffer { get; private set; }
		public VertexBufferBinding[] VertexBufferBindings { get; private set; }

		public VertexFont[] Vertices { get; private set; }
		public short[] Indices { get; private set; }
		public TextInstance[] Instances { get; private set; }

		private readonly int vertexFontStride;

		public DynamicTextBuffer(GraphicsDevice graphicsDevice, int maxCharacters)
		{
			lock (lockObject)
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

				GenerateQuadIndicesUnsafe();
			}
		}

		public void UpdateText(int vertexStartIndex, int vertexCount)
		{
			if (disposed) return;

			lock (lockObject)
			{
				if (disposed) return;

				// Bounds checking
				if (vertexStartIndex < 0 || vertexStartIndex + vertexCount > Vertices.Length)
					throw new ArgumentOutOfRangeException(nameof(vertexStartIndex));

				// Vertex buffer
				int vertexOffsetBytes = vertexStartIndex * vertexFontStride;

				VertexBuffer.SetData(
					vertexOffsetBytes,
					Vertices,
					vertexStartIndex,
					vertexCount,
					vertexFontStride,
					SetDataOptions.NoOverwrite
				);
			}
		}

		/*public unsafe void InvalidateRange(int startIndex, int count)
		{
			if (disposed) return;
			
			lock (lockObject)
			{
				if (disposed) return;
				
				int offsetInBytes = startIndex * vertexFontStride;

				// Zero the target range in-place
				fixed (VertexFont* ptr = &Vertices[startIndex])
				{
					Unsafe.InitBlock(ptr, 0, (uint)(count * vertexFontStride));
				}

				// Push the zeroed portion directly to the GPU
				VertexBuffer.SetData(offsetInBytes, Vertices, startIndex, count, vertexFontStride, SetDataOptions.None);
			}
		}*/

		private void ResizeVBOUnsafe()
		{
			var oldMaxCharacters = MaxCharacters;
			MaxCharacters *= 2;

			// Store old buffers for disposal
			var oldVertexBuffer = VertexBuffer;
			var oldIndexBuffer = IndexBuffer;

			// Resize arrays
			var newVertices = Vertices;
			var newIndices = Indices;
			Array.Resize(ref newVertices, newVertices.Length * 2);
			Array.Resize(ref newIndices, newIndices.Length * 2);
			Vertices = newVertices;
			Indices = newIndices;

			try
			{
				// Create new vertex buffer
				VertexBuffer = new DynamicVertexBuffer(
					graphicsDevice,
					typeof(VertexFont),
					Vertices.Length,
					BufferUsage.WriteOnly
				);

				// Copy existing data over
				VertexBuffer.SetData(
					0,
					Vertices,
					0,
					Vertices.Length / 2,
					vertexFontStride,
					SetDataOptions.NoOverwrite
				);

				// Create new index buffer
				IndexBuffer = new DynamicIndexBuffer(
					graphicsDevice,
					IndexElementSize.SixteenBits,
					Indices.Length,
					BufferUsage.WriteOnly
				);

				GenerateQuadIndicesUnsafe();

				// Update vertex buffer bindings
				VertexBufferBindings[0] = new VertexBufferBinding(VertexBuffer, 0, 0);
			}
			catch
			{
				// If creation fails, restore the old state
				MaxCharacters = oldMaxCharacters;
				var revertVertices = Vertices;
				var revertIndices = Indices;
				Array.Resize(ref revertVertices, revertVertices.Length / 2);
				Array.Resize(ref revertIndices, revertIndices.Length / 2);
				Vertices = revertVertices;
				Indices = revertIndices;
				throw;
			}
			finally
			{
				// Dispose old buffers
				oldVertexBuffer?.Dispose();
				oldIndexBuffer?.Dispose();
			}
		}

		public void ResizeVBO()
		{
			if (disposed) return;

			lock (lockObject)
			{
				if (disposed) return;
				ResizeVBOUnsafe();
			}
		}

		private void GenerateQuadIndicesUnsafe()
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
				SetDataOptions.NoOverwrite
			);
		}

		public void GenerateQuadIndices()
		{
			if (disposed) return;

			lock (lockObject)
			{
				if (disposed) return;
				GenerateQuadIndicesUnsafe();
			}
		}

		public void ResizeInstanceBuffer()
		{
			if (disposed) return;

			lock (lockObject)
			{
				if (disposed) return;

				var oldInstanceBuffer = InstanceBuffer;

				try
				{
					var newInstances = Instances;
					Array.Resize(ref newInstances, newInstances.Length * 2);
					Instances = newInstances;

					InstanceBuffer = new DynamicVertexBuffer(
						graphicsDevice,
						TextInstance.VertexDeclaration,
						Instances.Length,
						BufferUsage.WriteOnly
					);

					VertexBufferBindings[1] = new VertexBufferBinding(InstanceBuffer, 0, 1);
				}
				catch
				{
					// Restore old state on failure
					var revertInstances = Instances;
					Array.Resize(ref revertInstances, revertInstances.Length / 2);
					Instances = revertInstances;
					throw;
				}
				finally
				{
					oldInstanceBuffer?.Dispose();
				}
			}
		}

		public BufferRange Allocate(int vertexCount)
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(DynamicTextBuffer));

			lock (lockObject)
			{
				if (disposed)
					throw new ObjectDisposedException(nameof(DynamicTextBuffer));

				return AllocateUnsafe(vertexCount);
			}
		}

		private BufferRange AllocateUnsafe(int vertexCount)
		{
			int indexCount = vertexCount / 4 * 6;

			for (int i = 0; i < FreeRanges.Count; i++)
			{
				BufferRange range = FreeRanges[i];
				if (range.VertexCount >= vertexCount && range.IndexCount >= indexCount)
				{
					// Split the range
					BufferRange allocated = new(range.VertexOffset, vertexCount, range.IndexOffset, indexCount);

					// Adjust the remaining free range
					range.VertexOffset += vertexCount;
					range.IndexOffset += indexCount;
					range.VertexCount -= vertexCount;
					range.IndexCount -= indexCount;

					if (range.VertexCount == 0 || range.IndexCount == 0)
					{
						FreeRanges.RemoveAt(i);
					}
					else
					{
						FreeRanges[i] = range;
					}

					return allocated;
				}
			}

			// Need to resize
			ResizeVBOUnsafe();
			FreeUnsafe(new(Vertices.Length / 2, Vertices.Length / 2, Indices.Length / 2, Indices.Length / 2));
			return AllocateUnsafe(vertexCount);
		}

		public void Free(BufferRange range)
		{
			if (disposed) return;

			lock (lockObject)
			{
				if (disposed) return;
				FreeUnsafe(range);
			}
		}

		private void FreeUnsafe(BufferRange range)
		{
			// Add the range to free list
			FreeRanges.Add(range);
			FreeRanges.Sort((a, b) => a.VertexOffset.CompareTo(b.VertexOffset));

			// Merge adjacent ranges
			for (int i = 0; i < FreeRanges.Count - 1;)
			{
				BufferRange current = FreeRanges[i];
				BufferRange next = FreeRanges[i + 1];

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

		// Thread-safe property accessors for read-only data
		public int GetMaxCharacters()
		{
			return MaxCharacters; // MaxCharacters is now private set, so reads are safe
		}

		public VertexBufferBinding[] GetVertexBufferBindings()
		{
			lock (lockObject)
			{
				// Return a copy to prevent external modification
				return (VertexBufferBinding[])VertexBufferBindings.Clone();
			}
		}

		public void Dispose()
		{
			if (disposed) return;

			lock (lockObject)
			{
				if (disposed) return;

				disposed = true;

				VertexBuffer?.Dispose();
				IndexBuffer?.Dispose();
				InstanceBuffer?.Dispose();

				VertexBuffer = null;
				IndexBuffer = null;
				InstanceBuffer = null;
				VertexBufferBindings = null;
				Vertices = null;
				Indices = null;
				Instances = null;
				FreeRanges?.Clear();
				FreeRanges = null;
			}
		}
	}
}