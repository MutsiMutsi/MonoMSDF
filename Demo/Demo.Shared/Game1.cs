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
		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private MSDFTextRenderer _textRenderer;
		private MyCustomStyle excaliburStyle;
		private readonly List<TextGeometryHandle> textHandles = [];

		private static float totalTime = 0f;
		public Camera2D camera;

		private GlyphsOnDemand glyphsOnDemand;
		private MSDFTextRenderer _onDemandRenderer;

		private int showChineseChars = 0;
		private static readonly string chinese =
@$"{(char)0xe87d}急課最止再力版著立逆念掲命初平。
後彦都団足椅子買本選|{(char)0xe81a}東著大図際内。
客追案果指蓄単示治針続文送松控長景。
逮念一謙共全画銀中氏聞三 we can do multiple fonts at once, here's another symbol: {(char)0xf135}臨者延奮白婚金散。
拓能言直読始都当経細型向使作禁年善再毎。
新選次百部改図謙無造権作供宙。
当西野問在排広座写研川本何爆州松意。
新罪治止向要民州入意授口立康投。
写視花話藤名溝図推浜約機廃作。

用情県学亜車場択録腎林査進教件写正栄講求。
巡本球義会心担祈波遍見載周査祝金析蘭違。
図禁子学変図無議子内教町覧覧者図。
海本法洗写読世有康開毎申本任想心馬鮮。
栄文近利援需売多載報意平姿。
護長口類画述営総到選勧記禁福場男購。
編繰育難今載出断近千宅歌容治解科。
陸検州南出界転度命図輔掲新軍室。
輪部面同決被推第保触参者由郎辞中。

速意気日下守台返操能家探時告影。
同敷信村総|大文富力保東一備。
支須覇馬名真益力上高記本当円土社核死。
社断料窒強作都女権訴役治拡芸和章。
鈴写予組倍出護赤供製得子徳文意氏。
災公|人集国鮮文民禁掲止行登。
突制連生営目芸円身板具還食畑権済高火庫読。
機芸針捜外域組返浮先教定覧共和鯨判賃謝種。
載覚最記韓育録継主習銀前挙生紙旅福兵邸。

投載避検缶務庭題真回努報謙手街下討伴索連。
開情部康通可容事歯催感方万室目巨|大。
涯険複変正民民係技石王激新被保作半下予芸。
半晴年今高陸本側来独観近応。
信示夏森都勝者動魚断命企設融多。
心出聞暇初広陸半生料江機復督日頭。
民保商美熱一展幡読産動位新演者昇愛公。
報講中村治断月条春災三明歴釈思交激意東全。

祖理行怪交権晃申説聞報山画話。
用元湯況賀座恵食好護生子到濃現共毎徴植覧。
能思裕著義共南安郁新枚谷財検試小論時。
辺会表月振歩産阪出所敬加共索室。
読低真続乏流接次返九納掲内寺伊確。
載水国項思帯写二示直不応。
落力多意今提要平送谷雑髪号予重泉浜。
取衝上断以清瀬方懲測請面法書党。
乗戦事活供怪練忌理受治石提備暮演。

発周結戦憂女時由衝神上米嫌夜倒極。
際込津並尾推禁都違死紀文。
規変集併離策道心影投見東条外今社港。
事古加誘月道省事行並応|人続定田念着。
制能供南相蘊移保帳報豪漂月時小球庭詳。
点理失棚提念昭阪顔面敬手不福経時。
写文英手投真設度同機提身枚列初死近。
際申索者漂習見式量京役玲全週成勝。
直車撃雪玲少海学著定硬更華助際木。";

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
		private class YellowStyle : GlyphStyle
		{
			public override Color[] Fill => [Color.Yellow, Color.Yellow, Color.Orange, Color.Orange];
			public override Color[] Stroke => [Color.Red, Color.Red, Color.Red, Color.Red];

		}

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				GraphicsProfile = GraphicsProfile.HiDef
			};
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.PreferredBackBufferHeight = 1080;

			IsFixedTimeStep = true;
			_graphics.SynchronizeWithVerticalRetrace = true;
			InactiveSleepTime = TimeSpan.Zero;
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			//Initialize the text renderer
			_textRenderer = new MSDFTextRenderer(GraphicsDevice, Content);
			_ = _textRenderer.LoadAtlas("Content/Fonts/Kingthings_Petrock.json", "Content/Fonts/Kingthings_Petrock.png");

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

			_onDemandRenderer = new MSDFTextRenderer(GraphicsDevice, Content);
			glyphsOnDemand = new GlyphsOnDemand(_onDemandRenderer.GetAtlas, GraphicsDevice, "Content/NotoSansSC-Regular.ttf", 32f, 8f);

			glyphsOnDemand.AddGlyphs(ref _onDemandRenderer.GetAtlas, chinese[..showChineseChars].AsSpan(), false);
			_onDemandRenderer.LoadAtlas(_onDemandRenderer.GetAtlas, glyphsOnDemand.AtlasTexture);

			glyphsOnDemand.AddFont("Content/MaterialSymbolsSharp-Regular.ttf");

			_onDemandRenderer.Stylizer.Styles.Add(new YellowStyle());
			_onDemandRenderer.Stylizer.AddRule(new StyleRule(
				$"{(char)0xe81a}",
				0
			));
		}

		public static string GenerateRandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			Random random = new();
			StringBuilder stringBuilder = new(length);

			for (int i = 0; i < length; i++)
			{
				_ = stringBuilder.Append(chars[random.Next(chars.Length)]);
			}

			return stringBuilder.ToString();
		}
		private bool start = false;

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

			if (Keyboard.GetState().IsKeyDown(Keys.F1))
			{
				start = true;
			}

			if (start && showChineseChars < chinese.Length)
			{
				showChineseChars++;
				glyphsOnDemand.AddGlyphs(ref _onDemandRenderer.GetAtlas, chinese[..showChineseChars].AsSpan(), false);
				_onDemandRenderer.LoadAtlas(_onDemandRenderer.GetAtlas, glyphsOnDemand.AtlasTexture);
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

			_spriteBatch.Begin();
			_spriteBatch.Draw(_onDemandRenderer.atlasTexture, new Vector2(1920 - _onDemandRenderer.atlasTexture.Width - 8, 8), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally, 0f);
			_spriteBatch.End();

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


			_onDemandRenderer.OneShotText(chinese[..showChineseChars].AsSpan(), new Vector2(8, 8), 64, Color.Red, Color.Black);
			_onDemandRenderer.RenderInstances(Matrix.Identity, Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f), FontDrawType.StandardTextWithStroke);

			base.Draw(gameTime);
		}
	}
}
