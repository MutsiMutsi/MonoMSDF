using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMSDF
{
	public class BufferDebugDraw
	{
		MSDFTextRenderer _textRenderer;
		SpriteBatch _spriteBatch;
		Texture2D nixel;


		public BufferDebugDraw(MSDFTextRenderer textRenderer, SpriteBatch sb, GraphicsDevice gd)
		{
			this._textRenderer = textRenderer;
			this._spriteBatch = sb;

			nixel = new Texture2D(gd, 1, 1);
			nixel.SetData([Color.White]);
		}

		public void DrawVertexBuffer(int posX, int posY, int barHeight)
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
			for (int i = 0; i < _textRenderer.TextBuffer.Vertices.Length; i++)
			{
				Color col = i % 2 == 0 ? new Color(255, 0, 0, 255) : new Color(0, 0, 0, 255);
				_spriteBatch.Draw(nixel, new Rectangle(posX + i, posY, 1, barHeight), col);
			}

			for (int r = 0; r < _textRenderer.TextBuffer.FreeRanges.Count; r++)
			{
				var range = _textRenderer.TextBuffer.FreeRanges[r];
				for (int x = 0; x < range.VertexCount; x++)
				{
					float xpos = posX + x + range.VertexOffset;
					Color col = x % 2 == 0 ? new Color(128, 128, 0, 255) : new Color(0, 128, 128, 255);
					if (x == 0)
					{
						col = Color.Black;
					}
					_spriteBatch.Draw(nixel, new Rectangle((int)xpos, posY, 1, barHeight), col);
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
