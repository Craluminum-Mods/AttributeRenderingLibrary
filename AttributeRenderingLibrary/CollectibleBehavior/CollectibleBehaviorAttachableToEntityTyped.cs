using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorAttachableToEntityTyped : CollectibleBehavior, IAttachableToEntity
{
    public Dictionary<string, OrderedDictionary<string, CompositeShape>> attachedShapeBySlotCodeByType = new();
    public Dictionary<string, string> categoryCodeByType = new();
    public Dictionary<string, string[]> disableElementsByType = new();
    public Dictionary<string, string[]> keepElementsByType = new();
    private IAttachableToEntity attrAtta;
    private ICoreAPI api;

    public CollectibleBehaviorAttachableToEntityTyped(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        attachedShapeBySlotCodeByType = properties["attachedShapeBySlotCode"].AsObject(defaultValue: new Dictionary<string, OrderedDictionary<string, CompositeShape>>());
        categoryCodeByType = properties["categoryCode"].AsObject(defaultValue: new Dictionary<string, string>());
        disableElementsByType = properties["disableElements"].AsObject(defaultValue: new Dictionary<string, string[]>());
        keepElementsByType = properties["keepElements"].AsObject(defaultValue: new Dictionary<string, string[]>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
        attrAtta = IAttachableToEntity.FromAttributes(collObj);
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
                ctex.Bake(api.Assets);
                intoDict[textureCode] = ctex;
                shape.Textures[textureCode] = ctex.Baked.BakedName;
            }
        }
    }

    CompositeShape IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
    {
        if (attachedShapeBySlotCodeByType == null || !attachedShapeBySlotCodeByType.Any())
        {
            return attrAtta?.GetAttachedShape(stack, slotCode);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(attachedShapeBySlotCodeByType, out OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode);

        if (attachedShapeBySlotCode != null)
        {
            foreach ((string _slotCode, CompositeShape cshape) in attachedShapeBySlotCode)
            {
                if (WildcardUtil.Match(_slotCode, slotCode))
                {
                    CompositeShape rcshape = variants.ReplacePlaceholders(cshape.Clone());
                    return rcshape;
                }
            }
        }

        return attrAtta?.GetAttachedShape(stack, slotCode);
    }

    string IAttachableToEntity.GetCategoryCode(ItemStack stack)
    {
        if (categoryCodeByType == null || !categoryCodeByType.Any())
        {
            return attrAtta?.GetCategoryCode(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(categoryCodeByType, out string categoryCode);
        return categoryCode;
    }

    string[] IAttachableToEntity.GetDisableElements(ItemStack stack)
    {
        if (disableElementsByType == null || !disableElementsByType.Any())
        {
            return attrAtta?.GetDisableElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(disableElementsByType, out string[] disableElements);
        return disableElements;
    }

    string[] IAttachableToEntity.GetKeepElements(ItemStack stack)
    {
        if (keepElementsByType == null || !keepElementsByType.Any())
        {
            return attrAtta?.GetKeepElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(keepElementsByType, out string[] keepElements);
        return keepElements;
    }

    string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack)
    {
        string texturePrefixCode = stack.Collectible.GetCollectibleInterface<IContainedMeshSource>().GetMeshCacheKey(stack);
        return texturePrefixCode;
    }

    bool IAttachableToEntity.IsAttachable(Entity toEntity, ItemStack itemStack)
    {
        return true;
    }
}
