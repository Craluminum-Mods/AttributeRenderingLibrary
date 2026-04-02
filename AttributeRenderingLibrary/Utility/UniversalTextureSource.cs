//using System.Collections.Generic;
//using System.Linq;
//using Vintagestory.API.Client;
//using Vintagestory.API.Common;
//using Vintagestory.API.MathTools;

//namespace AttributeRenderingLibrary;

//public class UniversalTextureSource : ITexPositionSource
//{   
//    ICoreClientAPI capi;
//    ITextureAtlasAPI targetAtlas;
//    AssetLocation filenameForLogging;
//    public Dictionary<string, CompositeTexture> textures = [];
//    public TextureAtlasPosition firstTexPos;

//    HashSet<AssetLocation> missingTextures = [];

//    public UniversalTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, AssetLocation filenameForLogging)
//    {
//        this.capi = capi;
//        this.targetAtlas = targetAtlas;
//        this.filenameForLogging = filenameForLogging;
//    }

//    public UniversalTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, AssetLocation filenameForLogging, IDictionary<string, CompositeTexture> texturesSource, TexturePathUpdater pathUpdater) : this(capi, targetAtlas, filenameForLogging)
//    {
//        foreach (var val in texturesSource)
//        {
//            var ctex = val.Value.Clone();
//            ctex.Base.Path = pathUpdater(ctex.Base.Path);
//            ctex.Bake(capi.Assets);
//            textures[val.Key] = ctex;
//        }
//    }

//    public TextureAtlasPosition this[string textureCode]
//    {
//        get
//        {
//            TextureAtlasPosition texPos;

//            if (textures.TryGetValue(textureCode, out CompositeTexture? ctex) || textures.TryGetValue("all", out ctex))
//            {
//                targetAtlas.GetOrInsertTexture(ctex, out _, out texPos);
//                firstTexPos ??= texPos;
//            }
//            else
//            {
//                if (!missingTextures.Contains(textureCode))
//                {
//                    LoggerUtil.Warn(capi, this, $"{filenameForLogging} has a shape using texture code '{textureCode}', but no such texture exists");
//                    missingTextures.Add(textureCode);
//                }

//                return targetAtlas.UnknownTexturePosition;
//            }

//            if (texPos == null)
//            {
//                return targetAtlas.UnknownTexturePosition;
//            }

//            firstTexPos ??= texPos;

//            return texPos;
//        }
//    }

//    public TextureAtlasPosition? GetFirstTexPos()
//    {
//        if (firstTexPos != null)
//        {
//            return firstTexPos;
//        }
//        if (textures.FirstOrDefault().Value is { } ctex)
//        {
//            targetAtlas.GetOrInsertTexture(ctex, out _, out var texPos);
//            return texPos;
//        }
//        return null;
//    }

//    public Size2i AtlasSize => targetAtlas.Size;
//}