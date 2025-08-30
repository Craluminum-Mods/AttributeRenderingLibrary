using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

/// <summary>
/// Helper methods for working with Shape overlays
/// </summary>
public class ShapeOverlayHelper
{
    /// <summary>
    /// Adds overlays from an origin Shape, to another Shape.<br/>
    /// Uses variants to resolve the overlay paths.
    /// </summary>
    /// <returns>A dictionary containing the original textures and asset locations that have been prefixed</returns>
    public static Dictionary<string, AssetLocation> AddOverlays(ICoreClientAPI clientApi, ShapeOverlaysProperties props)
    {
        props.Shape.SubclassForStepParenting(props.OverlayPrefix);
        Dictionary<string, AssetLocation> prefixedTextureCodes = props.Shape.Textures;
        props.Shape.Textures = new Dictionary<string, AssetLocation>(prefixedTextureCodes.Count);
        
        foreach (var entry in prefixedTextureCodes)
        {
            props.Shape.Textures[props.OverlayPrefix + entry.Key] = entry.Value;
        }

        Shape resolvedOverlayShape;
        foreach (CompositeShape overlay in props.OriginShape.Overlays)
        {
            props.Variants.ReplacePlaceholders(overlay.Base);
            overlay.Base = overlay.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
            resolvedOverlayShape = clientApi.Assets.TryGet(overlay.Base)?.ToObject<Shape>();

            if (resolvedOverlayShape == null) continue;

            resolvedOverlayShape.WalkElements("*", (e) =>
            {
                if (!string.IsNullOrEmpty(e.StepParentName))
                {
                    e.StepParentName = props.OverlayPrefix + e.StepParentName;
                }
            });

            props.Shape.StepParentShape(resolvedOverlayShape, overlay.Base.ToString(), props.OriginShape.Base.ToString(), clientApi.Logger, (textureCode, textureLocation) =>
            {
                props.TextureSource.textures[textureCode] = new CompositeTexture(textureLocation);
            });
        }

        return prefixedTextureCodes;
    }

    /// <summary>
    /// Bakes textures based on variants and optional prefix, and adds them to the texture source
    /// </summary>
    public static void BakeVariantTextures(ICoreClientAPI clientApi, BakeTextureProperties props)
    {
        if (!props.Resolve()) return;

        foreach ((string textureCode, CompositeTexture texture) in props.textures)
        {
            CompositeTexture ctex = texture.Clone();

            ctex = props.ApplyOverlaysToTexture(ctex, textureCode);

            ctex = props.Variants.ReplacePlaceholders(ctex);
            if (!clientApi.Assets.Exists(ctex.Base.CopyWithPathPrefixAndAppendixOnce("textures/", ".png")))
            {
                ctex.Base.Path = "unknown";
                ctex.Base.Domain = "game";
            }
            ctex.Bake(clientApi.Assets);
            if (props.PrefixedTextureCodes != null && props.PrefixedTextureCodes.ContainsKey(textureCode))
            {
                props.TextureSource.textures[props.OverlayPrefix + textureCode] = ctex;
            }
            else
            {
                props.TextureSource.textures[textureCode] = ctex;
            }
        }
    }
}