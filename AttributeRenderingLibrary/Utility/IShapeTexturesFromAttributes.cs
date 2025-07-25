using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public interface IShapeTexturesFromAttributes
{
    public Dictionary<string, CompositeShape> shapeByType { get; }
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; }
}
