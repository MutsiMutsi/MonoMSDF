using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Demo.Shared
{
	public class Camera2D
	{
		private Vector2 _position = new(1920 / 4, 1080 / 4);
		private readonly Viewport _viewport;
		public Matrix TransformMatrix { get; private set; }
		public float Zoom { get; private set; } = 1f;

		public Camera2D(Viewport viewport)
		{
			_viewport = viewport;
			UpdateMatrix();
		}

		public void Update(GameTime gameTime)
		{
			UpdateMatrix();
			float sin01 = (MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds) + 1.0f) / 2f;
			Zoom = sin01 * 10f;
		}

		private void UpdateMatrix()
		{
			TransformMatrix = Matrix.CreateTranslation(new Vector3(-_position, 0)) *
							 Matrix.CreateScale(Zoom, Zoom, 1) *
							 Matrix.CreateTranslation(new Vector3(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0));
		}
	}
}
