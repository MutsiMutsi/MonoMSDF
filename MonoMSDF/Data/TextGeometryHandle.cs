namespace MonoMSDF
{
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
}
