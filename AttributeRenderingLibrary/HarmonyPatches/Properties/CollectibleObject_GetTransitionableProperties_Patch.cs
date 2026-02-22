using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetTransitionableProperties))]
public static class CollectibleObject_GetTransitionableProperties_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref TransitionableProperties[] __result, IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        TransitionableProperties[] value = propertiesSupplier.GetTransitionableProperties(world, itemstack, forEntity, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}