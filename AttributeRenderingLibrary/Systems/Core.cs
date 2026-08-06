using AttributeRenderingLibrary.HarmonyPatches;
using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new(Mod.Info.ModID);

    private Harmony HarmonyAttributeRedirectionInstance => new("ARL (Ignore this, look at stacktrace instead! in 99.9% cases the crash is not related to ARL)");

    public static ICoreAPI Api;

    public override void StartPre(ICoreAPI api)
    {
        Api = api;

        if (!Harmony.HasAnyPatches(HarmonyInstance.Id))
        {
            HarmonyInstance.PatchAllUncategorized();
        }
        
        if (!Harmony.HasAnyPatches(HarmonyAttributeRedirectionInstance.Id))
        {
            AttributeRedirectionPatch.ScanAndApply(HarmonyAttributeRedirectionInstance, Mod.Logger);
        }

        if (api.Side.IsServer())
        {
            HarmonyInstance.PatchCategory("Server");
        }

        if (api.Side.IsClient())
        {
            HarmonyInstance.PatchCategory("Client");
        }
    }

    public override void Start(ICoreAPI api)
    {
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
        RegisterItems(api);
        RegisterCollectibleBehaviors(api);
        RegisterBlockBehaviors(api);
        RegisterBlockEntityBehaviors(api);
    }

    public void RegisterItems(ICoreAPI api)
    {
        api.RegisterItemClass("AttributeRenderingLibrary.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
    }

    public void RegisterCollectibleBehaviors(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(CollectibleBehaviorShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.WearableAttachment", typeof(CollectibleBehaviorWearableAttachment));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.Wearable", typeof(CollectibleBehaviorWearable));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GenerateCreativeStacks", typeof(CollectibleBehaviorGenerateCreativeStacks));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.DisplayableProps", typeof(CollectibleBehaviorDisplayableProps));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.FoodTags", typeof(CollectibleBehaviorFoodTags));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HealingItem", typeof(CollectibleBehaviorHealingItem));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GroundStoredProcessable", typeof(CollectibleBehaviorGroundStoredProcessable));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.AnvilWorkable", typeof(CollectibleBehaviorAnvilWorkable));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ContainedTransform", typeof(CollectibleBehaviorCustomTransform));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.CustomTransform", typeof(CollectibleBehaviorCustomTransform));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBagTyped", typeof(CollectibleBehaviorHeldBag));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBag", typeof(CollectibleBehaviorHeldBag));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.Handbook", typeof(CollectibleBehaviorHandbook));
    }

    public void RegisterBlockBehaviors(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.BlockShapeTexturesFromAttributes", typeof(BlockBehaviorShapeTexturesFromAttributes));

        // backward compatibility
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalOrientable", typeof(Vintagestory.GameContent.BlockBehaviorHorizontalOrientable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalAttachable", typeof(Vintagestory.GameContent.BlockBehaviorHorizontalAttachable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.NWOrientable", typeof(Vintagestory.GameContent.BlockBehaviorNWOrientable));
    }

    public void RegisterBlockEntityBehaviors(ICoreAPI api)
    {
        api.RegisterBlockEntityBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(BlockEntityBehaviorShapeTexturesFromAttributes));
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
        HarmonyAttributeRedirectionInstance.UnpatchAll(HarmonyAttributeRedirectionInstance.Id);
    }
}