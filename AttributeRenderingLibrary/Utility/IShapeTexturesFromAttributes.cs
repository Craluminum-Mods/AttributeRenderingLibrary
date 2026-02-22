using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public interface IShapeTexturesFromAttributes
{
    Dictionary<string, CompositeShape> shapeByType { get; }
    Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; }
}

public interface IBlockShapeTexturesFromAttributes : IShapeTexturesFromAttributes
{
    Dictionary<string, BlockDropItemStack[]> DropsByType { get; }
}