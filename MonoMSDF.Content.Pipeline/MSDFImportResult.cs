using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoMSDF.Content.Pipeline
{
    public struct MSDFImportResult 
    {
        /// <summary>
        /// Input .ttf/.otf file name.
        /// </summary
        public string FontInput;
        /// <summary>
        /// Output .json file name.
        /// </summary>
        public string MetadataOutput;
        /// <summary>
        /// Output .png
        /// </summary>
        public string AtlasOutput;
        /// <summary>
        /// Output .png
        /// </summary>
        public ContentImporterContext ImporterContext;
    }
}
