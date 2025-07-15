using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoMSDF
{
	[StructLayout(LayoutKind.Sequential)]
	public struct TextInstance
	{
		public Matrix WorldTransform;   // 64 bytes
		public Vector2 PixelRanges;     // 8 bytes  
		public Vector2 VertexRange;     // 8 bytes  

		public TextInstance(Matrix world, float screenPxRange, float distanceRange, int vertexStartId, int vertexEndId)
		{
			PixelRanges.X = screenPxRange;
			PixelRanges.Y = distanceRange;
			WorldTransform = world;

			VertexRange.X = vertexStartId;
			VertexRange.Y = vertexEndId;
		}

		public static readonly VertexDeclaration VertexDeclaration = new(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
			new VertexElement(64, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5),
			new VertexElement(72, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 6)
		);
	}
}
