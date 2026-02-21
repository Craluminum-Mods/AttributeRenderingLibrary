using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetTags))]
public static class CollectibleObject_GetTags_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref TagSet __result, ItemStack stack)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not IPropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        TagSet value = propertiesSupplier.GetTags(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}