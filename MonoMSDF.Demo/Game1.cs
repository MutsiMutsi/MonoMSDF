using FontStashSharp;
using MonoMSDF;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace MGSDF_Demo
{
	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private MSDFTextRenderer _textRenderer;
		private FontSystem _fontSharp;

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

			Effect fx = Content.Load<Effect>("msdf_ps");

			// TODO: use this.Content to load your game content here
			_textRenderer = new MSDFTextRenderer(GraphicsDevice, fx);
			_textRenderer.LoadAtlas("big.json", "big.png");

			_fontSharp = new FontSystem();
			_fontSharp.AddFont(File.ReadAllBytes(@"C:\Windows\Fonts\consola.ttf"));
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			// TODO: Add your update logic here

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Palette[0]);


			float sin01 = (MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds) + 1.0f) / 2f;
			for (int i = 0; i < 1; i++)
			{
				_textRenderer.GenerateGeometry("Hello World\nWe can increase transparency.", new Vector2(0, 0), 64, Palette[8], Palette[3] * sin01);

				_textRenderer.RenderText(
					Matrix.Identity,
					Matrix.Identity,
					Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f),
					FontDrawType.StandardText
				);

				_textRenderer.RenderText(
					Matrix.Identity,
					Matrix.Identity,
					Matrix.CreateOrthographicOffCenter(0, 1920, 1080- 256, -256, -1f, 1f),
					FontDrawType.StandardTextWithStroke
				);
			}

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
	}
}
