using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public interface IShapeTexturesFromAttributes
{
    public Dictionary<string, CompositeShape> shapeByType { get; }
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; }
}
