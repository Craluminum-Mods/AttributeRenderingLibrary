using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace AttributeRenderingLibrary;

public static class ShapeExtensions
{
    public static bool CheckIfExists(this CompositeShape compositeShape, ICoreAPI api)
    {
        return api.Assets.Exists(compositeShape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json"));
    }

    public static bool CheckIfExists(this CompositeShape compositeShape, ICoreAPI api, out Shape? shape)
    {
        AssetLocation loc = compositeShape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
        if (api.Assets.Exists(loc))
        {
            shape = Vintagestory.API.Common.Shape.TryGet(api, loc);
            if (shape != null) return true;
        }
        shape = null;
        return false;
    }

    public static CompositeShape? RemoveNonExistingOverlays(this CompositeShape? shape, ICoreAPI api)
    {
        if (shape?.Overlays is { Length: > 0 })
        {
            shape.Overlays = [.. shape.Overlays.Where(overlay => overlay.CheckIfExists(api))];
        }
        return shape;
    }

    extension(ITesselatorAPI tesselator)
    {
        /// <summary>
        /// Same as <see cref="ITesselatorAPI.TesselateShape(string, AssetLocation, CompositeShape, out MeshData, ITexPositionSource, int, byte, byte, int?, string[])"/>.<br/>
        /// Handles offset, rotation, scaling etc.
        /// </summary>
        /// <param name="typeForLogging"></param>
        /// <param name="compositeShape"></param>
        /// <param name="modeldata"></param>
        /// <param name="texSource"></param>
        /// <param name="generalGlowLevel"></param>
        /// <param name="climateColorMapIndex"></param>
        /// <param name="seasonColorMapIndex"></param>
        /// <param name="quantityElements"></param>
        /// <param name="selectiveElements"></param>
        public void TesselateShapeExt(string typeForLogging, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[]? selectiveElements = null)
        {
            tesselator.TesselateShapeExt(
            type: typeForLogging,
            sourceName: compositeShape.Base.ToString(),
            compositeShape: compositeShape,
            modeldata: out modeldata,
            texSource: texSource,
            generalGlowLevel: generalGlowLevel,
            climateColorMapIndex: climateColorMapIndex,
            seasonColorMapIndex: seasonColorMapIndex,
            quantityElements: quantityElements,
            selectiveElements: selectiveElements);
        }

        public void TesselateShapeWithJointIdsExt(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f rotation, int? quantityElements, string[] selectiveElements, string[] ignoreElements)
        {
            TesselationMetaData meta = new TesselationMetaData
            {
                TypeForLogging = typeForLogging,
                TexSource = texSource,
                GeneralGlowLevel = 0,
                GeneralWindMode = 0,
                ClimateColorMapId = 0,
                SeasonColorMapId = 0,
                QuantityElements = quantityElements,
                SelectiveElements = selectiveElements,
                IgnoreElements = ignoreElements,
                WithJointIds = true,
                WithDamageEffect = false
            };

            ShapeTesselator _this = (tesselator as ShapeTesselator)!;
            _this.TesselateShape(shapeBase, out modeldata, rotation, null, 1f, meta);
        }

        /// <summary>
        /// Same as <see cref="ITesselatorAPI.TesselateShape(string, AssetLocation, CompositeShape, out MeshData, ITexPositionSource, int, byte, byte, int?, string[])"/>.<br/>
        /// Handles offset, rotation, scaling and overlays properly
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sourceName"></param>
        /// <param name="compositeShape"></param>
        /// <param name="modeldata"></param>
        /// <param name="texSource"></param>
        /// <param name="generalGlowLevel"></param>
        /// <param name="climateColorMapIndex"></param>
        /// <param name="seasonColorMapIndex"></param>
        /// <param name="quantityElements"></param>
        /// <param name="selectiveElements"></param>
        public void TesselateShapeExt(string type, AssetLocation sourceName, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[]? selectiveElements = null)
        {
            ClientMain game = tesselator.GetField<ClientMain>("game");
            ShapeTesselator _this = (tesselator as ShapeTesselator)!;

            if (!quantityElements.HasValue && compositeShape.QuantityElements > 0)
            {
                quantityElements = compositeShape.QuantityElements;
            }
            if (selectiveElements == null)
            {
                selectiveElements = compositeShape.SelectiveElements;
            }

            TesselationMetaData meta = new TesselationMetaData();
            meta.UsesColorMap = false;
            meta.TypeForLogging = type + " " + sourceName;
            meta.TexSource = texSource;
            meta.GeneralGlowLevel = generalGlowLevel;
            meta.QuantityElements = quantityElements;
            meta.WithJointIds = false;
            meta.SelectiveElements = selectiveElements;
            meta.IgnoreElements = compositeShape.IgnoreElements;
            meta.ClimateColorMapId = climateColorMapIndex;
            meta.SeasonColorMapId = seasonColorMapIndex;

            Shape shape = Shape.TryGet(game.api, compositeShape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json"));
            if (shape == null)
            {
                modeldata = RenderExtensions.GetUnknownItemModelData(game.api);
                LoggerUtil.Error(game.Api, tesselator, $"Could not find shape {compositeShape.Base} for {type} {sourceName}");
                return;
            }

            Vec3f rotationVec = new();
            Vec3f offsetVec = new();
            rotationVec.Set(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ);
            offsetVec.Set(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
            _this.TesselateShape(shape, out modeldata, rotationVec, offsetVec, compositeShape.Scale, meta);
            if (compositeShape.Overlays != null)
            {
                for (int j = 0; j < compositeShape.Overlays.Length; j++)
                {
                    CompositeShape ovCompShape = compositeShape.Overlays[j];

                    meta.QuantityElements = quantityElements;
                    rotationVec.Set(ovCompShape.rotateX, ovCompShape.rotateY, ovCompShape.rotateZ);
                    offsetVec.Set(ovCompShape.offsetX, ovCompShape.offsetY, ovCompShape.offsetZ);

                    Shape overlayShape = Shape.TryGet(game.api, ovCompShape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json"));
                    if (shape == null)
                    {
                        modeldata.AddMeshData(RenderExtensions.GetUnknownItemModelData(game.api));
                        LoggerUtil.Error(game.Api, tesselator, $"Could not find overlay shape {ovCompShape.Base} for {type} {sourceName}");
                        continue;
                    }

                    _this.TesselateShape(overlayShape, out var ovModelData, rotationVec, offsetVec, ovCompShape.Scale, meta);
                    modeldata.AddMeshData(ovModelData);
                }
            }
        }
    }
}