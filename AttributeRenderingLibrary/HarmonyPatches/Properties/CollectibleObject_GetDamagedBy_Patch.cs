using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetDamagedBy))]
public static class CollectibleObject_GetDamagedBy_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref EnumItemDamageSource[] __result, ItemSlot slot)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        EnumItemDamageSource[] value = propertiesSupplier.GetDamagedBy(slot, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}
