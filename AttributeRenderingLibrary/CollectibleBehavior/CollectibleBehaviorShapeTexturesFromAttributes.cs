using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorShapeTexturesFromAttributes(CollectibleObject collObj) : CollectibleBehavior(collObj), IShapeTexturesFromAttributes, IPropertiesSupplier, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{
    #region Collectible properties
    public Dictionary<string, TagSet>? TagsByType { get; protected set; }
    public Dictionary<string, CompositeShape>? shapeByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>>? texturesByType { get; protected set; }
    public Dictionary<string, List<object>>? NameByType { get; protected set; }
    public Dictionary<string, List<object>>? DescriptionByType { get; protected set; }
    public Dictionary<string, List<object>>? ContainedNameByType { get; protected set; }
    public Dictionary<string, List<object>>? ContainedDescriptionByType { get; protected set; }
    public Dictionary<string, byte[]>? LightHsvByType { get; protected set; }
    public Dictionary<string, EnumItemStorageFlags>? StorageFlagsByType { get; protected set; }
    public Dictionary<string, int>? DurabilityByType { get; protected set; }
    public Dictionary<string, float>? AttackPowerByType { get; protected set; }
    public Dictionary<string, float>? AttackRangeByType { get; protected set; }
    public Dictionary<string, Dictionary<EnumBlockMaterial, float>>? MiningSpeedByType { get; protected set; }
    public Dictionary<string, EnumItemDamageSource[]>? DamagedByByType { get; protected set; }
    public Dictionary<string, EnumTool?>? ToolByType { get; protected set; }
    public Dictionary<string, int>? ToolTierByType { get; protected set; }
    #endregion
    #region Collectible properties (resolvable)
    public Dictionary<string, CombustibleProperties>? CombustiblePropsByType { get; protected set; }
    public Dictionary<string, FoodNutritionProperties>? NutritionPropsByType { get; protected set; }
    public Dictionary<string, GrindingProperties>? GrindingPropsByType { get; protected set; }
    public Dictionary<string, CrushingProperties>? CrushingPropsByType { get; protected set; }
    public Dictionary<string, TransitionableProperties[]>? TransitionablePropsByType { get; protected set; }
    public Dictionary<string, JuiceableProperties>? JuiceablePropsByType { get; protected set; }
    public Dictionary<string, DistillationProps>? DistillationPropsByType { get; protected set; }
    #endregion
    #region Animations
    public Dictionary<string, string>? HeldLeftReadyAnimationByType { get; protected set; }
    public Dictionary<string, string>? HeldRightReadyAnimationByType { get; protected set; }

    public Dictionary<string, string>? HeldLeftTpIdleAnimationByType { get; protected set; }
    public Dictionary<string, string>? HeldRightTpIdleAnimationByType { get; protected set; }

    public Dictionary<string, string>? HeldTpUseAnimationByType { get; protected set; }
    public Dictionary<string, string>? HeldTpHitAnimationByType { get; protected set; }
    #endregion
    #region Extra shape overrides
    public Dictionary<string, string[]>? ShapeIgnoreElementsByType { get; protected set; }
    public Dictionary<string, string[]>? ShapeIgnoreElementsCombineByType { get; protected set; }
    public Dictionary<string, string[]>? ShapeSelectiveElementsByType { get; protected set; }
    public Dictionary<string, string[]>? ShapeSelectiveElementsCombineByType { get; protected set; }
    #endregion
    #region IAttachableToEntity
    public Dictionary<string, CompositeShape>? AttachedShapeByType { get; protected set; }
    public Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>? AttachedShapeBySlotCodeByType { get; protected set; }
    public Dictionary<string, string>? CategoryCodeByType { get; protected set; }
    public Dictionary<string, string[]>? DisableElementsByType { get; protected set; }
    public Dictionary<string, string[]>? KeepElementsByType { get; protected set; }
    public IAttachableToEntity? iattr;
    #endregion

#nullable disable
    public ICoreClientAPI clientApi;
    public ICoreAPI coreApi;
#nullable enable
    
    public virtual string MeshRefCacheKey => $"ARL_{this}_MeshRefs";

    public override void OnLoaded(ICoreAPI api)
    {
        if (collObj.Attributes != null && collObj.Attributes.IsTrue("wearableAttachment") && this is not AttributeRenderingLibrary.CollectibleBehaviorWearableAttachment)
        {
            LoggerUtil.Warn(api, this, $"'AttributeRenderingLibrary.ShapeTexturesFromAttributes' behavior no longer supports wearables properly. Please, replace it with 'AttributeRenderingLibrary.Wearable' behavior for {collObj.Code} instead");
        }

        clientApi = api as ICoreClientAPI;
        coreApi = api;
        iattr = IAttachableToEntity.FromAttributes(collObj);

        if (clientApi != null)
        {
            clientApi.Event.ReloadShapes += Event_ReloadShapes;
            clientApi.Event.ReloadTextures += Event_ReloadTextures;
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        DisposeOfMeshes(api);
    }

    public virtual void Event_ReloadShapes() => DisposeOfMeshes(coreApi);
    public virtual void Event_ReloadTextures() => DisposeOfMeshes(coreApi);

    public virtual void DisposeOfMeshes(ICoreAPI api)
    {
        Dictionary<string, MultiTextureMeshRef>? meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, MeshRefCacheKey);
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, MeshRefCacheKey);
    }


    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        LoadTypes(properties);
    }

    public virtual void LoadTypes(JsonObject properties)
    {
        if (properties is not { Count: > 0 }) return;

        LoadTags(properties);

        shapeByType = properties["shape"].AsObject<Dictionary<string, CompositeShape>>(null, collObj.Code.Domain);
        texturesByType = properties["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>(null, collObj.Code.Domain);

        ShapeIgnoreElementsByType = properties["shapeIgnoreElements"].AsObject<Dictionary<string, string[]>>();
        ShapeIgnoreElementsCombineByType = properties["shapeIgnoreElementsCombine"].AsObject<Dictionary<string, string[]>>();
        ShapeSelectiveElementsByType = properties["shapeSelectiveElements"].AsObject<Dictionary<string, string[]>>();
        ShapeSelectiveElementsCombineByType = properties["shapeSelectiveElementsCombine"].AsObject<Dictionary<string, string[]>>();

        NameByType = properties["name"].AsObject<Dictionary<string, List<object>>>();
        DescriptionByType = properties["description"].AsObject<Dictionary<string, List<object>>>();
        ContainedNameByType = properties["containedName"].AsObject<Dictionary<string, List<object>>>();
        ContainedDescriptionByType = properties["containedDescription"].AsObject<Dictionary<string, List<object>>>();
        LightHsvByType = properties["lightHsv"].AsObject<Dictionary<string, byte[]>>();
        StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();
        DurabilityByType = properties["durability"].AsObject<Dictionary<string, int>>();
        AttackPowerByType = properties["attackPower"].AsObject<Dictionary<string, float>>();
        AttackRangeByType = properties["attackRange"].AsObject<Dictionary<string, float>>();
        MiningSpeedByType = properties["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();
        DamagedByByType = properties["damagedBy"].AsObject<Dictionary<string, EnumItemDamageSource[]>>();
        ToolByType = properties["tool"].AsObject<Dictionary<string, EnumTool?>>();
        ToolTierByType = properties["toolTier"].AsObject<Dictionary<string, int>>();
        CombustiblePropsByType = properties["combustibleProps"].AsObject<Dictionary<string, CombustibleProperties>>(null, collObj.Code.Domain);
        NutritionPropsByType = properties["nutritionProps"].AsObject<Dictionary<string, FoodNutritionProperties>>(null, collObj.Code.Domain);
        GrindingPropsByType = properties["grindingProps"].AsObject<Dictionary<string, GrindingProperties>>(null, collObj.Code.Domain);
        CrushingPropsByType = properties["crushingProps"].AsObject<Dictionary<string, CrushingProperties>>(null, collObj.Code.Domain);
        TransitionablePropsByType = properties["transitionableProps"].AsObject<Dictionary<string, TransitionableProperties[]>>(null, collObj.Code.Domain);
        JuiceablePropsByType = properties["juiceableProperties"].AsObject<Dictionary<string, JuiceableProperties>>(null, collObj.Code.Domain);
        DistillationPropsByType = properties["distillationProps"].AsObject<Dictionary<string, DistillationProps>>(null, collObj.Code.Domain);

        HeldLeftReadyAnimationByType = properties["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightReadyAnimationByType = properties["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

        HeldLeftTpIdleAnimationByType = properties["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightTpIdleAnimationByType = properties["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

        HeldTpUseAnimationByType = properties["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
        HeldTpHitAnimationByType = properties["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

        AttachedShapeByType = properties["STFA_attachableToEntity"]?["attachedShape"].AsObject<Dictionary<string, CompositeShape>>(null, collObj.Code.Domain);
        AttachedShapeBySlotCodeByType = properties["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>>(null, collObj.Code.Domain);
        CategoryCodeByType = properties["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
        DisableElementsByType = properties["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
        KeepElementsByType = properties["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();

        // it is necessary to add empty attributes for juiceableProperties and distillationProps, otherwise they may not properly show up in handbook
        if (JuiceablePropsByType is { Count: > 0 } && (collObj.Attributes == null || !collObj.Attributes.KeyExists("juiceableProperties")))
        {
            collObj.EnsureAttributesNotNull();
            collObj.Attributes!.Token!["juiceableProperties"] = JToken.FromObject(new JuiceableProperties());
        }
        if (DistillationPropsByType is { Count: > 0 } && (collObj.Attributes == null || !collObj.Attributes.KeyExists("distillationProps")))
        {
            collObj.EnsureAttributesNotNull();
            collObj.Attributes!.Token!["distillationProps"] = JToken.FromObject(new DistillationProps());
        }
    }

    public virtual void LoadTags(JsonObject properties)
    {
        Dictionary<string, List<string>>? unresolvedTags = properties["tags"].AsObject<Dictionary<string, List<string>>>();
        if (unresolvedTags is not { Count: > 0 }) return;

        TagsByType = [];

        foreach ((string type, List<string> tags) in unresolvedTags)
        {
            Core.Api.CollectibleTagRegistry.TryCreateTagSetAndLogIssues(out TagSet resolvedTags, tags);
            TagsByType.Add(type, resolvedTags);
        }
    }

    public virtual MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas) => GetOrCreateMesh(slot, targetAtlas, overrideShape: null);

    public virtual MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, CompositeShape overrideShape)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack!);

        Shape? shape = GetShape(slot, variants, overrideShape, out CompositeShape? rcshape);

        if (shape == null || rcshape == null)
        {
            return RenderExtensions.GetUnknownItemModelData(clientApi);
        }

        UniversalShapeTextureSource stexSource = new(clientApi, targetAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation>? prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays is { Length: > 0 })
        {
            overlayPrefix = GetMeshCacheKey(slot);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in slot.Itemstack!.Item.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        rcshape.IgnoreElements = GetShapeIgnoreElements(variants, rcshape);

        clientApi.Tesselator.TesselateShapeExt(
            typeForLogging: $"{this} item behavior",
            compositeShape: rcshape,
            modeldata: out mesh,
            texSource: stexSource,
            quantityElements: rcshape.QuantityElements,
            selectiveElements: GetShapeSelectiveElements(variants, rcshape));

        return mesh;
    }

    public virtual Shape? GetShape(ItemSlot? slot, Variants variants, CompositeShape? overrideShape, out CompositeShape? originalShape)
    {
        CompositeShape? ucshape = overrideShape;
        if (ucshape == null)
        {
            variants.FindByVariant(shapeByType!, out ucshape);
            ucshape ??= slot!.Itemstack?.Item.Shape;
        }
        if (ucshape == null)
        {
            originalShape = ucshape;
            return null;
        }

        originalShape = variants.ReplacePlaceholders(ucshape.Clone());
        if (originalShape.CheckIfExists(coreApi, out Shape? shape))
        {
            return shape;
        }
        return null;
    }

    public virtual string[] GetShapeIgnoreElements(Variants variants, CompositeShape cshape)
    {
        if (ShapeIgnoreElementsByType is { Count: > 0 })
        {
            if (variants.FindByVariant(ShapeIgnoreElementsByType, out string[] elements) && elements != null)
            {
                return variants.ReplacePlaceholders(elements).Append(cshape.IgnoreElements);
            }
        }

        if (ShapeIgnoreElementsCombineByType is { Count: > 0 })
        {
            List<string> result = cshape.IgnoreElements is null ? new() : new(cshape.IgnoreElements);
            foreach (string[] elements in variants.FindAllByVariant(ShapeIgnoreElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(elements));
            }
            return [.. result];
        }

        return cshape.IgnoreElements;
    }

    public virtual string[] GetShapeSelectiveElements(Variants variants, CompositeShape cshape)
    {
        if (ShapeSelectiveElementsByType is { Count: > 0 })
        {
            if (variants.FindByVariant(ShapeSelectiveElementsByType, out string[] elements) && elements != null)
            {
                return variants.ReplacePlaceholders(elements).Append(cshape.SelectiveElements);
            }
        }

        if (ShapeSelectiveElementsCombineByType is { Count: > 0 })
        {
            List<string> result = cshape.SelectiveElements is null ? new() : new(cshape.SelectiveElements);
            foreach (string[] elements in variants.FindAllByVariant(ShapeSelectiveElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(elements));
            }
            return [.. result];
        }

        return cshape.SelectiveElements;
    }

    public override void OnBeforeRender(ICoreClientAPI clientApi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, MeshRefCacheKey, () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(renderinfo.InSlot);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenMesh(renderinfo.InSlot, clientApi.ItemTextureAtlas, null);
            meshref = clientApi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(clientApi, itemstack, target, ref renderinfo);
    }

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        if (!itemStack.FindByVariant(NameByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return;
        }

        sb.Clear().Append(variants.GetName(langKeys));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.GetDebugDescription(dsc, withDebugInfo);

        if (!inSlot.Itemstack.FindByVariant(DescriptionByType, out List<object> langKeys) || langKeys is not { Count: > 0 })
        {
            return;
        }
        variants.GetDescription(dsc, langKeys);
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags))
        {
            return base.GetStorageFlags(itemstack, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return storageFlags;
    }

    public override int GetMaxDurability(ItemStack itemstack, int durability, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(DurabilityByType, out int maxDurability))
        {
            return base.GetMaxDurability(itemstack, durability, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return maxDurability;
    }

    public override float GetAttackPower(ItemStack itemstack, float attackPower, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(AttackPowerByType, out float newAttackPower))
        {
            return base.GetAttackPower(itemstack, attackPower, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return newAttackPower;
    }

    public override float GetAttackRange(ItemStack itemstack, float attackRange, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(AttackRangeByType, out float newAttackRange))
        {
            return base.GetAttackRange(itemstack, attackRange, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return newAttackRange;
    }

    public override Dictionary<EnumBlockMaterial, float> GetMiningSpeeds(ItemSlot slot, ref EnumHandling handling)
    {
        if (!slot.Itemstack.FindByVariant(MiningSpeedByType, out Dictionary<EnumBlockMaterial, float> miningSpeed))
        {
            return base.GetMiningSpeeds(slot, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return miningSpeed;
    }

    public override string GetHeldReadyAnimation(ItemSlot slot, Entity forEntity, EnumHand hand, ref EnumHandling handling)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;
        if (!slot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldReadyAnimation(slot, forEntity, hand, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot slot, Entity forEntity, EnumHand hand, ref EnumHandling handling)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;
        if (!slot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldTpIdleAnimation(slot, forEntity, hand, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot slot, Entity forEntity, ref EnumHandling handling)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpUseAnimationByType, out string animCode))
        {
            return base.GetHeldTpUseAnimation(slot, forEntity, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity, ref EnumHandling handling)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpHitAnimationByType, out string animCode))
        {
            return base.GetHeldTpHitAnimation(slot, byEntity, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }
    #region IContainedMeshSource
    public virtual MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(slot, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot)
    {
        return $"{slot.Itemstack.Collectible.Code}-{Variants.FromStack(slot.Itemstack)}";
    }
    #endregion
    #region IContainedCustomName
    public virtual string GetContainedInfo(ItemSlot inSlot)
    {
        if (!inSlot.Itemstack.FindByVariant(ContainedDescriptionByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return inSlot.Itemstack.GetName();
        }

        StringBuilder dsc = new();
        variants.GetDescription(dsc, langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        if (!inSlot.Itemstack.FindByVariant(ContainedNameByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return inSlot.Itemstack.GetName();
        }

        StringBuilder dsc = new();
        variants.GetDescription(dsc, langKeys);
        return dsc.ToString();
    }
    #endregion
    #region IAttachableToEntity
    public virtual void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
    {
        foreach ((string textureCode, CompositeTexture texture) in stack.Item.Textures)
        {
            shape.Textures[textureCode] = texture.Baked.BakedName;
        }

        Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = [];

        if (stack.Collectible.GetCollectibleInterface<IShapeTexturesFromAttributes>() is { } STFA)
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
                if (!clientApi.Assets.Exists(ctex.Base.CopyWithPathPrefixAndAppendixOnce("textures/", ".png")))
                {
                    ctex.Base.Path = "unknown";
                    ctex.Base.Domain = "game";
                }
                ctex.Bake(clientApi.Assets);
                intoDict[textureCode] = ctex;
                shape.Textures[textureCode] = ctex.Baked.BakedName;
            }
        }
    }

    public virtual CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
    {
        Variants variants = Variants.FromStack(stack);

        if (stack.FindByVariant(AttachedShapeByType, out CompositeShape attachedShape) && attachedShape != null)
        {
            CompositeShape rcshape = variants.ReplacePlaceholders(attachedShape.Clone());
            return rcshape.RemoveNonExistingOverlays(coreApi)!;
        }

        if (stack.FindByVariant(AttachedShapeBySlotCodeByType, out var attachedShapeBySlotCode) && attachedShapeBySlotCode is { Count: > 0 })
        {
            foreach ((string _slotCode, CompositeShape ucshape) in attachedShapeBySlotCode)
            {
                if (WildcardUtil.Match(_slotCode, slotCode))
                {
                    CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
                    return rcshape.RemoveNonExistingOverlays(coreApi)!;
                }
            }
        }

        if (iattr is AttributeAttachableToEntity attachableToEntity)
        {
            if (attachableToEntity.AttachedShape != null)
            {
                return attachableToEntity.AttachedShape;
            }
            if (attachableToEntity.AttachedShapeBySlotCode != null)
            {
                foreach (var val in attachableToEntity.AttachedShapeBySlotCode)
                {
                    if (WildcardUtil.Match(val.Key, slotCode))
                    {
                        return val.Value;
                    }
                }
            }
        }

        _ = GetShape(new DummySlot(stack), variants, null, out CompositeShape? compositeShape);
        return compositeShape.RemoveNonExistingOverlays(coreApi)!;
    }

    public virtual string GetCategoryCode(ItemStack stack) => stack.GetByVariant(CategoryCodeByType!, defaultValue: () => iattr?.GetCategoryCode(stack))!;

    public virtual string[] GetDisableElements(ItemStack stack) => stack.GetByVariant(DisableElementsByType!, defaultValue: () => iattr?.GetDisableElements(stack))!;

    public virtual string[] GetKeepElements(ItemStack stack) => stack.GetByVariant(KeepElementsByType!, defaultValue: () => iattr?.GetKeepElements(stack))!;

    public virtual string GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(new DummySlot(stack));

    public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    public virtual int RequiresBehindSlots { get; set; }
    #endregion
    #region IPropertiesSupplier
    public virtual TagSet GetTags(ItemStack stack, ref EnumHandling handling)
    {
        if (stack.FindByVariant(TagsByType, out TagSet result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack, ref EnumHandling handling)
    {
        if (stack.FindByVariant(LightHsvByType, out byte[] result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual EnumItemDamageSource[] GetDamagedBy(ItemSlot slot, ref EnumHandling handling)
    {
        if (slot.Itemstack.FindByVariant(DamagedByByType, out EnumItemDamageSource[] result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual EnumTool? GetTool(ItemSlot slot, ref EnumHandling handling)
    {
        if (slot.Itemstack.FindByVariant(ToolByType, out EnumTool? result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual int GetToolTier(ItemSlot slot, ref EnumHandling handling)
    {
        if (slot.Itemstack.FindByVariant(ToolTierByType, out int result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(CombustiblePropsByType, out CombustibleProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.SmeltedStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        CombustibleProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.SmeltedStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Smelted stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.SmeltedStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(NutritionPropsByType, out FoodNutritionProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.EatenStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        FoodNutritionProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.EatenStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Eaten stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.EatenStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(GrindingPropsByType, out GrindingProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.GroundStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        GrindingProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.GroundStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Ground stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.GroundStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(CrushingPropsByType, out CrushingProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.CrushedStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        CrushingProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.CrushedStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Crushed stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.CrushedStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(TransitionablePropsByType, out TransitionableProperties[] result, out Variants variants) || result == null)
        {
            return result;
        }

        List<TransitionableProperties> allResolvedProps = [];

        for (int i = 0; i < result.Length; i++)
        {
            if (result[i].TransitionedStack == null)
            {
                allResolvedProps.Add(result[i]);
                continue;
            }

            TransitionableProperties clonedProps = result[i].Clone();
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.TransitionedStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(world.Api, this, $"Transitioned stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                continue;
            }

            allResolvedProps.Add(clonedProps);
        }

        handling = EnumHandling.PreventSubsequent;
        return [.. allResolvedProps];
    }

    public virtual JuiceableProperties GetJuiceableProperties(ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(JuiceablePropsByType, out JuiceableProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        JuiceableProperties clonedProps = new()
        {
            LitresPerItem = result.LitresPerItem,
            PressedDryRatio = result.PressedDryRatio,
            LiquidStack = result.LiquidStack?.Clone(),
            PressedStack = result.PressedStack?.Clone(),
            ReturnStack = result.ReturnStack?.Clone()
        };

        if (clonedProps.LiquidStack != null)
        {
            clonedProps.LiquidStack = variants.ReplacePlaceholders(clonedProps.LiquidStack);
            if (!clonedProps.LiquidStack.Resolve(coreApi.World, ""))
            {
                LoggerUtil.Warn(coreApi, this, $"Liquid stack with code '{clonedProps.LiquidStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.LiquidStack = null;
            }
        }
        if (clonedProps.PressedStack != null)
        {
            clonedProps.PressedStack = variants.ReplacePlaceholders(clonedProps.PressedStack);
            if (!clonedProps.PressedStack.Resolve(coreApi.World, ""))
            {
                LoggerUtil.Warn(coreApi, this, $"Pressed stack with code '{clonedProps.PressedStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.PressedStack = null;
            }
        }
        if (clonedProps.ReturnStack != null)
        {
            clonedProps.ReturnStack = variants.ReplacePlaceholders(clonedProps.ReturnStack);
            if (!clonedProps.ReturnStack.Resolve(coreApi.World, ""))
            {
                LoggerUtil.Warn(coreApi, this, $"Return stack with code '{clonedProps.ReturnStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.ReturnStack = null;
            }
        }

        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual DistillationProps GetDistillationProperties(ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(DistillationPropsByType, out DistillationProps result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.DistilledStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        DistillationProps clonedProps = new()
        {
            DistilledStack = result.DistilledStack?.Clone(),
            Ratio = result.Ratio
        };

        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.DistilledStack);
        if (!resultStack.Resolve(coreApi.World, ""))
        {
            LoggerUtil.Warn(coreApi, this, $"Distilled stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.DistilledStack = null;
        }

        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }
    #endregion
}
