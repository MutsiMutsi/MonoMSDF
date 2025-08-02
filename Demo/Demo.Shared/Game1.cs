using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMSDF;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Shared
{
	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private MSDFTextRenderer _textRenderer;
		private MyCustomStyle excaliburStyle;
		private List<TextGeometryHandle> textHandles = [];

		private static float totalTime = 0f;
		public Camera2D camera;

		private BufferDebugDraw debugDraw;

		private class MyCustomStyle : GlyphStyle
		{
			private readonly Color[] fillColors = new Color[4];
			private readonly Color[] rotatedFillColors = new Color[4];

			private readonly Color[] strokeColors = new Color[4];
			private readonly Color[] rotatedStrokeColors = new Color[4];

			private readonly float rotationSpeed = 5f;

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
				float rotationPhase = totalTime * rotationSpeed % 4.0f;

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

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			_graphics.GraphicsProfile = GraphicsProfile.HiDef;
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.PreferredBackBufferHeight = 1080;

			IsFixedTimeStep = false;
			_graphics.SynchronizeWithVerticalRetrace = false;
			InactiveSleepTime = TimeSpan.Zero;
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			//Initialize the text renderer
			_textRenderer = new MSDFTextRenderer(GraphicsDevice, Content);
			_ = _textRenderer.LoadAtlas("Content/Fonts/Kingthings_Petrock.json", "Content/Fonts/Kingthings_Petrock.png");

			//bool built = Builder.GenerateFontAtlasTask.GenerateAtlas("C:\\Windows\\Fonts\\consola.ttf", 32.0, 8.0);

			//Create a new custom style and add it to the stylizers.
			excaliburStyle = new MyCustomStyle();
			_textRenderer.Stylizer.Styles.Add(excaliburStyle);
			//Let the custom style calculate its gradients
			excaliburStyle.Update();

			//A simple camera to showcase the matrix transforms (optional).
			camera = new Camera2D(GraphicsDevice.Viewport);

			//Add some word matching style rules
			_textRenderer.Stylizer.AddRule(new StyleRule(
					"Excalibur",
					0
				));
			_textRenderer.Stylizer.AddRule(new StyleRule(
				"EXCALIBUR",
				0
			));

			_textRenderer.Stylizer.AddTag(new TagDefinition("<", ">", 0));
			_textRenderer.Stylizer.AddTag(new TagDefinition("123", "321", 0));

			//Create some static text geometry
			textHandles.Add(_textRenderer.GenerateGeometry("I am looking for the |<legendary> sword that goes by the name of |Excalibur.", Color.White, Color.Transparent));
			textHandles.Add(_textRenderer.GenerateGeometry("You have found the sword of |Excalibur may you wield it with honour.", Color.White, Color.Transparent));
			textHandles.Add(_textRenderer.GenerateGeometry("|EXCALIBUR", Color.White, Color.Transparent));
			textHandles.Add(_textRenderer.GenerateGeometry("Wait this Excalibur is |123fake321, |it doesn't quite shine like the others.!", Color.White, Color.Transparent));

			debugDraw = new BufferDebugDraw(_textRenderer, GraphicsDevice);
		}

		public static string GenerateRandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			Random random = new Random();
			StringBuilder stringBuilder = new StringBuilder(length);

			for (int i = 0; i < length; i++)
			{
				stringBuilder.Append(chars[random.Next(chars.Length)]);
			}

			return stringBuilder.ToString();
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			//To show how well this text scales up.
			if (Keyboard.GetState().IsKeyDown(Keys.Enter))
			{
				camera.Update(gameTime);
			}

			//To showcase how fast geometry can be replaced in realtime on demand.
			if (Keyboard.GetState().IsKeyDown(Keys.Space))
			{
				totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
				excaliburStyle.Update();

				textHandles[0] = _textRenderer.ReplaceGeometry(textHandles[0], GenerateRandomString(128), Color.White, Color.Transparent);
				textHandles[1] = _textRenderer.ReplaceGeometry(textHandles[1], GenerateRandomString(128), Color.White, Color.Transparent);
				textHandles[2] = _textRenderer.ReplaceGeometry(textHandles[2], GenerateRandomString(128), Color.White, Color.Transparent);
				textHandles[3] = _textRenderer.ReplaceGeometry(textHandles[3], GenerateRandomString(128), Color.White, Color.Transparent);
			}



			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(new Color(55, 47, 58));

			//Add the instances so we can actually see our 4 text geometries.
			_textRenderer.AddTextInstance(new Vector2(8, 8), 16f, textHandles[0].BufferRange);
			_textRenderer.AddTextInstance(new Vector2(8, 32f), 32f, textHandles[1].BufferRange);
			_textRenderer.AddTextInstance(new Vector2(8, 48f), 256f, textHandles[2].BufferRange);
			_textRenderer.AddTextInstance(new Vector2(8, 256f + 48f), 24f, textHandles[3].BufferRange);

			//Immediate drawing, this is less efficient, but sure is a lot more convenient.
			_textRenderer.OneShotText("The above text geometry is |<static>, hold space to update the geometry.", new Vector2(8, 512), 32f, Color.MonoGameOrange, Color.Black);
			_textRenderer.OneShotText("Hold enter to zoom in and out", new Vector2(8, 512 + 40f), 32f, Color.CornflowerBlue, Color.Black);

			//Tell the renderer to draw all the instances of text we added above.
			_textRenderer.RenderInstances(camera.TransformMatrix, Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f), FontDrawType.StandardTextWithStroke);

			_spriteBatch.Begin();
			debugDraw.DrawVertexBuffer(_spriteBatch, 16, 16, 16);
			_spriteBatch.End();

			base.Draw(gameTime);
		}

		protected override void EndDraw()
		{
			base.EndDraw();
		}
	}
}
