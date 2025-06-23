namespace MonoMSDF
{
	public struct TextGeometryHandle
	{
		public BufferRange BufferRange;

		public TextGeometryHandle(int vertexOffset, int vertexCount, int indexOffset, int indexCount)
		{
			BufferRange = new BufferRange(vertexOffset, vertexCount, indexOffset, indexCount);
		}

		public TextGeometryHandle(BufferRange range)
		{
			BufferRange = range;
		}
	}
}
