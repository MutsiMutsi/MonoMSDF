using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoMSDF.Pipeline
{
    class MSDFReader : ContentTypeReader<Texture2D>
    {
        protected override Texture2D Read(ContentReader input, Texture2D existingInstance)
        {
            return existingInstance;
        }
    }
}
