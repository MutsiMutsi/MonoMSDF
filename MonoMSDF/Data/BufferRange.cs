namespace MonoMSDF
{
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
}
