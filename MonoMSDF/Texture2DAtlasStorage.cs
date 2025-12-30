using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpMSDF.Atlas;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MonoMSDF
{
	public class Texture2DAtlasStorage : AtlasStorage, IDisposable
	{
		public RenderTarget2D RenderTarget { get; private set; }

		private static RenderTarget2D blitRT;
		private static Color[] colorData;

		private static GraphicsDevice _graphicsDevice;
		private static SpriteBatch _spriteBatch;

		private bool _disposed = false;

		private static int _maxGlyphSize;

		public Texture2DAtlasStorage()
		{
		}

		public Texture2DAtlasStorage(GraphicsDevice graphicsDevice, int maxGlyphSize)
		{
			_graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
			_spriteBatch = new SpriteBatch(_graphicsDevice);
			_maxGlyphSize = maxGlyphSize;
		}

		public override void Init(int width, int height)
		{
			if (width < 1)
			{
				width = 256;
			}

			if (height < 1)
			{
				height = 256;
			}

			// Create a render target that we can draw to
			RenderTarget = new RenderTarget2D(_graphicsDevice, width, height, false,
				SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

			blitRT = new RenderTarget2D(_graphicsDevice, _maxGlyphSize, _maxGlyphSize, false,
				SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

			colorData = new Color[_maxGlyphSize * _maxGlyphSize];

			// Clear the render target
			_graphicsDevice.SetRenderTarget(RenderTarget);
			_graphicsDevice.Clear(Color.Transparent);
			_graphicsDevice.SetRenderTarget(null);
		}

		public override bool Resize(int width, int height)
		{
			if (RenderTarget.Width >= width)
			{
				return false;
			}

			// Store the old render target
			RenderTarget2D oldRenderTarget = RenderTarget;

			Debug.Write("===RESIZE===");

			// Create a new one with the desired size
			RenderTarget = new RenderTarget2D(_graphicsDevice, width, height, false,
				SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

			// Set new RT as the current render target
			_graphicsDevice.SetRenderTarget(RenderTarget);

			// Clear it (optional, depending on your blending mode)
			_graphicsDevice.Clear(Color.Transparent);

			if (oldRenderTarget != null)
			{
				// Draw the old RT into the new one
				_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);

				// Determine what portion fits
				Rectangle sourceRect = new(0, 0,
					Math.Min(oldRenderTarget.Width, RenderTarget.Width),
					Math.Min(oldRenderTarget.Height, RenderTarget.Height));

				_spriteBatch.Draw(oldRenderTarget, Vector2.Zero, sourceRect, Color.White);
				_spriteBatch.End();

				oldRenderTarget.Dispose(); // Dispose if no longer needed
			}

			_graphicsDevice.SetRenderTarget(null);

			return true;
		}

		public override void Put(int x, int y, BitmapView subBitmap)
		{
			// For bitmap data, we need to create a temporary texture
			// This is less efficient but maintains compatibility with your existing bitmap system
			CreateTextureFromBitmap(subBitmap);
			Put(x, y, blitRT);
		}

		public void Put(int x, int y, Texture2D sourceTexture)
		{
			if (sourceTexture == null || RenderTarget == null)
			{
				return;
			}

			_graphicsDevice.SetRenderTarget(RenderTarget);

			_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
			_spriteBatch.Draw(sourceTexture, new Vector2(x, y), Color.White);
			_spriteBatch.End();

			_graphicsDevice.SetRenderTarget(null);
		}

		public override void Get(int x, int y, BitmapView subBitmap)
		{
			// Getting data back from GPU is expensive and should be avoided if possible
			// This would require reading back from the render target
			throw new NotImplementedException("Reading back from GPU texture is not recommended for performance reasons. Consider redesigning to avoid this operation.");
		}

		// Helper method to convert bitmap data to texture (for compatibility)
		private void CreateTextureFromBitmap(BitmapView bitmap)
		{
			Array.Clear(colorData);

			for (int y = 0; y < bitmap.SubHeight; y++)
			{
				for (int x = 0; x < bitmap.SubWidth; x++)
				{
					int destIndex = (y * _maxGlyphSize) + x;

					colorData[destIndex].R = PixelFloatToByte(bitmap.Get(x, y, 0));
					colorData[destIndex].G = PixelFloatToByte(bitmap.Get(x, y, 1));
					colorData[destIndex].B = PixelFloatToByte(bitmap.Get(x, y, 2));
					colorData[destIndex].A = 255;
				}
			}

			blitRT.SetData(colorData);
		}

		// Efficient method for when you're working directly with textures
		public void PutTexture(int x, int y, Texture2D sourceTexture, Microsoft.Xna.Framework.Rectangle? sourceRect = null, Color? tint = null)
		{
			if (sourceTexture == null || RenderTarget == null)
			{
				return;
			}

			_graphicsDevice.SetRenderTarget(RenderTarget);

			_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
			_spriteBatch.Draw(sourceTexture, new Vector2(x, y), sourceRect, tint ?? Color.White);
			_spriteBatch.End();

			_graphicsDevice.SetRenderTarget(null);
		}

		// Method to clear the atlas
		public void Clear(Color? clearColor = null)
		{
			if (RenderTarget == null)
			{
				return;
			}

			_graphicsDevice.SetRenderTarget(RenderTarget);
			_graphicsDevice.Clear(clearColor ?? Color.Transparent);
			_graphicsDevice.SetRenderTarget(null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte PixelFloatToByte(float value)
		{
			return (byte)Math.Clamp((int)((value * 255.0f) + 0.5f), 0, 255);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					RenderTarget?.Dispose();
					_spriteBatch?.Dispose();
				}
				_disposed = true;
			}
		}

		public override int Size => RenderTarget.Width;
	}
}

