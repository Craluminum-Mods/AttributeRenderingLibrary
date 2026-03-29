using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorAnvilWorkable(CollectibleObject collObj) : CollectibleBehavior(collObj), IAnvilWorkable
{
    public Dictionary<string, float>? WorkableTemperatureByType { get; protected set; }
    public Dictionary<string, EnumHelveWorkableMode>? HelveWorkableModeByType { get; protected set; }
    public Dictionary<string, int>? RequiresAnvilTierByType { get; protected set; }

    public Dictionary<string, string>? MetalByType { get; protected set; }
    public Dictionary<string, Vec3i>? SizeByType { get; protected set; }
    public Dictionary<string, Vec2i>? OffsetByType { get; protected set; }

#nullable disable
    public ICoreAPI coreApi;
#nullable enable

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties is not { Count: > 0 }) return;

        WorkableTemperatureByType = properties["workableTemperature"].AsObject<Dictionary<string, float>>();
        HelveWorkableModeByType = properties["helveWorkableMode"].AsObject<Dictionary<string, EnumHelveWorkableMode>>();
        RequiresAnvilTierByType = properties["requiresAnvilTier"].AsObject<Dictionary<string, int>>();

        MetalByType = properties["metal"].AsObject<Dictionary<string, string>>();
        SizeByType = properties["size"].AsObject<Dictionary<string, Vec3i>>();
        OffsetByType = properties["offset"].AsObject<Dictionary<string, Vec2i>>();
    }

    public override void OnLoaded(ICoreAPI api)
    {
        coreApi = api;
    }

    public virtual bool CanWork(ItemStack stack)
    {
        float temperature = stack.Collectible.GetTemperature(coreApi.World, stack);
        float meltingpoint = stack.Collectible.GetMeltingPoint(coreApi.World, null, new DummySlot(stack));

        float workableTemperature = stack.GetByVariant(WorkableTemperatureByType, meltingpoint / 2);
        return workableTemperature <= temperature;
    }

    public virtual ItemStack GetBaseMaterial(ItemStack stack) => stack;

    public virtual EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil) => stack.GetByVariant(HelveWorkableModeByType, EnumHelveWorkableMode.NotWorkable);

    public virtual List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        Item? baseItem = coreApi.World.GetItem(new AssetLocation("ingot-" + GetMetal(stack)));
        if (baseItem == null) return [];

        ItemStack basemat = new ItemStack(baseItem);
        return (from r in coreApi.GetSmithingRecipes()
                where r.Ingredient.SatisfiesAsIngredient(basemat)
                where r.Output.ResolvedItemstack.Collectible.Code != stack.Collectible.Code
                orderby r.Output.ResolvedItemstack.Collectible.Code
                select r).ToList();
    }

    public virtual int GetRequiredAnvilTier(ItemStack stack) => stack.GetByVariant(RequiresAnvilTierByType, defaultValue: () => GetDefaultAnvilTier(stack));

    public virtual int GetDefaultAnvilTier(ItemStack stack)
    {
        if (coreApi.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(GetMetal(stack) ?? "", out MetalPropertyVariant? var))
        {
            return var.Tier - 1;
        }
        return 0;
    }

    public virtual ItemStack? TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (!CanWork(stack)) return null;

        string? metal = GetMetal(stack);
        if (string.IsNullOrEmpty(metal)) return null;

        bool isBlisterSteel = IsBlisterSteel(metal);
        Item? item = coreApi.World.GetItem(new AssetLocation("workitem-" + GetMetal(stack)));
        if (item == null) return null;

        ItemStack workItemStack = new ItemStack(item);
        workItemStack.Collectible.SetTemperature(coreApi.World, workItemStack, stack.Collectible.GetTemperature(coreApi.World, stack));

        if (beAnvil.WorkItemStack == null)
        {
            CreateVoxelsFromItem(stack, coreApi, ref beAnvil.Voxels, isBlisterSteel);
        }
        else
        {
            if (isBlisterSteel) return null;

            if (!string.Equals(beAnvil.WorkItemStack.Collectible.Variant["metal"], metal))
            {
                if (coreApi is ICoreClientAPI capi) capi.TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
                return null;
            }

            if (AddVoxelsFromItem(stack, ref beAnvil.Voxels) == 0)
            {
                if (coreApi is ICoreClientAPI capi) capi.TriggerIngameError(this, "requireshammering", Lang.Get("Try hammering down before adding additional voxels"));
                return null;
            }
        }

        return workItemStack;
    }

    public virtual int VoxelCountForHandbook(ItemStack stack)
    {
        Vec3i voxelSize = GetVoxelSize(stack);
        return voxelSize.X * voxelSize.Y * voxelSize.Z;
    }

    public virtual string? GetMetal(ItemStack stack) => stack.FindByVariant(MetalByType!, out string? metal, out Variants variants) ? variants.ReplacePlaceholders(metal) : null;

    public virtual bool IsBlisterSteel(string metal) => metal == "blistersteel";

    private static readonly Vec3i MaxVoxelSize = new Vec3i(16, 6, 16);

    public virtual Vec3i GetVoxelSize(ItemStack stack)
    {
        Vec3i size = stack.GetByVariant(SizeByType) ?? new Vec3i(1, 1, 1);
        return new Vec3i(
            x: Math.Min(size.X, MaxVoxelSize.X),
            y: Math.Min(size.Y, MaxVoxelSize.Y),
            z: Math.Min(size.Z, MaxVoxelSize.Z));
    }

    public virtual Vec2i GetVoxelOffset(ItemStack stack) => stack.GetByVariant(OffsetByType) ?? new Vec2i(4, 6);

    public virtual void CreateVoxelsFromItem(ItemStack stack, ICoreAPI api, ref byte[,,] voxels, bool isBlisterSteel = false)
    {
        voxels = new byte[16, 6, 16];
        Vec3i size = GetVoxelSize(stack);
        Vec2i offset = GetVoxelOffset(stack);

        int offsetX = Math.Clamp(offset.X, 0, 16 - size.X);
        int offsetZ = Math.Clamp(offset.Y, 0, 16 - size.Z);

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    voxels[offsetX + x, y, offsetZ + z] = (byte)EnumVoxelMaterial.Metal;

                    if (isBlisterSteel)
                    {
                        if (y + 1 < 6)
                        {
                            if (api.World.Rand.NextDouble() < 0.5)
                                voxels[offsetX + x, y + 1, offsetZ + z] = (byte)EnumVoxelMaterial.Metal;
                            if (api.World.Rand.NextDouble() < 0.5)
                                voxels[offsetX + x, y + 1, offsetZ + z] = (byte)EnumVoxelMaterial.Slag;
                        }
                    }
                }
            }
        }
    }

    public virtual int AddVoxelsFromItem(ItemStack stack, ref byte[,,] voxels)
    {
        int totalAdded = 0;
        Vec3i size = GetVoxelSize(stack);
        Vec2i offset = GetVoxelOffset(stack);

        int offsetX = Math.Clamp(offset.X, 0, 16 - size.X);
        int offsetZ = Math.Clamp(offset.Y, 0, 16 - size.Z);

        for (int x = 0; x < size.X; x++)
        {
            for (int z = 0; z < size.Z; z++)
            {
                int y = 0;
                int added = 0;
                while (y < 6 && added < size.Y)
                {
                    if (voxels[offsetX + x, y, offsetZ + z] == (byte)EnumVoxelMaterial.Empty)
                    {
                        voxels[offsetX + x, y, offsetZ + z] = (byte)EnumVoxelMaterial.Metal;
                        added++;
                        totalAdded++;
                    }
                    y++;
                }
            }
        }
        return totalAdded;
    }
}