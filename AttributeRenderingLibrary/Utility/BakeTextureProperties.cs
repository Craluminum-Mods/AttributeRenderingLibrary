using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary;

public class BakeTextureProperties
{
    /// <summary>
    /// The texture source to use
    /// </summary>
    public UniversalShapeTextureSource TextureSource;
    
    /// <summary>
    /// The variants used to resolve the textures
    /// </summary>
    public Variants Variants;

    /// <summary>
    /// The textures grouped by variant
    /// </summary>
    public Dictionary<string, Dictionary<string, CompositeTexture>> TexturesByType;

    /// <summary>
    /// The texture overlays grouped by variant and texture code
    /// </summary>
    public Dictionary<string, Dictionary<string, BlendedOverlayTexture[]>> TextureOverlaysByType;

    /// <summary>
    /// The texture codes that have been prefixed
    /// </summary>
    public Dictionary<string, AssetLocation> PrefixedTextureCodes = [];

    /// <summary>
    /// The texture prefix to use for prefixed codes
    /// </summary>
    public string OverlayPrefix = "";

    /// <summary>
    /// Unresolved textures after calling Resolve()
    /// </summary>
    public Dictionary<string, CompositeTexture> textures = new();

    /// <summary>
    /// Unresolved texture overlays after calling Resolve()
    /// </summary>
    public Dictionary<string, BlendedOverlayTexture[]> textureOverlaysByTextureCode = new();

    public bool Resolve()
    {
        if (!Variants.FindByVariant(TexturesByType, out textures))
        {
            return false;
        }

        Variants.FindByVariant(TextureOverlaysByType, out textureOverlaysByTextureCode);
        return true;
    }

    public CompositeTexture ApplyOverlaysToTexture(CompositeTexture ctex, string textureCode)
    {
        if (textureOverlaysByTextureCode.Count != 0
            && textureOverlaysByTextureCode.TryGetValue(textureCode, out BlendedOverlayTexture[] curTextureOverlays)
            && curTextureOverlays != null
            && curTextureOverlays.Length != 0)
        {
            foreach (BlendedOverlayTexture texOverlay in curTextureOverlays)
            {
                BlendedOverlayTexture ctexOverlay = texOverlay.Clone();
                ctexOverlay = Variants.ReplacePlaceholders(ctexOverlay);
                ctex.BlendedOverlays ??= [];
                ctex.BlendedOverlays = ctex.BlendedOverlays.Append(ctexOverlay);
            }
            return ctex;
        }

        return ctex;
    }
}