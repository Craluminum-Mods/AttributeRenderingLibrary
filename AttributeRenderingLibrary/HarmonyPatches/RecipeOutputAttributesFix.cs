using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary.HarmonyPatches;

public static class RecipeOutputAttributesFix
{
    [HarmonyPatch(typeof(JsonItemStack), nameof(JsonItemStack.ToBytes))]
    public static class JsonItemStack_ToBytes_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(JsonItemStack __instance, BinaryWriter writer)
        {
            writer.Write((short)__instance.Type);
            writer.Write(__instance.Code.ToShortString());
            writer.Write(__instance.StackSize);
            writer.Write(__instance.ResolvedItemStack != null);
            __instance.ResolvedItemStack?.ToBytes(writer);

            string? jsonAttruibutes = __instance.Attributes?.ToString();
            writer.Write(jsonAttruibutes != null);
            if (jsonAttruibutes != null)
            {
                writer.Write(jsonAttruibutes);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(JsonItemStack), nameof(JsonItemStack.FromBytes), argumentTypes: [typeof(BinaryReader), typeof(IClassRegistryAPI) ])]
    public static class JsonItemStack_FromBytes_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(JsonItemStack __instance, BinaryReader reader, IClassRegistryAPI instancer)
        {
            __instance.Type = (EnumItemClass)reader.ReadInt16();
            __instance.Code = new AssetLocation(reader.ReadString());
            __instance.StackSize = reader.ReadInt32();

            if (reader.ReadBoolean())
            {
                __instance.ResolvedItemStack = new ItemStack(reader);
            }

            if (reader.ReadBoolean())
            {
                __instance.Attributes = new JsonObject(JToken.Parse(reader.ReadString()));
            }

            return false;
        }
    }
}