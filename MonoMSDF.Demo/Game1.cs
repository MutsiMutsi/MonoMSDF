using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMSDF;
using MonoMSDF.Demo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MGSDF_Demo
{
	public class FrameCounter
	{
		public long TotalFrames { get; private set; }
		public float TotalSeconds { get; private set; }
		public float AverageFramesPerSecond { get; private set; }
		public float CurrentFramesPerSecond { get; private set; }

		public const int MaximumSamples = 8192;

		private Queue<float> _sampleBuffer = new();

		public void Update(GameWindow window, float deltaTime)
		{
			CurrentFramesPerSecond = 1.0f / deltaTime;

			_sampleBuffer.Enqueue(CurrentFramesPerSecond);

			if (_sampleBuffer.Count > MaximumSamples)
			{
				_sampleBuffer.Dequeue();
				AverageFramesPerSecond = _sampleBuffer.Average(i => i);
			}
			else
			{
				AverageFramesPerSecond = CurrentFramesPerSecond;
			}

			TotalFrames++;
			TotalSeconds += deltaTime;

			if (TotalFrames % 100 == 0)
			{
				var fps = string.Format("FPS: {0}", AverageFramesPerSecond);
				window.Title = fps;
			}
		}
	}

	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private MSDFTextRenderer _textRenderer;
		private FontSystem _fontSharp;
		private SpriteFont _spriteFont;

		private List<TextGeometryHandle> textHandles = new List<TextGeometryHandle>();

		private Texture2D nixel;

		private FrameCounter _frameCounter = new FrameCounter();

		private static float totalTime = 0f;

		public Camera2D camera;


		class MyCustomStyle : GlyphStyle
		{
			private Color[] fillColors = new Color[4];
			private Color[] rotatedFillColors = new Color[4];

			private Color[] strokeColors = new Color[4];
			private Color[] rotatedStrokeColors = new Color[4];

			private float rotationSpeed = 5f;

			public override float Spacing => (MathF.Sin(totalTime * 3f) + 1.0f) / 2.0f * 0.1f;
			public override Color[] Fill => rotatedFillColors;
			public override Color[] Stroke => rotatedStrokeColors;

			public MyCustomStyle()
			{
				fillColors[0] = Color.MonoGameOrange;
				fillColors[1] = Color.MonoGameOrange;
				fillColors[2] = Color.CornflowerBlue;
				fillColors[3] = Color.CornflowerBlue;

				strokeColors[3] = Color.MonoGameOrange;
				strokeColors[2] = Color.MonoGameOrange;
				strokeColors[1] = Color.CornflowerBlue;
				strokeColors[0] = Color.CornflowerBlue;
			}

			public void Update()
			{
				// Calculate rotation phase (0 to 4, cycling)
				float rotationPhase = (totalTime * rotationSpeed) % 4.0f;

				// For each vertex, calculate its rotated color
				for (int i = 0; i < 4; i++)
				{
					// Calculate which colors to interpolate between
					float sourceIndex = (i + rotationPhase) % 4.0f;
					int currentIndex = (int)sourceIndex;
					int nextIndex = (currentIndex + 1) % 4;

					// Calculate interpolation factor
					float t = sourceIndex - currentIndex;

					// Interpolate between the two colors
					rotatedFillColors[i] = Color.Lerp(fillColors[currentIndex], fillColors[nextIndex], t);

					rotatedStrokeColors[i] = Color.Lerp(Color.Goldenrod * 0.5f, Color.Yellow * 0f, (MathF.Sin(totalTime * 5f) + 1.0f) / 2f);
				}
			}
		}

		//Dull aquatic palette:
		//https://lospec.com/palette-list/dull-aquatic
		public static Color[] Palette =
		[
			new Color(55, 47, 58),
			new Color(70, 68, 89),
			new Color(84, 94, 114),
			new Color(93, 118, 128),
			new Color(106, 147, 149),
			new Color(123, 173, 159),
			new Color(142, 178, 154),
			new Color(179, 198, 180),
			new Color(197, 210, 206),
			new Color(211, 216, 217),
		];

		public static Random rng = new Random();

		private string[] randomStrings = new string[1024];

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.PreferredBackBufferHeight = 1080;

			IsFixedTimeStep = false;
			_graphics.SynchronizeWithVerticalRetrace = false;
			InactiveSleepTime = TimeSpan.Zero;
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here
			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			Effect fx = Content.Load<Effect>("msdf_effect");

			// TODO: use this.Content to load your game content here
			_textRenderer = new MSDFTextRenderer(GraphicsDevice, fx);
			_textRenderer.LoadAtlas("king.json", "king.png");

			_fontSharp = new FontSystem();
			_fontSharp.AddFont(File.ReadAllBytes(@"C:\Users\mitch\Documents\Repositories\VelCore\VelCore\lib\msdf-atlas-gen\Kingthings_Petrock.ttf"));

			_spriteFont = Content.Load<SpriteFont>("consola");

			nixel = new Texture2D(GraphicsDevice, 1, 1);
			nixel.SetData([Color.White]);

			for (int i = 0; i < 1024; i++)
			{
				int length = rng.Next(1, 64);
				randomStrings[i] = RandomAsciiString(length);
			}


			excaliburStyle = new MyCustomStyle();
			_textRenderer.Stylizer.Styles.Add(excaliburStyle);

			camera = new Camera2D(GraphicsDevice.Viewport);

			for (int i = 0; i < 100000; i++)
			{
				_textRenderer.Stylizer.AddRule(new StyleRule(
					"Excalibur",
					0
				));
				_textRenderer.Stylizer.AddRule(new StyleRule(
					"EXCALIBUR",
					0
				));
			}

			_textRenderer.Stylizer.AddTag(new TagDefinition(
				"<", ">",
				0
			));

			_textRenderer.Stylizer.AddTag(new TagDefinition(
				"123", "321",
				0
			));
		}

		KeyboardState previousKS;
		private MyCustomStyle excaliburStyle;

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			camera.HandleInput(Mouse.GetState(), gameTime);
			camera.Update(gameTime);

			// TODO: Add your update logic here
			var ks = Keyboard.GetState();
			if (ks.IsKeyDown(Keys.Space) && !previousKS.IsKeyDown(Keys.Space))
			{
				textHandles.Add(_textRenderer.GenerateGeometry(randomStrings[textHandles.Count], Color.White, Color.Black));
			}
			previousKS = ks;


			/*for (int t = 0; t < textHandles.Count; t++)
			{
				int idx = rng.Next(0, 1024);
				textHandles[t] = _textRenderer.ReplaceGeometry(textHandles[t], randomStrings[idx], new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f), new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f));
			}

			totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
			excaliburStyle.Update();*/
			totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
			excaliburStyle.Update();

			for (int i = 0; i < textHandles.Count; i++)
			{
				_textRenderer.AddTextInstance(new Vector2(8, 32f * i + 8), 32f, textHandles[i].GeometryID);
				//_textRenderer.AddTextInstance(Matrix.CreateScale(32f) * Matrix.CreateTranslation(8, 32f * i + 8, 0), 32f, textHandles[i].GeometryID);
			}

			//Immediate drawing
			_textRenderer.OneShotText("I am looking for the |<legendary> sword that goes by the name of |Excalibur.", new Vector2(8, 8), 16f, Color.White, Color.Transparent);
			_textRenderer.OneShotText("You have found the sword of |Excalibur may you wield it with honour.", new Vector2(8, 32f), 32f, Color.White, Color.Transparent);
			_textRenderer.OneShotText("|EXCALIBUR", new Vector2(8, 48f), 256f, Color.White, Color.Transparent);
			_textRenderer.OneShotText("Wait this Excalibur is |123fake321, |it doesn't quite shine like the others.!", new Vector2(8, 256f + 48f), 24f, Color.White, Color.Transparent);

			//_textRenderer.OneShotText(@"!زﺮﻤﻳﺎﺟ ﻮﻧﻮﻣ ﻼﻫﺃﻎﻈﺿ ﺬﺨﺛ ﺖﺷﺮﻗ ﺺﻔﻌﺳ ﻦﻤﻠﻛ ﻲﻄﺣ زﻮﻫ ﺪﺠﺑﺃ.ﻂﺳﻮﻟﺍ ﻲﻓ Some Text ﻊﻣ ﺺﻧ!ِفْيَّﺺﻟﺍ ِﺔَﻟْﻂُﻋ ﻲﻓ ِﺮَﻤَﻘﻟﺍ ﻰﻟِﺇ َﺮِﻓﺎﺴُﻳ ْنَﺃ ُةﺎﻔُﺤﻟُّﺲﻟﺍ َرَّﺮَﻗ", new Vector2(8, 8), 16f, Color.White, Color.Transparent);


			base.Update(gameTime);
		}

		public static string RandomAsciiString(int length)
		{
			Span<char> buffer = length <= 256 ? stackalloc char[length] : new char[length];
			for (int i = 0; i < buffer.Length; i++)
			{
				// Printable ASCII range: 33 (space) to 126 (~)
				buffer[i] = (char)rng.Next(33, 126);
			}

			return new string(buffer).Replace('|', '/');
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Palette[0]);

			var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float totalTime = (float)gameTime.TotalGameTime.TotalSeconds;
			float sin01 = (MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds) + 1.0f) / 2f;
			float cos01 = (MathF.Cos((float)gameTime.TotalGameTime.TotalSeconds) + 1.0f) / 2f;

			/*_frameCounter.Update(Window, deltaTime);

			//Draw a debug view of our buffers.
			_spriteBatch.Begin();
			DrawVertexBuffer(8, 580 - 8 - 32 - 64, 32);
			DrawIndexBuffer(8, 580 - 8 - 32, 32);
			_spriteBatch.End();

			*/
			_textRenderer.RenderInstances(camera.TransformMatrix, Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f), FontDrawType.StandardTextWithStroke);

			/*_spriteBatch.Begin();
			for (int i = 0; i < textHandles.Count; i++)
			{
				int idx = rng.Next(0, 1024);
				//_spriteBatch.DrawString(_fontSharp.GetFont(32f), randomStrings[i], new Vector2(8 + 1920 / 2, 32f * i + 8), Color.White, effectAmount: 2, effect: FontSystemEffect.Stroked);
				_spriteBatch.DrawString(_spriteFont, randomStrings[i], new Vector2(8 + 1920 / 2, 32f * i + 8), Color.White);
			}
			_spriteBatch.End();*/


			/*_textRenderer.GenerateGeometry("Lorem ipsum dolor sit amet, consectetur adipiscing elit.\nNunc cursus ligula id lacus posuere pretium.\nSed eleifend turpis lorem, nec feugiat tortor ultrices quis.\nClass aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.\nUt feugiat erat sed placerat molestie.\nNam semper ultrices libero, et porttitor ex varius in.\nSed ac imperdiet ligula.\nPraesent odio mi, venenatis sed dapibus eleifend, rhoncus id eros.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.\nPellentesque sed leo non ex volutpat blandit ultricies a purus.\nPellentesque id molestie tortor.\nCurabitur auctor cursus porta.\nQuisque tincidunt eros risus, in efficitur urna molestie nec.\nSed convallis non mauris at blandit.\nDuis vel elit non augue ullamcorper pellentesque.\nInterdum et malesuada fames ac ante ipsum primis in faucibus.\nMauris vitae massa blandit, gravida massa vitae, porta odio.\nNullam accumsan eget nibh nec aliquam.\nInterdum et malesuada fames ac ante ipsum primis in faucibus.\nVestibulum blandit euismod ipsum, nec pretium turpis ultricies sit amet.\nSed lectus sem, volutpat id orci vitae, euismod aliquet purus.\nQuisque consectetur arcu neque, vel venenatis diam varius id.\nVivamus egestas malesuada vulputate.\nNulla eu egestas ante.\nCras a commodo metus.\nVivamus vulputate ante quis nulla pharetra tincidunt.\nCras egestas, ligula ac aliquet aliquet, nibh quam molestie enim, quis convallis lacus orci iaculis arcu.\nClass aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.\nDonec finibus et ex a elementum.\nMauris justo leo, vulputate vel quam a, dapibus egestas arcu.\nIn egestas orci ac tincidunt tincidunt.\nCurabitur eget tellus commodo, rutrum ante vel, tempor arcu.\nPhasellus ac tortor quis nisi pulvinar blandit.\nVestibulum tempor semper pulvinar.\nPellentesque varius dictum enim et pulvinar.\nCurabitur ac tellus et dolor faucibus efficitur quis vel elit.\nIn commodo dolor at urna tristique commodo.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.\nSed euismod sodales condimentum.\nOrci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.\nSed scelerisque commodo odio non ultricies.\nSed id venenatis magna, in tempus metus.\nCras finibus libero vel nisi vehicula cursus.\nNam tristique, arcu eget posuere maximus, ex quam pretium nisi, eu porttitor leo nisl a leo.\nEtiam eget libero sit amet nunc egestas dapibus.Nunc imperdiet eros quam, sed varius mi gravida interdum.\nDonec id odio scelerisque, consequat libero sit amet, pulvinar ex.\nPraesent ut ligula magna.\nInteger mattis fermentum est tincidunt ornare.\nDuis vel lorem risus.\nEtiam varius odio sed felis auctor aliquet.\nFusce aliquet nunc at venenatis iaculis.\nNulla nec dolor sit amet dui ornare elementum.\nIn nulla nisl, tristique non pharetra a, imperdiet ac sem.",
				Palette[8], Palette[3]);

			_textRenderer.RenderText(
				Matrix.Identity,
				Matrix.CreateScale(64f) * Matrix.CreateTranslation(0, 0, 0),
				Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f),
				64f,
				FontDrawType.StandardText
			);*/

			/*_textRenderer.RenderText(
				Matrix.Identity,
				Matrix.CreateScale(64f) * Matrix.CreateTranslation(64, 64, 0),
				Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f),
				64f,
				FontDrawType.StandardText
			);

			for (int i = 0; i < 99999; i++)
			{
				float scale = rng.NextSingle() * 64 * (1.0f - sin01);
				Vector2 pos = new Vector2(rng.NextSingle() * 1920, rng.NextSingle() * 1080);
				int idx = rng.Next(0, 5);
				_textRenderer.AddTextInstance(Matrix.CreateScale(scale) * Matrix.CreateTranslation(pos.X, pos.Y, 0), scale, idx);
			}

			_textRenderer.AddTextInstance(Matrix.CreateScale(64f + sin01 * 64f) * Matrix.CreateTranslation(0, 1080 / 2 - 32, 0), 64f, 3);

			//_textRenderer.AddTextInstance(Matrix.CreateScale(64f) * Matrix.CreateTranslation(32, 0, 0), 64f, 0);
			//_textRenderer.AddTextInstance(Matrix.CreateScale(8f) * Matrix.CreateTranslation(32, 256, 0), 8f, 1);
			//_textRenderer.AddTextInstance(Matrix.CreateScale(1024f) * Matrix.CreateTranslation(0, 128, 0), 1024f, 0);

			//_textRenderer.AddTextInstance(Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(300, 100, 0));

			_textRenderer.RenderInstances(Matrix.Identity, Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f), FontDrawType.StandardTextWithStroke);

			/*float sin01 = (MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds) + 1.0f) / 2f;
			float sin01Fast = (MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 20f) + 1.0f) / 2f;
			// sin01 = .1f;
			for (int i = 0; i < 64; i += 1)
			{
				_textRenderer.GenerateGeometry("The quick brown fox jumped over the lazy dog.", new Vector2(-i * 0.01f, i * 0.01f), 1f, Palette[8], Palette[3] * sin01Fast);


				_textRenderer.RenderText(
					Matrix.Identity,
					Matrix.CreateScale(i * 160 * sin01),
					Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f),
					i * 160 * sin01,
					FontDrawType.StandardTextWithStroke
				);
			}*/

			/*_spriteBatch.Begin();

			_spriteBatch.DrawString(_spriteFont, "The quick brown fox jumped over the lazy dog.", new Vector2(0, 32), Color.Blue, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

			//SpriteFontBase font = _fontSharp.GetFont(341.333344f * sin01);
			//_spriteBatch.DrawString(font, "The quick brown fox jumped over the lazy dog.", new Vector2(0, 64), Color.Red, effect: FontSystemEffect.Stroked, effectAmount: 1);

			_spriteBatch.End();


			*/
			// Render some text
			/*_spriteBatch.Begin();
			for (int i = 0; i < 64; i++)
			{
				SpriteFontBase font = _fontSharp.GetFont(i * 4);
				_spriteBatch.DrawString(font, "Hello World\nWe can increase transparency.", new Vector2(0, i * 4 * 3), Palette[8], effect: FontSystemEffect.Stroked, effectAmount: 1);
			}
			_spriteBatch.End();*/

			base.Draw(gameTime);
		}

		private void DrawVertexBuffer(int posX, int posY, int barHeight)
		{
			int totalSize = _textRenderer.TextBuffer.Vertices.Length;
			int totalUsed = totalSize - _textRenderer.TextBuffer.FreeRanges.Sum(o => o.VertexCount);

			_spriteBatch.DrawString(_fontSharp.GetFont(16), $"Vertex Buffer [{totalUsed}/{totalSize}]", new Vector2(posX, posY - 18), Color.White);
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

			/*for (int i = 0; i < textHandles.Count; i++)
			{
				for (int x = 0; x < textHandles[i].BufferRange.VertexCount; x++)
				{
					float xpos = posX + x + textHandles[i].BufferRange.VertexOffset;
					if (xpos > 1920)
					{
						break;
					}

					Color col = x % 2 == 0 ? new Color(255, 64, 0, 255) : new Color(255 - 64, 64, 64, 255);

					if (x == 0)
					{
						col = Color.Black;
					}
					_spriteBatch.Draw(nixel, new Rectangle((int)xpos, posY, 1, barHeight), col);
				}
			}*/

			/*for (int i = 0; i < _textRenderer.TextBuffer.FreeRanges.Count; i++)
			{
				for (int x = 0; x < _textRenderer.TextBuffer.FreeRanges[i].VertexCount; x++)
				{
					float xpos = posX + x + _textRenderer.TextBuffer.FreeRanges[i].VertexOffset;
					if (xpos > 1920)
					{
						break;
					}
					_spriteBatch.Draw(nixel, new Rectangle((int)xpos, posY, 1, barHeight), Color.Green);
				}
			}*/
		}

		private void DrawIndexBuffer(int posX, int posY, int barHeight)
		{
			int totalUsed = textHandles.Sum(o => o.BufferRange.IndexCount);
			int totalSize = _textRenderer.TextBuffer.Indices.Length;

			_spriteBatch.DrawString(_fontSharp.GetFont(16), $"Index Buffer [{totalUsed}/{totalSize}]", new Vector2(posX, posY - 18), Color.White);
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
		}
	}
}
