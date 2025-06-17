using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoMSDF.Demo
{
	public class Camera2D
	{
		private Vector2 _position;
		private float _zoom = 1f;
		private float _targetZoom = 1f;
		private readonly Viewport _viewport;

		// Zoom configuration
		private const float MinZoom = 0.1f;
		private const float MaxZoom = 50f;
		private const float ZoomSpeed = 0.001f;
		private const float ZoomLerpSpeed = 0.1f;
		private const float ZoomExponent = 1.2f; // Makes zoom non-linear

		public Matrix TransformMatrix { get; private set; }
		public Vector2 Position => _position;
		public float Zoom => _zoom;

		private int lastZoomValue = 0;

		public Camera2D(Viewport viewport)
		{
			_viewport = viewport;
			UpdateMatrix();
		}

		public void Update(GameTime gameTime)
		{
			// Smoothly interpolate toward target zoom
			_zoom = MathHelper.Lerp(_zoom, _targetZoom, ZoomLerpSpeed);
			UpdateMatrix();
		}

		public void HandleInput(MouseState input, GameTime gameTime)
		{
			float zoomDelta = (lastZoomValue - input.ScrollWheelValue) * ZoomSpeed;
			lastZoomValue = input.ScrollWheelValue;

			if (zoomDelta == 0) return;

			// 1. Store current transform and mouse position
			Matrix oldTransform = TransformMatrix;
			Vector2 mouseWorldPosBefore = ScreenToWorld(input.Position.ToVector2(), oldTransform);

			// 2. Calculate new zoom (but don't apply it yet)
			zoomDelta = Math.Sign(zoomDelta) * (float)Math.Pow(Math.Abs(zoomDelta), ZoomExponent);
			float newTargetZoom = Math.Clamp(_targetZoom + zoomDelta, MinZoom, MaxZoom);

			// 3. Calculate what the transform WOULD BE with new zoom
			Matrix newTransform = Matrix.CreateTranslation(new Vector3(-_position, 0)) *
								 Matrix.CreateScale(newTargetZoom, newTargetZoom, 1) *
								 Matrix.CreateTranslation(new Vector3(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0));

			// 4. Find where mouse would be after zoom
			Vector2 mouseWorldPosAfter = ScreenToWorld(input.Position.ToVector2(), newTransform);

			// 5. Now apply the changes
			_targetZoom = newTargetZoom;
			_position += mouseWorldPosBefore - mouseWorldPosAfter;
		}


		private void UpdateMatrix()
		{
			TransformMatrix = Matrix.CreateTranslation(new Vector3(-_position, 0)) *
							 Matrix.CreateScale(_zoom, _zoom, 1) *
							 Matrix.CreateTranslation(new Vector3(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0));
		}

		private Vector2 ScreenToWorld(Vector2 screenPosition, Matrix? transform = null)
		{
			return Vector2.Transform(screenPosition, Matrix.Invert(transform ?? TransformMatrix));
		}

		public Vector2 WorldToScreen(Vector2 worldPosition)
		{
			return Vector2.Transform(worldPosition, TransformMatrix);
		}
	}
}
