using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMSDF
{
	public class BufferDebugDraw
	{
		MSDFTextRenderer _textRenderer;
		Texture2D nixel;

		public BufferDebugDraw(MSDFTextRenderer textRenderer, GraphicsDevice gd)
		{
			this._textRenderer = textRenderer;
			nixel = new Texture2D(gd, 1, 1);
			nixel.SetData([Color.White]);
		}

		public void DrawVertexBuffer(SpriteBatch sb, int posX, int posY, int barHeight, int maxWidth = 1920)
		{
			int totalSize = _textRenderer.TextBuffer.Vertices.Length;
			int usedVertices = 0;
			var freeRanges = _textRenderer.TextBuffer.FreeRanges;
			for (int i = 0; i < freeRanges.Count; i++)
			{
				usedVertices += freeRanges[i].VertexCount;
			}
			int totalUsed = totalSize - usedVertices;
			//_spriteBatch.DrawString(_fontSharp.GetFont(16), $"Vertex Buffer [{totalUsed}/{totalSize}]", new Vector2(posX, posY - 18), Color.White);

			// Draw the main vertex buffer with wrapping
			for (int i = 0; i < _textRenderer.TextBuffer.Vertices.Length; i++)
			{
				int currentLine = i / maxWidth;
				int xOffset = i % maxWidth;
				int currentX = posX + xOffset;
				int currentY = posY + (currentLine * (barHeight + 2)); // +2 for small gap between lines

				Color col = i % 2 == 0 ? new Color(255, 0, 0, 255) : new Color(0, 0, 0, 255);
				sb.Draw(nixel, new Rectangle(currentX, currentY, 1, barHeight), col);
			}

			// Draw the free ranges with wrapping
			for (int r = 0; r < _textRenderer.TextBuffer.FreeRanges.Count; r++)
			{
				var range = _textRenderer.TextBuffer.FreeRanges[r];
				for (int x = 0; x < range.VertexCount; x++)
				{
					int absoluteIndex = x + range.VertexOffset;
					int currentLine = absoluteIndex / maxWidth;
					int xOffset = absoluteIndex % maxWidth;
					int currentX = posX + xOffset;
					int currentY = posY + (currentLine * (barHeight + 2)); // +2 for small gap between lines

					Color col = x % 2 == 0 ? new Color(128, 128, 0, 255) : new Color(0, 128, 128, 255);
					if (x == 0)
					{
						col = Color.Black;
					}
					sb.Draw(nixel, new Rectangle(currentX, currentY, 1, barHeight), col);
				}
			}
		}

		private void DrawIndexBuffer(int posX, int posY, int barHeight)
		{
			/*int totalUsed = textHandles.Sum(o => o.BufferRange.IndexCount);
			int totalSize = _textRenderer.TextBuffer.Indices.Length;

			//_spriteBatch.DrawString(_fontSharp.GetFont(16), $"Index Buffer [{totalUsed}/{totalSize}]", new Vector2(posX, posY - 18), Color.White);
			for (int i = 0; i < _textRenderer.TextBuffer.Indices.Length; i++)
			{
				Color col = i % 2 == 0 ? new Color(128, 128, 0, 255) : new Color(0, 128, 128, 255);
				_spriteBatch.Draw(nixel, new Rectangle(posX + i, posY, 1, barHeight), col);
			}
			for (int i = 0; i < textHandles.Count; i++)
			{
				for (int x = 0; x < textHandles[i].BufferRange.IndexCount; x++)
				{
					Color col = x % 2 == 0 ? new Color(255, 64, 0, 255) : new Color(255 - 64, 64, 64, 255);
					if (x == 0)
					{
						col = Color.Black;
					}
					_spriteBatch.Draw(nixel, new Rectangle(posX + x + textHandles[i].BufferRange.IndexOffset, posY, 1, barHeight), col);
				}
			}
		}*/
		}
	}
}
