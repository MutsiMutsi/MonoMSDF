using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonoMSDF.Pipeline
{
    class MSDFReader : ContentTypeReader<MSDFAtlas>
    {
        protected override MSDFAtlas Read(ContentReader input, MSDFAtlas existingInstance)
        {
            if (existingInstance == null)
                existingInstance = new MSDFAtlas();

            existingInstance._Metadata = JsonSerializer.Deserialize(input.ReadString(), SerializationModeOptionsContext.Default.GlyphAtlas);
            existingInstance._Texture = input.ReadObject<Texture2D>();

            return existingInstance;
        }
    }
}
