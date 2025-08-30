using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public class ShapeOverlaysProperties
{
    /// <summary>
    /// The prefix to use for parenting the overlays
    /// </summary>
    public UniversalShapeTextureSource TextureSource;

    /// <summary>
    /// The variants used to resolve the paths
    /// </summary>
    public Variants Variants;

    /// <summary>
    /// The shape to add the overlays to
    /// </summary>
    public Shape Shape;

    /// <summary>
    /// The origin shape holding the overlays
    /// </summary>
    public CompositeShape OriginShape;

    /// <summary>
    /// The prefix to use for parenting the overlays
    /// </summary>
    public string OverlayPrefix = "";
}