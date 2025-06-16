# MonoMSDF

A multi signed distance field text renderer for MonoGame.

For those that want sharper text, rich customization features, the best performance, rock solid kerning, zero heap allocations, thousands of dynamic and static lines of text.
All of this typography glory is guaranteed to be drawn in a single draw call.

Benchmarked to outperform all other implementations in static text, and dynamic text.

![MonoMSDF Demo_0w3n5bW14G](https://github.com/user-attachments/assets/8da7924f-26fe-41aa-8ead-e0da752b6768)

# Features

- Render any text at any scale at any screen space position without having to reconstruct geometry.
- If you do want to change text, reuse the buffer space for blazing fast in-place vertex data updates.
- The option the style any range of text up to a single character, defined by dictionary matching, or begin and end tags.
- If you do not care for the optimal performance by using the static geometry and buffer swapping, you have the ability to automatically create and destroy text geometry every frame for simplicity.

# How to use

### The Setup

First create an MSDF atlas using either:
- https://msdf-bmfont.donmccurdy.com/
- https://github.com/Chlumsky/msdf-atlas-gen

This will yield both a png and a json file with the atlas metadata.
I have used msdf-atlas-gen mostly, its an excellent tool here's a basic command example that'll set you up right away with beautiful fonts:

`msdf-atlas-gen -font "path/to/your_font.ttf" -charset ascii_charset.txt -size 32 -pxrange 6 -format png -imageout your_font.png -json your_font.json`

A pxrange of 6 will guarantee crisp and glyph rendering at multiple orders of magnitude of scales, and with a size of 32, all of this fits into a tiny 256x256 atlas.

The charset param points to a text file containing a range of characters, to generate an atlas for just ASCII characters you can create a file `ascii_charset.txt` and fill it with text `[0x20, 0x7E]`.
This will instruct the msdf-atlas-gen with the range of characters to stuff into the atlas. The range can be any UTF16 range, I've confirmed arabic and font awesome unicode emoji and symbols will both work.

### The Runtime

Init and Loading
```c#
//Create a text renderer
private MSDFTextRenderer _textRenderer;

//Initialize/LoadContent:
Effect fx = Content.Load<Effect>("msdf_effect");
_textRenderer = new MSDFTextRenderer(GraphicsDevice, fx);
//Then load your atlas data
_textRenderer.LoadAtlas("your_font_atlas.json", "your_font_atlas.png");
```
The msdf_effect file is included in this repo, you are expected to add this to your monogame project content, in the future I think I will ship releases of this library with compiled fx files so this step is no longer required.
However in this early stage, it is what it is, at least this will make changing and adding features to the shader trivial.

Optionally styles and rules
```c#

//Optionally declare some cool styles which will be automatically applied to your matching texts
//Implement a GlyphStyle class and add it to the styles, as in; public class MyCustomStyle : GlyphStyle is implemented.
var superCoolStyle = new MyCustomStyle();
_textRenderer.Stylizer.Styles.Add(superCoolStyle);

//Once you have a style declared you can define rules on which text this is applied to
//Either by word matching:
_textRenderer.Stylizer.AddRule(new StyleRule("Excalibur", StyleID));

//Or by custom begin/end tag matching
_textRenderer.Stylizer.AddTag(new TagDefinition("<!", "!>", StyleID));
```
Following this example, any text rendered that contains the word "Excalibur" will have its style applied automatically, additionally any text that is surrounded with <! and !> will also have this style applied.
There is one **caveat** to ensure styling is as blazing fast as rendering, prefix any part of text eliglbe for styling with a pipe character '|'.

This ensures that we are not constantly scanning all text for possible word and start tags. So in the case of Excalibur we could have a text input of `"I think |Excalibur is the best sword ever!"`.

For a tag it would look like this: `"The following text |<!has the best styling in the world!>" `

Now finally to render text, first I will show you how to render **static text**.
The biggest gains made here as opposed to many other implementations is that the text geometry is only generated once, which means drawing static text is incredibly fast.
```c#
//First we generate the geometry for the string.
var handle = _textRenderer.GenerateGeometry("Any text you would love to see\nNewlines are also supported!", fillColor, strokeColor);
//This will return a TextGeometryHandle keep this safe somewhere, you will need this to free up the geometry, or replace the text.

//You only have to generate this geometry, and this handle once. Once you have it you can draw this text as many times as you want, anywhere at any scale.
//You can even draw the same text by this handle many times on the same screen practically without any performance hit, this is done through hardware instancing.
_textRenderer.AddTextInstance(position, fontSizeInPixelHeight, handle.GeometryID);
```
__Note: adding instances is super lightweight, and you are expected to do this every frame, for as long as you want to see the text show up. It does not persist.__

The justification to have all this additional complexity (believe me there's a ton going on in the backend to make all this happen) is that in games text rarely changes every single frame.
Tons and tons of computation is wated on reconstructing the text geometry over and over every single frame just to display that yes, you health is still 100/100.
Now with this handle you can replace the geometry from "health 100/100" to "health 99/100" only when its required to do so. 
So here we see how to **replace static text**.
```c#
handle = _textRenderer.ReplaceGeometry(handle, "health 99/100", fillColor, strokeColor);
```
Make sure you do overwrite the handle with the newly returned value, if the new text does not fit within the buffer it will have a new location, and it might also have a different length, its important to keep your handles up to date when changing text.

Now there are cases where you just want to quick and dirty render some text, and you dont care about the performance, for that I provide a OneShotText option.
This will generate geometry, create the instance for it, and will handle freeing up the geometry and buffers automatically behind the scenes. 
```c#
_textRenderer.OneShotText("I am looking for the legendary sword that goes by the name of Excalibur.", new Vector2(8, 8), 16f, fillColor, strokeColor);
```

## Finally show me the text already!

Once you've setup all your static text, instanced text, etc. It is time to finally render everything.

You simply call this in your draw path:
```c#
_textRenderer.RenderInstances(viewMatrix, projectionMatrix, FontDrawType.StandardText);

//Here's an example with a camera that can move, zoom, and rotate, for a 2d projection (1920x1080 viewport)
_textRenderer.RenderInstances(camera.TransformMatrix, Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1f, 1f), FontDrawType.StandardText);

//You are free to use any kind of view and projection matrix setup you want!
//If youre not sure how this works, the simplest case for UI would be something like this:
_textRenderer.RenderInstances(Matrix.Identity, Matrix.CreateOrthographicOffCenter(0, screenwidth, screenheight, 0, -1f, 1f), FontDrawType.StandardText);
```
Now what is FontDrawType? If you want to enable stroke around text, go for FontDrawType.StandardTextWithStroke then for all characters that have a stroke colour with an alpha > 0 will render out strokes.
The performance impact is minor, personally I always leave it on StandardTextWithStroke. Furthermore there are also tiny text options. This should improve text when rendered very small <12px for example.
This is however a cumbersome approach, if tiny text does improve your legibility, do let me know perhaps we can find a way to automate this, so you can render standard text, stroked text, tiny standard text, and tiny stroked text all at once.

# If you have any bugs, feature requests, questions, pull requests you name it.
Do let me know! Open a PR or issue. I would be more than happy to help, or merge your changes!
