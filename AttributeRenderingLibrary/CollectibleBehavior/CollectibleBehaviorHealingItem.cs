using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorHealingItem(CollectibleObject collObj) : CollectibleBehavior(collObj), ICanHealCreature
{
    private static class DefaultValues
    {
        public static float Health => 1;
        public static float ApplicationTimeSec => 2;
        public static float MaxApplicationTimeSec => 10;
        public static int Ticks => 10;
        public static float EffectDurationSec => 10;
        public static bool CancelInAir => true;
        public static bool CancelWhileSwimming => false;
        public static AssetLocation? Sound => new AssetLocation("game:sounds/player/poultice");
        public static AssetLocation? AppliedSound => new AssetLocation("game:sounds/player/poultice-applied");
        public static float SoundRange => 8;
        public static bool CanRevive => true;
        public static bool AffectedByArmor => true;
        public static float DelayToCancelSec => 0.5f;
    }

    [JsonProperty("Health")]
    public Dictionary<string, float>? HealthByType { get; protected set; }

    [JsonProperty("ApplicationTimeSec")]
    public Dictionary<string, float>? ApplicationTimeSecByType { get; protected set; }

    [JsonProperty("MaxApplicationTimeSec")]
    public Dictionary<string, float>? MaxApplicationTimeSecByType { get; protected set; }

    [JsonProperty("Ticks")]
    public Dictionary<string, int>? TicksByType { get; protected set; }

    [JsonProperty("EffectDurationSec")]
    public Dictionary<string, float>? EffectDurationSecByType { get; protected set; }

    [JsonProperty("CancelInAir")]
    public Dictionary<string, bool>? CancelInAirByType { get; protected set; }

    [JsonProperty("CancelWhileSwimming")]
    public Dictionary<string, bool>? CancelWhileSwimmingByType { get; protected set; }

    [JsonProperty("Sound")]
    public Dictionary<string, AssetLocation?>? SoundByType { get; protected set; }

    [JsonProperty("AppliedSound")]
    public Dictionary<string, AssetLocation?>? AppliedSoundByType { get; protected set; }

    [JsonProperty("SoundRange")]
    public Dictionary<string, float>? SoundRangeByType { get; protected set; }

    [JsonProperty("CanRevive")]
    public Dictionary<string, bool>? CanReviveByType { get; protected set; }

    [JsonProperty("AffectedByArmor")]
    public Dictionary<string, bool>? AffectedByArmorByType { get; protected set; }

    [JsonProperty("DelayToCancelSec")]
    public Dictionary<string, float>? DelayToCancelSecByType { get; protected set; }

    protected IProgressBar? progressBarRender;
    protected Dictionary<string, ILoadedSound?>? applicationSoundByKey;

    #nullable disable
    protected ICoreAPI api;
    #nullable enable

    protected float secondsUsedToCancel = 0;

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties.Exists) JsonUtil.Populate(properties.Token, this);
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        applicationSoundByKey?.Foreach(x => x.Value?.Dispose());
        applicationSoundByKey?.Clear();;
    }

    public virtual ILoadedSound? GetOrLoadApplicationSound(ItemSlot slot)
    {
        if (api is not ICoreClientAPI capi) return null;

        AssetLocation? soundLocation = GetSound(slot);
        if (soundLocation == null) return null;

        float soundRange = GetSoundRange(slot);
        string key = $"{soundRange}-{soundLocation}";

        applicationSoundByKey ??= [];
        if (applicationSoundByKey.TryGetValue(key, out ILoadedSound? cached) && cached != null && !cached.IsDisposed)
        {
            return cached;
        }

        return applicationSoundByKey[key] = capi.World.LoadSound(new SoundParams()
        {
            DisposeOnFinish = false,
            Location = soundLocation,
            ShouldLoop = true,
            Range = soundRange
        });
    }

    public virtual float GetHealth(ItemSlot slot) => slot.Itemstack.GetByVariant(HealthByType, DefaultValues.Health);
    public virtual float GetApplicationTimeSec(ItemSlot slot) => slot.Itemstack.GetByVariant(ApplicationTimeSecByType, DefaultValues.ApplicationTimeSec);
    public virtual float GetMaxApplicationTimeSec(ItemSlot slot) => slot.Itemstack.GetByVariant(MaxApplicationTimeSecByType, DefaultValues.MaxApplicationTimeSec);
    public virtual int GetTicks(ItemSlot slot) => slot.Itemstack.GetByVariant(TicksByType, DefaultValues.Ticks);
    public virtual float GetEffectDurationSec(ItemSlot slot) => slot.Itemstack.GetByVariant(EffectDurationSecByType, DefaultValues.EffectDurationSec);
    public virtual bool CancelInAir(ItemSlot slot) => slot.Itemstack.GetByVariant(CancelInAirByType, DefaultValues.CancelInAir);
    public virtual bool CancelWhileSwimming(ItemSlot slot) => slot.Itemstack.GetByVariant(CancelWhileSwimmingByType, DefaultValues.CancelWhileSwimming);
    public virtual AssetLocation? GetSound(ItemSlot slot) => slot.Itemstack.GetByVariant(SoundByType, DefaultValues.Sound);
    public virtual AssetLocation? GetAppliedSound(ItemSlot slot) => slot.Itemstack.GetByVariant(AppliedSoundByType, DefaultValues.AppliedSound);
    public virtual float GetSoundRange(ItemSlot slot) => slot.Itemstack.GetByVariant(SoundRangeByType, DefaultValues.SoundRange);
    public virtual bool CanRevive(ItemSlot slot) => slot.Itemstack.GetByVariant(CanReviveByType, DefaultValues.CanRevive);
    public virtual bool AffectedByArmor(ItemSlot slot) => slot.Itemstack.GetByVariant(AffectedByArmorByType, DefaultValues.AffectedByArmor);
    public virtual float GetDelayToCancelSec(ItemSlot slot) => slot.Itemstack.GetByVariant(DelayToCancelSecByType, DefaultValues.DelayToCancelSec);

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        if (CancelApplication(slot, byEntity))
        {
            return;
        }

        handHandling = EnumHandHandling.PreventDefault;
        handling = EnumHandling.PreventSubsequent;

        api?.World.RegisterCallback(_ => GetOrLoadApplicationSound(slot)?.Stop(), (int)GetApplicationTime(slot, byEntity) * 1000);

        if (api?.Side == EnumAppSide.Client)
        {
            ModSystemProgressBar progressBarSystem = api.ModLoader.GetModSystem<ModSystemProgressBar>();
            progressBarSystem.RemoveProgressbar(progressBarRender);
            progressBarRender = progressBarSystem.AddProgressbar();
        }

        secondsUsedToCancel = 0;
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
    {
        if (!CancelApplication(slot, byEntity))
        {
            secondsUsedToCancel = 0;
        }
        else
        {
            if (secondsUsedToCancel == 0) secondsUsedToCancel = secondsUsed;
        }

        if (CancelApplication(slot, byEntity) && secondsUsed - secondsUsedToCancel > 0.5)
        {
            return false;
        }

        ILoadedSound? applicationSound = GetOrLoadApplicationSound(slot);
        if (applicationSound?.HasStopped == true)
        {
            applicationSound.Start();
        }

        applicationSound?.SetPosition((float)byEntity.Pos.X, (float)byEntity.Pos.InternalY, (float)byEntity.Pos.Z);

        handling = EnumHandling.Handled;

        float progress = secondsUsed / (GetApplicationTime(slot, byEntity));
        if (progressBarRender != null)
        {
            progressBarRender.Progress = progress;
        }
        return progress < 1;
    }

    public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
    {
        GetOrLoadApplicationSound(slot)?.Stop();
        api?.ModLoader.GetModSystem<ModSystemProgressBar>()?.RemoveProgressbar(progressBarRender);
        return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection? entitySel, ref EnumHandling handling)
    {
        GetOrLoadApplicationSound(slot)?.Stop();
        api?.ModLoader.GetModSystem<ModSystemProgressBar>()?.RemoveProgressbar(progressBarRender);

        handling = EnumHandling.Handled;

        if (secondsUsed < GetApplicationTime(slot, byEntity) || byEntity.World.Side != EnumAppSide.Server)
        {
            return;
        }

        Entity targetEntity = GetTargetEntity(slot, byEntity, entitySel);

        EntityBehaviorPlayerRevivable? revivableBehavior = targetEntity.GetBehavior<EntityBehaviorPlayerRevivable>();
        if (revivableBehavior != null && CanRevive(slot) && !targetEntity.Alive)
        {
            revivableBehavior.AttemptRevive();
        }
        else
        {
            DamageSource damageSource = new()
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heal,
                DamageTier = 0,
                Duration = TimeSpan.FromSeconds(GetEffectDurationSec(slot)),
                TicksPerDuration = GetTicks(slot)
            };

            targetEntity.ReceiveDamage(damageSource, GetHealth(slot));
        }

        if (GetAppliedSound(slot) != null)
        {
            byEntity.World.PlaySoundAt(GetAppliedSound(slot), byEntity, null, false, GetSoundRange(slot));
        }

        slot.TakeOut(1);
        slot.MarkDirty();
    }

    public override void GetHeldItemInfo(ItemSlot slot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        dsc.AppendLine(Lang.Get("healing-item-info", $"{GetHealth(slot):F1}", $"{GetEffectDurationSec(slot):F1}", $"{GetApplicationTimeSec(slot):F1}"));
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        return [
            new()
            {
                ActionLangCode = "game:heldhelp-heal",
                MouseButton = EnumMouseButton.Right,
            }
        ];
    }

    public virtual WorldInteraction[] GetHealInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
    {
        return [
            new()
            {
                ActionLangCode = "heldhelp-heal",
                HotKeyCode = "ctrl",
                MouseButton = EnumMouseButton.Right,
            }
        ];
    }

    public virtual bool CanHeal(Entity target) => true;

    protected virtual float GetApplicationTime(ItemSlot slot, Entity byEntity)
    {
        float healingEffectiveness = 0;

        if (AffectedByArmor(slot))
        {
            healingEffectiveness = byEntity.Stats.GetBlended("healingeffectivness");
            healingEffectiveness = Math.Clamp(healingEffectiveness, 0, 2) - 1;
        }

        if (healingEffectiveness < 0)
        {
            return GetApplicationTimeSec(slot) + (GetApplicationTimeSec(slot) - GetMaxApplicationTimeSec(slot)) * healingEffectiveness;
        }

        if (healingEffectiveness > 0)
        {
            return GetApplicationTimeSec(slot) * (1 - healingEffectiveness);
        }

        return GetApplicationTimeSec(slot);
    }

    protected virtual Entity GetTargetEntity(ItemSlot slot, EntityAgent byEntity, EntitySelection? entitySelection)
    {
        Entity targetEntity = byEntity;
        Entity? selectedEntity = entitySelection?.Entity;

        if (selectedEntity == null)
        {
            return targetEntity;
        }

        EntityBehaviorHealth? healthBehavior = selectedEntity.GetBehavior<EntityBehaviorHealth>();

        if (
            byEntity.Controls.CtrlKey &&
            !byEntity.Controls.Forward &&
            !byEntity.Controls.Backward &&
            !byEntity.Controls.Left &&
            !byEntity.Controls.Right &&
            CanHeal(selectedEntity) &&
            healthBehavior != null &&
            healthBehavior.IsHealable(byEntity, slot))
        {
            targetEntity = selectedEntity;
        }

        return targetEntity;
    }

    protected virtual bool CancelApplication(ItemSlot slot, Entity entity) => (!entity.OnGround && !entity.Swimming && CancelInAir(slot)) || (entity.Swimming && CancelWhileSwimming(slot));
}