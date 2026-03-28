using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorGroundStoredProcessable(CollectibleObject collObj) : CollectibleBehavior(collObj), IContainedInteractable
{
    private static class DefaultValues
    {
        public static float ProcessTime => 0;
        public static BlockDropItemStack[]? ProcessedStacks => [];
        public static AssetLocation? ProcessingSound => null;
        public static string? ProcessingAnimationCode => null;
        public static JsonItemStack? RemainingItem => null;
        public static string? InteractionHelpCode => "blockhelp-processable-process";
        public static string? HandbookProcessIntoTitle => "groundstoredprocessesdesc-title";
        public static string? HandbookCreatedByTitle => "handbook-createdby-groundstoredprocessing";
        public static EnumTool? Tool => null;
        public static int ToolDamage = 1;
    }

    [JsonProperty("ProcessTime")]
    public Dictionary<string, float>? ProcessTimeByType { get; protected set; }

    [JsonProperty("ProcessedStacks")]
    public Dictionary<string, BlockDropItemStack[]?>? ProcessedStacksByType { get; protected set; }

    [JsonProperty("ProcessingSound")]
    public Dictionary<string, AssetLocation?>? ProcessingSoundByType { get; protected set; }

    [JsonProperty("ProcessingAnimationCode")]
    public Dictionary<string, string?>? ProcessingAnimationCodeByType { get; protected set; }

    [JsonProperty("RemainingItem")]
    public Dictionary<string, JsonItemStack?>? RemainingItemByType { get; protected set; }

    [JsonProperty("interactionHelpCode")]
    public Dictionary<string, string?>? InteractionHelpCodeByType { get; protected set; }

    [JsonProperty("HandbookProcessIntoTitle")]
    public Dictionary<string, string?>? HandbookProcessIntoTitleByType { get; protected set; }

    [JsonProperty("HandbookCreatedByTitle")]
    public Dictionary<string, string?>? HandbookCreatedByTitleByType { get; protected set; }

    [JsonProperty("Tool")]
    public Dictionary<string, EnumTool?>? ToolByType { get; protected set; }

    [JsonProperty("ToolDamage")]
    public Dictionary<string, int>? ToolDamageByType { get; protected set; }

    #nullable disable
    protected ICoreAPI api;
    #nullable enable

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties.Exists) JsonUtil.Populate(properties.Token, this);
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        this.api = api;

        if (ProcessedStacksByType is not { Count: > 0 } && RemainingItemByType is not { Count: > 0 })
        {
            LoggerUtil.Warn(api, this, $"{collObj.Code} has no processedStacks or remainingItem specified for GroundStoredProcessable behavior");
        }
    }

    public virtual float GetProcessTime(ItemSlot slot) => slot.Itemstack.GetByVariant(ProcessTimeByType, DefaultValues.ProcessTime);
    public virtual AssetLocation? GetProcessingSound(ItemSlot slot) => slot.Itemstack.GetByVariant(ProcessingSoundByType, DefaultValues.ProcessingSound);
    public virtual string? GetProcessingAnimationCode(ItemSlot slot) => slot.Itemstack.GetByVariant(ProcessingAnimationCodeByType, DefaultValues.ProcessingAnimationCode);
    public virtual string? GetInteractionHelpCode(ItemSlot slot) => slot.Itemstack.GetByVariant(InteractionHelpCodeByType, DefaultValues.InteractionHelpCode);
    public virtual string? GetHandbookProcessIntoTitle(ItemSlot slot) => slot.Itemstack.GetByVariant(HandbookProcessIntoTitleByType, DefaultValues.HandbookProcessIntoTitle);
    public virtual string? GetHandbookCreatedByTitle(ItemSlot slot) => slot.Itemstack.GetByVariant(HandbookCreatedByTitleByType, DefaultValues.HandbookCreatedByTitle);
    public virtual EnumTool? GetTool(ItemSlot slot) => slot.Itemstack.GetByVariant(ToolByType, DefaultValues.Tool);
    public virtual int GetToolDamage(ItemSlot slot) => slot.Itemstack.GetByVariant(ToolDamageByType, DefaultValues.ToolDamage);

    public virtual BlockDropItemStack[]? GetProcessedStacks(ItemSlot slot)
    {
        if (!slot.Itemstack.FindByVariant(ProcessedStacksByType, out BlockDropItemStack[] result, out Variants variants) || result == null)
        {
            return result;
        }

        List<BlockDropItemStack> resolvedStacks = [];
        if (result != null)
        {
            foreach (BlockDropItemStack unresolvedStack in result)
            {
                BlockDropItemStack resolvedStack = variants.ReplacePlaceholders(unresolvedStack.Clone());
                if (resolvedStack.Resolve(api.World, "processedStack of item ", collObj.Code))
                {
                    resolvedStacks.Add(resolvedStack);
                }
            }
        }
        return [.. resolvedStacks];
    }

    public virtual JsonItemStack? GetRemainingItem(ItemSlot slot)
    {
        if (!slot.Itemstack.FindByVariant(RemainingItemByType, out JsonItemStack result, out Variants variants) || result == null)
        {
            return result;
        }

        JsonItemStack resolvedStack = variants.ReplacePlaceholders(result.Clone());
        resolvedStack?.Resolve(api.World, "remainingItem of item ", collObj.Code);
        return resolvedStack;
    }

    public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        var requiredTool = GetTool(slot);
        if (requiredTool != null && byPlayer.InventoryManager.ActiveTool != requiredTool) return false;

        if (!byPlayer.Entity.Controls.ShiftKey) return false;

        if (!be.Api.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
        {
            return false;
        }

        if (GetProcessedStacks(slot) != null || GetRemainingItem(slot) != null)
        {
            be.Api.World.PlaySoundAt(GetProcessingSound(slot), blockSel.Position, 0, byPlayer);
            return true;
        }

        return false;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        var requiredTool = GetTool(slot);
        if (requiredTool != null && byPlayer.InventoryManager.ActiveTool != requiredTool) return false;

        if (!byPlayer.Entity.Controls.ShiftKey) return false;

        if (blockSel == null) return false;

        string? processingAnimationCode = GetProcessingAnimationCode(slot);
        if (processingAnimationCode != null && byPlayer.Entity.World is IClientWorldAccessor) byPlayer.Entity.StartAnimation(processingAnimationCode);

        if (be.Api.World.Rand.NextDouble() < 0.05)
        {
            be.Api.World.PlaySoundAt(GetProcessingSound(slot), blockSel.Position, 0, byPlayer);
        }

        if (be.Api.World.Side == EnumAppSide.Client && be.Api.World.Rand.NextDouble() < 0.25 && (GetProcessedStacks(slot)?[0]?.ResolvedItemstack ?? GetRemainingItem(slot)?.ResolvedItemstack) is { } outputStack)
        {
            be.Api.World.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(blockSel.HitPosition), outputStack, 0.25f, 1, 0.5f, byPlayer, new Vec3f(0, 1, 0));
        }

        return secondsUsed < GetProcessTime(slot);
    }

    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        byPlayer.Entity.StopAnimation(GetProcessingAnimationCode(slot));

        EnumTool? requiredTool = GetTool(slot);
        if (requiredTool != null && byPlayer.InventoryManager.ActiveTool != requiredTool) return;

        int toolDamage = GetToolDamage(slot);
        AssetLocation? processingSound = GetProcessingSound(slot);
        BlockDropItemStack[]? processedStacks = GetProcessedStacks(slot);
        JsonItemStack? remainingItem = GetRemainingItem(slot);
        if (secondsUsed > GetProcessTime(slot) - 0.05f && (processedStacks != null || remainingItem != null) && be.Api.World.Side == EnumAppSide.Server)
        {
            processedStacks?.Foreach(processedStack =>
            {
                ItemStack? stack = processedStack.GetNextItemStack(slot.Itemstack.StackSize);
                if (stack == null) return;
                var origStack = stack.Clone();
                var quantity = stack.StackSize;
                if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    be.Api.World.SpawnItemEntity(stack, blockSel.Position);
                }
                be.Api.World.Logger.Audit("{0} Took {1}x{2} from {3} at {4}.",
                    byPlayer.PlayerName,
                    quantity,
                    stack.Collectible.Code,
                    collObj.Code,
                    blockSel.Position
                );

                TreeAttribute tree = new TreeAttribute();
                tree["itemstack"] = new ItemstackAttribute(origStack.Clone());
                tree["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
                be.Api.World.Api.Event.PushEvent("onitemcollected", tree);
            });

            int stacksize = slot.StackSize;
            slot.Itemstack = remainingItem?.ResolvedItemstack?.Clone();
            slot.Itemstack?.StackSize *= stacksize;
            be.MarkDirty(true);
            if (be.Inventory.Empty) be.Api.World.BlockAccessor.SetBlock(0, blockSel.Position);

            if (requiredTool != null)
            {
                var toolSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                toolSlot.Itemstack?.Collectible.DamageItem(be.Api.World, byPlayer.Entity, toolSlot, toolDamage);
            }

            be.Api.World.PlaySoundAt(processingSound, blockSel.Position, 0, byPlayer);
        }
    }

    public bool OnContainedInteractCancel(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
    {
        byPlayer.Entity.StopAnimation(GetProcessingAnimationCode(slot));
        return true;
    }

    public WorldInteraction[] GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        BlockDropItemStack[]? processedStacks = GetProcessedStacks(slot);
        JsonItemStack? remainingItem = GetRemainingItem(slot);
        if (processedStacks != null || remainingItem != null)
        {
            bool notProtected = true;

            if (be.Api.World.Claims != null && be.Api.World is IClientWorldAccessor clientWorld && clientWorld.Player?.WorldData.CurrentGameMode == EnumGameMode.Survival)
            {
                EnumWorldAccessResponse resp = clientWorld.Claims.TestAccess(clientWorld.Player, blockSel.Position, EnumBlockAccessFlags.Use);
                if (resp != EnumWorldAccessResponse.Granted) notProtected = false;
            }

            if (notProtected) return
            [
                new()
                    {
                        ActionLangCode = GetInteractionHelpCode(slot),
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = GetTool(slot) == null ? null : ObjectCacheUtil.GetToolStacks(be.Api, (EnumTool)GetTool(slot))
                    }
            ];
        }
        return [];
    }
}