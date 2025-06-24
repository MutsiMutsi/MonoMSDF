using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using System;
using System.IO;

namespace MonoMSDF.Content.Pipeline;

[ContentTypeWriter]
class MSDFWriter : ContentTypeWriter<MSDFProcessResult>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform) => "MonoMSDF.Pipeline.MSDFReader, MonoMSDF";

    protected override void Write(ContentWriter output, MSDFProcessResult processed)
    {
        output.Write(processed.MetadataContent);
        output.WriteObject(processed.AtlasContent);
    }
}
