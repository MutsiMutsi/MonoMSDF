using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;


namespace MonoMSDF.Content.Pipeline;

[ContentProcessor(DisplayName = "MSDF Atlas Processor - MonoMSDF")]
class MSDFProcessor : ContentProcessor<MSDFImportResult, MSDFProcessResult>
{

    [DllImport("..\\..\\..\\..\\native\\win-x64\\CutlassWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GenerateAtlas(string fontFileName, double fontSize, double distanceRange, string outputPng, string outputJson);

    [DisplayName("Font Size (In Atlas)")]
    public double FontSize { get; set; } = 32.0;

    [DisplayName("Distance Range")]
    public double DistanceRange { get; set; } = 6.0;

    [DisplayName("Premultiply Alpha")]
    public bool PremultiplyAlpha { get; set; } = true;

    [DisplayName("Texture Format")]
    public TextureProcessorOutputFormat TextureFormat { get; set; }

    [DisplayName("Generate Mipmaps")]
    public bool GenerateMipmaps { get; set; }


    public override MSDFProcessResult Process(MSDFImportResult input, ContentProcessorContext context)
    {
        // GenerateAtlas
        GenerateAtlas(
            fontFileName: input.FontInput,
            fontSize: FontSize,
            distanceRange: DistanceRange,
            outputPng: input.AtlasOutput,
            outputJson: input.MetadataOutput
            );

        TextureContent atlasTextureContent;
        // We import the texture
        atlasTextureContent = new TextureImporter()
            .Import(input.AtlasOutput, input.ImporterContext);
        // We process the texture
        atlasTextureContent = new TextureProcessor()
        {
            PremultiplyAlpha = PremultiplyAlpha,
            TextureFormat = TextureFormat,
            GenerateMipmaps = GenerateMipmaps,
        }   .Process(atlasTextureContent, context);

        string metadataContent;
        using (var jsonFile = new StreamReader(File.OpenRead(input.MetadataOutput)))
            metadataContent = jsonFile.ReadToEnd();

        // We return as result of msdf process Texture2DContent along with .json content
        return new MSDFProcessResult()
        {
            AtlasContent = (Texture2DContent)atlasTextureContent,
            MetadataContent = metadataContent,

        };
    }
}
