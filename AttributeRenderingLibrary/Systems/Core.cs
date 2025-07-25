using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public override void StartPre(ICoreAPI api)
    {
        HarmonyInstance.PatchAllUncategorized();
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("AttributeRenderingLibrary.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(CollectibleBehaviorShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ContainedTransform", typeof(CollectibleBehaviorContainedTransform));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.AttachableToEntityTyped", typeof(CollectibleBehaviorAttachableToEntityTyped));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBagTyped", typeof(CollectibleBehaviorHeldBagTyped));
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }
}
