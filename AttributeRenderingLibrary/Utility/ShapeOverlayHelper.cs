using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

/// <summary>
/// Helper methods for working with shape overlays
/// </summary>
public class ShapeOverlayHelper
{
    /// <summary>
    /// Adds overlays from an origin shape, to another shape.<br/>
    /// Uses variants to resolve the overlay paths.
    /// </summary>
    /// <param name="clientApi"></param>
    /// <param name="overlayPrefix">The prefix to use for parenting the overlays</param>
    /// <param name="variants">The variants used to resolve the paths</param>
    /// <param name="textureSource">The texture source that will be used to tesselate</param>
    /// <param name="shape">The shape to add the overlays to</param>
    /// <param name="originShape">The origin shape holding the overlays</param>
    /// <returns>A dictionary containing the original textures and asset locations that have been prefixed</returns>
    public static Dictionary<string, AssetLocation> AddOverlays(ICoreClientAPI clientApi, string overlayPrefix, Variants variants, UniversalShapeTextureSource textureSource, Shape shape, CompositeShape originShape)
    {
        shape.SubclassForStepParenting(overlayPrefix);
        Dictionary<string, AssetLocation> prefixedTextureCodes = shape.Textures;
        shape.Textures = new Dictionary<string, AssetLocation>(prefixedTextureCodes.Count);
        
        foreach (var entry in prefixedTextureCodes)
        {
            shape.Textures[overlayPrefix + entry.Key] = entry.Value;
        }

        Shape resolvedOverlayShape;
        foreach (CompositeShape overlay in originShape.Overlays)
        {
            variants.ReplacePlaceholders(overlay.Base);
            overlay.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            resolvedOverlayShape = clientApi.Assets.TryGet(overlay.Base)?.ToObject<Shape>();

            if (resolvedOverlayShape == null) continue;

            resolvedOverlayShape.WalkElements("*", (e) =>
            {
                if (!string.IsNullOrEmpty(e.StepParentName))
                {
                    e.StepParentName = overlayPrefix + e.StepParentName;
                }
            });

            shape.StepParentShape(resolvedOverlayShape, overlay.Base.ToString(), originShape.Base.ToString(), clientApi.Logger, (textureCode, textureLocation) =>
            {
                textureSource.textures[textureCode] = new CompositeTexture(textureLocation);
            });
        }

        return prefixedTextureCodes;
    }

    /// <summary>
    /// Bakes textures based on variants and optional prefix, and adds them to the texture source
    /// </summary>
    /// <param name="clientApi"></param>
    /// <param name="textureSource">The texture source to use</param>
    /// <param name="variants">The variants used to resolve the textures</param>
    /// <param name="texturesByType">The textures grouped by variant</param>
    /// <param name="prefixedTextureCodes">The texture codes that have been prefixed</param>
    /// <param name="overlayPrefix">The texture prefix to use for prefixed codes</param>
    public static void BakeVariantTextures(ICoreClientAPI clientApi, UniversalShapeTextureSource textureSource, Variants variants, Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType, Dictionary<string, AssetLocation> prefixedTextureCodes = null, string overlayPrefix = "")
    {
        if (!variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> variantTextures)) return;

        foreach((string textureCode, CompositeTexture texture) in variantTextures)
        {
            CompositeTexture ctex = texture.Clone();
            ctex = variants.ReplacePlaceholders(ctex);
            ctex.Bake(clientApi.Assets);
            if (prefixedTextureCodes != null && prefixedTextureCodes.ContainsKey(textureCode))
            {
                textureSource.textures[overlayPrefix + textureCode] = ctex;
            }
            else
            {
                textureSource.textures[textureCode] = ctex;
            }
        }
    }
}
