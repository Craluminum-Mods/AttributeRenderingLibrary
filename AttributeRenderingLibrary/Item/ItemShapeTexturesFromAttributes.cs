using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class ItemShapeTexturesFromAttributes : Item, IShapeTexturesFromAttributes, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; } = new();
    public Dictionary<string, int> DurabilityByType { get; protected set; } = new();
    public Dictionary<string, Dictionary<EnumBlockMaterial, float>> MiningSpeedByType { get; protected set; } = new();
    public Dictionary<string, float> AttackPowerByType { get; protected set; } = new();

    public Dictionary<string, CompositeShape> shapeByType { get; protected set; } = new();
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; protected set; } = new();

    #region IAttachableToEntity
    public Dictionary<string, OrderedDictionary<string, CompositeShape>> attachedShapeBySlotCodeByType = new();
    public Dictionary<string, string> categoryCodeByType = new();
    public Dictionary<string, string[]> disableElementsByType = new();
    public Dictionary<string, string[]> keepElementsByType = new();
    private IAttachableToEntity iattr;
    #endregion

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
        iattr = IAttachableToEntity.FromAttributes(this);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs");
    }

    public virtual void LoadTypes()
    {
        if (Attributes != null)
        {
            NameByType = Attributes["name"].AsObject<Dictionary<string, List<object>>>();
            DescriptionByType = Attributes["description"].AsObject<Dictionary<string, List<object>>>();
            ContainedDescriptionByType = Attributes["containedDescription"].AsObject<Dictionary<string, List<object>>>();
            DurabilityByType = Attributes["durability"].AsObject<Dictionary<string, int>>();
            MiningSpeedByType = Attributes["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();
            AttackPowerByType = Attributes["attackPower"].AsObject<Dictionary<string, float>>();

            shapeByType = Attributes["shape"].AsObject<Dictionary<string, CompositeShape>>();
            texturesByType = Attributes["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

            attachedShapeBySlotCodeByType = Attributes["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, OrderedDictionary<string, CompositeShape>>>();
            categoryCodeByType = Attributes["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
            disableElementsByType = Attributes["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
            keepElementsByType = Attributes["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();
        }
    }

    public virtual MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI clientApi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape ucshape);
        ucshape ??= Shape;

        if (ucshape == null) return mesh;

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        Shape shape = clientApi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null) return mesh;

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(clientApi, targetAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation> prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays != null && rcshape.Overlays.Length > 0)
        {
            overlayPrefix = GetMeshCacheKey(itemstack);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in itemstack.Item.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        clientApi.Tesselator.TesselateShape("ShapeTexturesFromAttributes item", shape, out mesh, stexSource, quantityElements: rcshape.QuantityElements, selectiveElements: rcshape.SelectiveElements);
        return mesh;
    }

    public override void OnBeforeRender(ICoreClientAPI clientApi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenMesh(itemstack, clientApi.ItemTextureAtlas, null);
            meshref = clientApi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(clientApi, itemstack, target, ref renderinfo);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        if (NameByType == null || NameByType.Count == 0)
        {
            return base.GetHeldItemName(itemStack);
        }

        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);

        string name = variants.GetName(_langKeys);
        if (string.IsNullOrEmpty(name))
        {
            name = base.GetHeldItemName(itemStack);
        }
        return name;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (DescriptionByType == null || DescriptionByType.Count == 0)
        {
            return;
        }

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
        variants.GetDescription(dsc, _langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public override int GetMaxDurability(ItemStack itemstack)
    {
        if (DurabilityByType == null || DurabilityByType.Count == 0)
        {
            return base.GetMaxDurability(itemstack);
        }

        Variants variants = Variants.FromStack(itemstack);
        if (!variants.FindByVariant(DurabilityByType, out int durability))
        {
            return base.GetMaxDurability(itemstack);
        }
        return durability;
    }

    public override float GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer)
    {
        if (MiningSpeedByType == null || MiningSpeedByType.Count == 0)
        {
            return base.GetMiningSpeed(itemstack, blockSel, block, forPlayer);
        }

        Variants variants = Variants.FromStack(itemstack as ItemStack);
        if (!variants.FindByVariant(MiningSpeedByType, out Dictionary<EnumBlockMaterial, float> miningSpeedByMaterial) || miningSpeedByMaterial == null || miningSpeedByMaterial.Count == 0)
        {
            return base.GetMiningSpeed(itemstack, blockSel, block, forPlayer);
        }

        float traitMultiplier = 1f;
        float finalMiningSpeed = 1f;
        EnumBlockMaterial material = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);
        if (material == EnumBlockMaterial.Ore || material == EnumBlockMaterial.Stone)
        {
            traitMultiplier = forPlayer.Entity.Stats.GetBlended("miningSpeedMul");
        }
        if (!miningSpeedByMaterial.TryGetValue(material, out float miningSpeed))
        {
            finalMiningSpeed *= traitMultiplier;
        }
        else
        {
            finalMiningSpeed *= miningSpeed * traitMultiplier * GlobalConstants.ToolMiningSpeedModifier;
        }
        return finalMiningSpeed;
    }

    public override float GetAttackPower(IItemStack withItemStack)
    {
        if (AttackPowerByType == null || AttackPowerByType.Count == 0)
        {
            return base.GetAttackPower(withItemStack);
        }

        Variants variants = Variants.FromStack(withItemStack as ItemStack);
        if (!variants.FindByVariant(AttackPowerByType, out float attackPower))
        {
            return base.GetAttackPower(withItemStack);
        }
        return attackPower;
    }

    public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(itemstack, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack)
    {
        return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
    }

    public virtual string GetContainedInfo(ItemSlot inSlot)
    {
        if (ContainedDescriptionByType == null || ContainedDescriptionByType.Count == 0)
        {
            return GetHeldItemName(inSlot.Itemstack);
        }

        StringBuilder dsc = new();
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(ContainedDescriptionByType, out List<object> _langKeys);

        if (_langKeys == null || _langKeys.Count == 0)
        {
            return GetHeldItemName(inSlot.Itemstack);
        }

        variants.GetDescription(dsc, _langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        return GetHeldItemName(inSlot.Itemstack);
    }

    void IAttachableToEntity.CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
    {
        foreach ((string textureCode, CompositeTexture texture) in stack.Item.Textures)
        {
            shape.Textures[textureCode] = texture.Baked.BakedName;
        }

        Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();

        if (stack.Collectible.GetCollectibleInterface<IShapeTexturesFromAttributes>() is IShapeTexturesFromAttributes STFA)
        {
            texturesByType = STFA.texturesByType;
        }

        Variants variants = Variants.FromStack(stack);
        if (variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures))
        {
            foreach ((string textureCode, CompositeTexture texture) in _textures)
            {
                CompositeTexture ctex = texture.Clone();
                ctex = variants.ReplacePlaceholders(ctex);
                if (!api.Assets.Exists(ctex.Base.CopyWithPathPrefixAndAppendixOnce("textures/", ".png")))
                {
                    ctex.Base.Path = "unknown";
                    ctex.Base.Domain = "game";
                }
                ctex.Bake(api.Assets);
                intoDict[textureCode] = ctex;
                shape.Textures[textureCode] = ctex.Baked.BakedName;
            }
        }
    }

    CompositeShape IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
    {
        if (attachedShapeBySlotCodeByType == null || attachedShapeBySlotCodeByType.Count == 0)
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

        Variants variants = Variants.FromStack(stack);
        if (!variants.FindByVariant(attachedShapeBySlotCodeByType, out OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode))
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

        if (attachedShapeBySlotCode != null && attachedShapeBySlotCode.Count != 0)
        {
            foreach ((string _slotCode, CompositeShape ucshape) in attachedShapeBySlotCode)
            {
                if (WildcardUtil.Match(_slotCode, slotCode))
                {
                    CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
                    if (rcshape.Overlays == null || rcshape.Overlays.Length == 0)
                    {
                        return rcshape;
                    }

                    List<CompositeShape> overlays = new();
                    foreach (CompositeShape overlay in rcshape.Overlays)
                    {
                        if (api.Assets.Exists(overlay.Base.Clone().CopyWithPathPrefixAndAppendixOnce("shapes/", ".json")))
                        {
                            overlays.Add(overlay);
                        }
                    }
                    rcshape.Overlays = overlays.ToArray();
                    return rcshape;
                }
            }
        }

        return iattr?.GetAttachedShape(stack, slotCode);
    }

    string IAttachableToEntity.GetCategoryCode(ItemStack stack)
    {
        if (categoryCodeByType == null || categoryCodeByType.Count == 0)
        {
            return iattr?.GetCategoryCode(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(categoryCodeByType, out string categoryCode);
        return categoryCode;
    }

    string[] IAttachableToEntity.GetDisableElements(ItemStack stack)
    {
        if (disableElementsByType == null || disableElementsByType.Count == 0)
        {
            return iattr?.GetDisableElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(disableElementsByType, out string[] disableElements);
        return disableElements;
    }

    string[] IAttachableToEntity.GetKeepElements(ItemStack stack)
    {
        if (keepElementsByType == null || keepElementsByType.Count == 0)
        {
            return iattr?.GetKeepElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(keepElementsByType, out string[] keepElements);
        return keepElements;
    }

    string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(stack);

    bool IAttachableToEntity.IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    int IAttachableToEntity.RequiresBehindSlots { get; set; }
}
