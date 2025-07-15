using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoMSDF
{
	[StructLayout(LayoutKind.Sequential)]
	public struct VertexFont : IVertexType
	{
		public Vector2 Position;
		public Vector2 TextureCoordinate;
		public uint FillColor;
		public uint StrokeColor;
		public float VertexID;

		public VertexFont(Vector2 pos, Vector2 tc, Color fillColor, Color strokeColor, uint vertexId)
		{
			Position = pos;
			TextureCoordinate = tc;
			FillColor = fillColor.PackedValue;
			StrokeColor = strokeColor.PackedValue;
			VertexID = (float)vertexId;
		}

		public VertexDeclaration VertexDeclaration => new(
			new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
			new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
			new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0),
			new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1),
			new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 7)
		);
	}
}
