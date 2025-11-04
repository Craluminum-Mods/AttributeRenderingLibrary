using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public static ICoreAPI Api;

    public override void StartPre(ICoreAPI api)
    {
        Api = api;
        HarmonyInstance.PatchAllUncategorized();
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("AttributeRenderingLibrary.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(CollectibleBehaviorShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ContainedTransform", typeof(CollectibleBehaviorContainedTransform));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBagTyped", typeof(CollectibleBehaviorHeldBagTyped));

        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.BlockShapeTexturesFromAttributes", typeof(BlockBehaviorShapeTexturesFromAttributes));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalOrientable", typeof(AttributeRenderingLibrary.BlockBehaviorHorizontalOrientable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalAttachable", typeof(AttributeRenderingLibrary.BlockBehaviorHorizontalAttachable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.NWOrientable", typeof(AttributeRenderingLibrary.BlockBehaviorNWOrientable));

        api.RegisterBlockEntityBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(BlockEntityBehaviorShapeTexturesFromAttributes));
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }
}
