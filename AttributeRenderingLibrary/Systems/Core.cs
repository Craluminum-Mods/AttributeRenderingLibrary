using AttributeRenderingLibrary.Utility.Creativestacks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace AttributeRenderingLibrary;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new(Mod.Info.ModID);

    public static ICoreAPI Api;

    public override void StartPre(ICoreAPI api)
    {
        Api = api;

        if (!Harmony.HasAnyPatches(HarmonyInstance.Id))
        {
            HarmonyInstance.PatchAllUncategorized();
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
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.WearableAttachment", typeof(AttributeRenderingLibrary.CollectibleBehaviorWearableAttachment));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.Wearable", typeof(AttributeRenderingLibrary.CollectibleBehaviorWearable));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GenerateCreativeStacks", typeof(CollectibleBehaviorGenerateCreativeStacks));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.DisplayableProps", typeof(AttributeRenderingLibrary.CollectibleBehaviorDisplayableProps));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.FoodTags", typeof(AttributeRenderingLibrary.CollectibleBehaviorFoodTags));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HealingItem", typeof(AttributeRenderingLibrary.CollectibleBehaviorHealingItem));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GroundStoredProcessable", typeof(AttributeRenderingLibrary.CollectibleBehaviorGroundStoredProcessable));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.AnvilWorkable", typeof(AttributeRenderingLibrary.CollectibleBehaviorAnvilWorkable));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ContainedTransform", typeof(CollectibleBehaviorCustomTransform));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.CustomTransform", typeof(CollectibleBehaviorCustomTransform));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBagTyped", typeof(AttributeRenderingLibrary.CollectibleBehaviorHeldBag));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBag", typeof(AttributeRenderingLibrary.CollectibleBehaviorHeldBag));
    }

    public void RegisterBlockBehaviors(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.BlockShapeTexturesFromAttributes", typeof(BlockBehaviorShapeTexturesFromAttributes));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalOrientable", typeof(AttributeRenderingLibrary.BlockBehaviorHorizontalOrientable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalAttachable", typeof(AttributeRenderingLibrary.BlockBehaviorHorizontalAttachable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.NWOrientable", typeof(AttributeRenderingLibrary.BlockBehaviorNWOrientable));
    }

    public void RegisterBlockEntityBehaviors(ICoreAPI api)
    {
        api.RegisterBlockEntityBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(BlockEntityBehaviorShapeTexturesFromAttributes));
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (api is ICoreServerAPI sapi)
        {
            CreateCreativeStacks(sapi);
        }
    }

    private void CreateCreativeStacks(ICoreServerAPI sapi)
    {
        VariantLoader loader = new(sapi);
        loader.CollectCollectibleObjectsToGenerate();
        loader.CollectVariantsFromWorldProperties();
        loader.ComposeVariants();
    }
}
