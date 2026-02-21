using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetCombustibleProperties))]
public static class CollectibleObject_GetCombustibleProperties_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref CombustibleProperties __result, IWorldAccessor world, ItemStack itemstack, BlockPos pos)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not IPropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        CombustibleProperties value = propertiesSupplier.GetCombustibleProperties(world, itemstack, pos, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}

//public CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack itemstack, BlockPos pos)
//public FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
//public GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack itemstack)
//public CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack itemstack)
//public TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)