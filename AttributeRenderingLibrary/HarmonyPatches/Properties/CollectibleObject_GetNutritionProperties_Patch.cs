using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetNutritionProperties))]
public static class CollectibleObject_GetNutritionProperties_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref FoodNutritionProperties __result, IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not IPropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        FoodNutritionProperties value = propertiesSupplier.GetNutritionProperties(world, itemstack, forEntity, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}