using Microsoft.Xna.Framework.Content.Pipeline;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace MonoMSDF.Content.Pipeline;


// If the library supports multiple fonts at a time
// might make it optional to read a json or a text file having all fonts needed
// and that file would have extension .msdf
[ContentImporter([".ttf", ".otf"], DisplayName = "MSDF Importer", DefaultProcessor = "MSDFProcessor")]
public class MSDFImporter : ContentImporter<MSDFImportResult>
{
    public override MSDFImportResult Import(string filename, ContentImporterContext context)
    {
        var tempPrefixPath = Path.Combine(context.IntermediateDirectory, Path.GetFileNameWithoutExtension(filename));

        return new MSDFImportResult() 
        {
            FontInput = filename,
            AtlasOutput = $"{tempPrefixPath}.{Path.GetRandomFileName()}.png",
            MetadataOutput = $"{tempPrefixPath}.{Path.GetRandomFileName()}.json",
            ImporterContext = context,
        };
    }
}
