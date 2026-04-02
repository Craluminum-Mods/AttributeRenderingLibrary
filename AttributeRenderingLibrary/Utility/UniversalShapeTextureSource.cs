using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary;

/// <summary>
/// Better version of <see cref="ShapeTextureSource"/> that supports both items and blocks
/// </summary>
public class UniversalShapeTextureSource : ITexPositionSource
{   
    ICoreClientAPI capi;
    ITextureAtlasAPI targetAtlas;
    Shape shape;
    string filenameForLogging;
    public Dictionary<string, CompositeTexture> textures = [];
    public TextureAtlasPosition firstTexPos;

    HashSet<AssetLocation> missingTextures = [];

    public UniversalShapeTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Shape shape, string filenameForLogging)
    {
        this.capi = capi;
        this.targetAtlas = targetAtlas;
        this.shape = shape;
        this.filenameForLogging = filenameForLogging;
    }

    public UniversalShapeTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Shape shape, string filenameForLogging, IDictionary<string, CompositeTexture> texturesSource, TexturePathUpdater pathUpdater) : this(capi, targetAtlas, shape, filenameForLogging)
    {
        foreach (var val in texturesSource)
        {
            var ctex = val.Value.Clone();
            ctex.Base.Path = pathUpdater(ctex.Base.Path);
            ctex.Bake(capi.Assets);
            textures[val.Key] = ctex;
        }
    }

    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            TextureAtlasPosition texPos;

            if (textures.TryGetValue(textureCode, out CompositeTexture? ctex) || textures.TryGetValue("all", out ctex))
            {
                targetAtlas.GetOrInsertTexture(ctex, out _, out texPos);
            }
            else
            {
                shape.Textures.TryGetValue(textureCode, out AssetLocation? texturePath);

                if (texturePath == null)
                {
                    if (!missingTextures.Contains(textureCode))
                    {
                        LoggerUtil.Warn(capi, this, $"Shape {filenameForLogging} has an element using texture code {textureCode}, but no such texture exists");
                        missingTextures.Add(textureCode);
                    }

                    return targetAtlas.UnknownTexturePosition;
                }

                targetAtlas.GetOrInsertTexture(texturePath, out _, out texPos);
            }

            if (texPos == null)
            {
                return targetAtlas.UnknownTexturePosition;
            }

            firstTexPos ??= texPos;

            return texPos;
        }
    }

    public Size2i AtlasSize => targetAtlas.Size;
}