# MonoMSDF


[![Build](https://github.com/MutsiMutsi/MonoMSDF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MutsiMutsi/MonoMSDF/actions/workflows/dotnet.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/MutsiMutsi/MonoMSDF.svg)](https://github.com/MutsiMutsi/MonoMSDF/blob/main/LICENSE)
[![Last Commit](https://img.shields.io/github/last-commit/MutsiMutsi/MonoMSDF.svg)](https://github.com/MutsiMutsi/MonoMSDF/commits/main)
[![Stars](https://img.shields.io/github/stars/MutsiMutsi/MonoMSDF.svg?style=social&label=Star)](https://github.com/MutsiMutsi/MonoMSDF/stargazers)
[![Forks](https://img.shields.io/github/forks/MutsiMutsi/MonoMSDF.svg?style=social&label=Fork)](https://github.com/MutsiMutsi/MonoMSDF/network/members)

<!-- If you're publishing to NuGet, uncomment below and set correct package name -->
<!--
[![NuGet](https://img.shields.io/nuget/v/MonoMSDF.svg)](https://www.nuget.org/packages/MonoMSDF/)
-->

A multi signed distance field text renderer for MonoGame.

For those that want sharper text, rich customization features, the best performance, rock solid kerning, zero heap allocations, thousands of dynamic and static lines of text.
All of this typography glory is guaranteed to be drawn in a single draw call.

Benchmarked to outperform all other implementations in static text, and dynamic text.

![MonoMSDF Demo_0w3n5bW14G](https://github.com/user-attachments/assets/8da7924f-26fe-41aa-8ead-e0da752b6768)

# Features

- [x] Render any text at any scale at any screen space position without having to reconstruct geometry.
- [x] If you do want to change text, reuse the buffer space for blazing fast in-place vertex data updates.
- [x] The option to style any range of text up to a single character, defined by dictionary matching, or begin and end tags.
- [x] If you do not care for the optimal performance through using the static geometry and buffer swapping, you have the ability to automatically create and destroy text geometry every frame for simplicity.
- [ ] Restyle geometry without having to reconstruct quad pos/texcoords (optimization)
- [ ] Compound geometry allows for instance to reuse "Health" text geometry with for example "100/100" and "50/"100" into "Health 100/100" and "Health 50/50".
- [x] Integrate msdf-atlas-gen tool (initially windows only) to allow for zero setup, just supply a ttf/otf.
- [ ] Cross platform the msdf-atlas-gen implementation somehow to allow Linux and MacOS to also have zero setup
- [ ] Support dynamic atlas building at runtime, add glyphs on demand.
- [ ] Implementation to support multiple fonts in a single atlas so that no texture swaps are neccesary
- [ ] Many more style and customization options, there are never enough. But never compromise on performance.

# How to use

### Automatic Atlas Generation (currently WINDOWS ONLY)

It is now possible to automatically build all fonts found within your content path to be built into atlasses.

This is for convenience and ease of onboarding, for the most control and settings refer to manual atlas generation.

To enable the automated process simply add this at the end of your game's csproj file:
```xml
<!-- Atlas Builder -->
<Import Project="..\AtlasBuilder\AtlasBuilder.targets" />
```
If you don't know the path to AtlasBuilder or how to find it, simply refer to the line in your csproj that includes the MonoMSDF library:
```xml
<ProjectReference Include="..\MonoMSDF\MonoMSDF.csproj" />
```
The path is the exact same, except you can replace MonoMSDF with AtlasBuilder, since they exist right next to eachother.

This will automatically pickup all ttf and otf fonts within your content folder, including subfolders, you dont have to add anything to the MonoGame pipeline editor.

If for some reason you dont want all fonts within your content folder turned into atlasses, you can configure which folder to search.
Additionally you can also configure the output folder, and the size and distance settings for the MSDF atlas generation, like so:

```xml
  <!-- Atlas Builder -->
  <Import Project="$(MSBuildThisFileDirectory)..\AtlasBuilder\AtlasBuilder.targets" />
  <PropertyGroup>
    <ContentFolder>YouChoose/APath/ToSearchFor/FontInputFiles</ContentFolder>
    <OutputFolder>YouChoose/APath/ToSaveThe/AtlasOutputFiles</OutputFolder>
    <FontSize>48.0</FontSize>
    <DistanceRange>6.0</DistanceRange>
  </PropertyGroup>
```

Once configured every time you add a font file or build your project, all fonts that are not yet built, or were modified, are built into font atlasses automatically.

### Manual Atlas Generation

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
_textRenderer = new MSDFTextRenderer(GraphicsDevice, Content);
//Then load your atlas data
_textRenderer.LoadAtlas("your_font_atlas.json", "your_font_atlas.png");
```

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
There is one **caveat** to ensure styling is as blazing fast as rendering, prefix any part of text eligible for styling with a pipe character '|'.

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
Tons and tons of computation is wasted on reconstructing the text geometry over and over every single frame just to display that yes, you health is still 100/100.
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
