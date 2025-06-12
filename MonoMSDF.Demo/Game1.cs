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
		private SpriteFont _spriteFont;

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
			_textRenderer.LoadAtlas("arial.json", "arial.png");

			_fontSharp = new FontSystem();
			_fontSharp.AddFont(File.ReadAllBytes(@"C:\Windows\Fonts\consola.ttf"));

			_spriteFont = Content.Load<SpriteFont>("consola");
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


			float sin01 = (MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 0.25f) + 1.0f) / 2f;
			_textRenderer.GenerateGeometry("Lorem ipsum dolor sit amet, consectetur adipiscing elit.\nNunc cursus ligula id lacus posuere pretium.\nSed eleifend turpis lorem, nec feugiat tortor ultrices quis.\nClass aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.\nUt feugiat erat sed placerat molestie.\nNam semper ultrices libero, et porttitor ex varius in.\nSed ac imperdiet ligula.\nPraesent odio mi, venenatis sed dapibus eleifend, rhoncus id eros.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.\nPellentesque sed leo non ex volutpat blandit ultricies a purus.\nPellentesque id molestie tortor.\nCurabitur auctor cursus porta.\nQuisque tincidunt eros risus, in efficitur urna molestie nec.\nSed convallis non mauris at blandit.\nDuis vel elit non augue ullamcorper pellentesque.\nInterdum et malesuada fames ac ante ipsum primis in faucibus.\nMauris vitae massa blandit, gravida massa vitae, porta odio.\nNullam accumsan eget nibh nec aliquam.\nInterdum et malesuada fames ac ante ipsum primis in faucibus.\nVestibulum blandit euismod ipsum, nec pretium turpis ultricies sit amet.\nSed lectus sem, volutpat id orci vitae, euismod aliquet purus.\nQuisque consectetur arcu neque, vel venenatis diam varius id.\nVivamus egestas malesuada vulputate.\nNulla eu egestas ante.\nCras a commodo metus.\nVivamus vulputate ante quis nulla pharetra tincidunt.\nCras egestas, ligula ac aliquet aliquet, nibh quam molestie enim, quis convallis lacus orci iaculis arcu.\nClass aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.\nDonec finibus et ex a elementum.\nMauris justo leo, vulputate vel quam a, dapibus egestas arcu.\nIn egestas orci ac tincidunt tincidunt.\nCurabitur eget tellus commodo, rutrum ante vel, tempor arcu.\nPhasellus ac tortor quis nisi pulvinar blandit.\nVestibulum tempor semper pulvinar.\nPellentesque varius dictum enim et pulvinar.\nCurabitur ac tellus et dolor faucibus efficitur quis vel elit.\nIn commodo dolor at urna tristique commodo.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.\nSed euismod sodales condimentum.\nOrci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.\nSed scelerisque commodo odio non ultricies.\nSed id venenatis magna, in tempus metus.\nCras finibus libero vel nisi vehicula cursus.\nNam tristique, arcu eget posuere maximus, ex quam pretium nisi, eu porttitor leo nisl a leo.\nEtiam eget libero sit amet nunc egestas dapibus.Nunc imperdiet eros quam, sed varius mi gravida interdum.\nDonec id odio scelerisque, consequat libero sit amet, pulvinar ex.\nPraesent ut ligula magna.\nInteger mattis fermentum est tincidunt ornare.\nDuis vel lorem risus.\nEtiam varius odio sed felis auctor aliquet.\nFusce aliquet nunc at venenatis iaculis.\nNulla nec dolor sit amet dui ornare elementum.\nIn nulla nisl, tristique non pharetra a, imperdiet ac sem.",
				Palette[8], Palette[3]);

			_textRenderer.RenderText(
				Matrix.Identity,
				Matrix.CreateScale(64f) * Matrix.CreateTranslation(0, 0, 0),
				Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f),
				64f,
				FontDrawType.StandardText
			);

			_textRenderer.GenerateGeometry("We want more!\nMuch more.", Color.Yellow, Color.Red);

			_textRenderer.RenderText(
				Matrix.Identity,
				Matrix.CreateScale(64f) * Matrix.CreateTranslation(64, 64, 0),
				Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f),
				64f,
				FontDrawType.StandardText
			);

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
	}
}
