using System.Collections.Generic;
using System;
using Exiled.Events.EventArgs.Player;

namespace Scp035
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CustomPlayerEffects;
    using Exiled.API.Enums;
    using Exiled.API.Extensions;
    using Exiled.API.Features;
    using Exiled.API.Features.Attributes;
    using Exiled.API.Features.Doors;
    using Exiled.API.Features.Items;
    using Exiled.API.Features.Pickups;
    using Exiled.API.Features.Roles;
    using Exiled.API.Features.Spawn;
    using Exiled.CustomItems.API.Features;
    using Exiled.CustomRoles.API.Features;
    using Exiled.Events;
    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.EventArgs.Scp096;
    using MEC;
    using PlayerRoles;
    using PlayerRoles.RoleHelp;
    using PlayerStatsSystem;
    using PluginAPI.Roles;
    using UnityEngine;
    using VoiceChat;
    using YamlDotNet.Serialization;
    using static Scp035.EventHandlers;
    using RoleTypeId = PlayerRoles.RoleTypeId;


    /// <summary>
    /// The <see cref="CustomRole"/> handler for SCP-035.
    /// </summary>
    [CustomRole(RoleTypeId.Tutorial)]
    public class Scp035Role : CustomRole
    {
        /// <inheritdoc />
        public override uint Id { get; set; } = 51;

        /// <summary>
        /// Gets or sets the role that is visible to players on the server aside from the player playing this role.
        /// </summary>
        public RoleTypeId VisibleRole { get; set; } = RoleTypeId.Scp049;
        public RoleTypeId VisibleRole_ClassD { get; set; } = RoleTypeId.ClassD;
        public RoleTypeId VisibleRole_Scientist { get; set; } = RoleTypeId.Scientist;
        public RoleTypeId VisibleRole_FacilityGuard { get; set; } = RoleTypeId.FacilityGuard;

        /// <inheritdoc />
        public override int MaxHealth { get; set; } = 500;

        /// <inheritdoc />
        public override string Name { get; set; } = "SCP-035";

        /// <inheritdoc />
        public override string Description { get; set; } =
            "An SCP who slowly corrodes over time, but is able to use items normally.";

        /// <inheritdoc />
        public override string CustomInfo { get; set; } = "D级人员";

        /// <inheritdoc />
        public override bool KeepInventoryOnSpawn { get; set; } = true;

        /// <summary>
        /// Gets or sets a multiplier used to modify the player's movement speed (running and walking).
        /// </summary>
        public byte MovementMultiplier { get; set; } = 1;

        /// <summary>
        /// Gets a list of item names that the player is unable to pickup while playing this role.
        /// </summary>
        public List<string> BlacklistedItems { get; set; } = new()
        {
            "SR-119",
            "GL-119",
            "SCP-2818",
            ItemType.MicroHID.ToString(),
        };

        /// <summary>
        /// Gets or sets how much damage per tick (1second) the player will take.
        /// </summary>
        public float DamagePerTick { get; set; } = 5f;

        /// <summary>
        /// Gets or sets the custom scale factor for players when they are this role.
        /// </summary>
        public override Vector3 Scale { get; set; } = new(1f, 1f, 1f);

        // The following properties are only defined so that we can add the YamlIgnore attribute to them so they cannot be changed via configs.
        /// <inheritdoc />
        [YamlIgnore]
        public override RoleTypeId Role { get; set; } = RoleTypeId.Tutorial;

        /// <inheritdoc />
        [YamlIgnore]
        public override List<CustomAbility> CustomAbilities { get; set; } = new();

        /// <inheritdoc />
        [YamlIgnore]
        public override SpawnProperties SpawnProperties { get; set; } = null;


        /// <inheritdoc />
        /// Hacky override to bypass bug in Exiled.CustomRoles

        public int RoleVar;
        public override void AddRole(Player player)
        {
            // Test
            if (player.Role.Type == RoleTypeId.FacilityGuard)
            {
                RoleVar = 1;
                Log.Info("Pre-Role: FacilityGuard");
                Log.Info("RoleVar:" + RoleVar);
            }

            else if (player.Role.Type == RoleTypeId.Scientist)
            {
                RoleVar = 2;
                Log.Info("Pre-Role: Scientist");
                Log.Info("RoleVar:" + RoleVar);
            }

            else if (player.Role.Type == RoleTypeId.ClassD)
            {
                RoleVar = 3;
                Log.Info("Pre-Role: ClassD");
                Log.Info("RoleVar:" + RoleVar);
            }

            Vector3 oldPos = player.Position;
            Log.Debug(this.Name + ": Adding role to " + player.Nickname + ".");
            if (this.Role != RoleTypeId.None)
                player.Role.Set(this.Role, RoleSpawnFlags.None);

            Timing.CallDelayed(1.5f, (System.Action)(() =>
            {
                Vector3 spawnPosition = this.GetSpawnPosition();
                Log.Debug(string.Format("{0}: Found {1} to spawn {2}", (object)nameof(AddRole), (object)spawnPosition,
                    (object)player.Nickname));
                player.Position = oldPos;
                if (spawnPosition != Vector3.zero)
                {
                    Log.Debug("AddRole: Setting " + player.Nickname + " position..");
                    player.Position = spawnPosition + Vector3.up * 1.5f;
                }

                if (!this.KeepInventoryOnSpawn)
                {
                    Log.Debug(this.Name + ": Clearing " + player.Nickname + "'s inventory.");
                    player.ClearInventory();
                }

                foreach (string itemName in this.Inventory)
                {
                    Log.Debug(this.Name + ": Adding " + itemName + " to inventory.");
                    this.TryAddItem(player, itemName);
                }

                foreach (AmmoType key in this.Ammo.Keys)
                {
                    Log.Debug(string.Format("{0}: Adding {1} {2} to inventory.", (object)this.Name,
                        (object)this.Ammo[key], (object)key));
                    player.SetAmmo(key, this.Ammo[key]);
                }

                Log.Debug(this.Name + ": Setting health values.");
                player.Health = (float)this.MaxHealth;
                player.MaxHealth = (float)this.MaxHealth;
                player.Scale = this.Scale;
            }));
            Log.Debug(this.Name + ": Setting player info");



            //player.CustomInfo = this.CustomInfo;
            player.CustomInfo = this.CustomInfo;
            Log.Info("CustomInfo:" + this.CustomInfo);


            player.InfoArea &= ~PlayerInfoArea.Role;
            if (this.CustomAbilities != null)
            {
                foreach (CustomAbility customAbility in this.CustomAbilities)
                    customAbility.AddAbility(player);
            }

            this.ShowMessage(player);
            this.RoleAdded(player);
            this.TrackedPlayers.Add(player);
            player.UniqueRole = this.Name;
            player.TryAddCustomRoleFriendlyFire(this.Name, this.CustomRoleFFMultiplier);
        }


        /// <inheritdoc />
        protected override void RoleAdded(Player player)
        {
            Timing.CallDelayed(1.5f, () =>
            {
                // Test
                if (RoleVar == 1)
                    player.ChangeAppearance(VisibleRole_FacilityGuard, true);
                else if (RoleVar == 2)
                    player.ChangeAppearance(VisibleRole_Scientist, true);
                else if (RoleVar == 3)
                    player.ChangeAppearance(VisibleRole_ClassD, true);


                if (MovementMultiplier > 0)
                {
                    StatusEffectBase? movement = player.GetEffect(EffectType.MovementBoost);
                    movement.Intensity = MovementMultiplier;
                }

                player.IsGodModeEnabled = false;
            });

            player.Scale = Scale;
            Log.Info("Before SCP-035 Voice Channel:" + player.VoiceChannel);
            player.VoiceChannel = VoiceChatChannel.ScpChat;
            Log.Info("After SCP-035 Voice Channel:" + player.VoiceChannel);

            Log.Info("Role:" + player.Role.Type);

            // Get player current role

            foreach (Item item in player.Items.ToList())
                if (CustomItem.TryGet(item, out CustomItem customItem))
                {
                    customItem.Spawn(player.Position, item);
                    player.RemoveItem(item);
                }

            Timing.RunCoroutine(Appearance(player), $"{player.UserId}-appearance");
            //Timing.RunCoroutine(Corrosion(player), $"{player.UserId}-corrosion");
            //Timing.RunCoroutine(Heal(player), $"{player.UserId}-heal per tick");

            base.RoleAdded(player);
        }

        /// <inheritdoc />
        protected override void RoleRemoved(Player player)
        {
            Timing.KillCoroutines($"{player.UserId}-appearance");
            Timing.KillCoroutines($"{player.UserId}-corrosion");
            player.Scale = Vector3.one;
            Scp035Item.ChangedPlayers.Remove(player);

            base.RoleRemoved(player);
            RoleVar = 0;
        }

        /// <inheritdoc />
        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
            base.SubscribeEvents();
        }

        /// <inheritdoc />
        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
            base.UnsubscribeEvents();
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (Check(ev.Player))
                Plugin.Instance.StopRagdollsList.Add(ev.Player);
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker != null && Check(ev.Attacker) && ev.Player.Role.Side == Side.Scp)
                ev.IsAllowed = Server.FriendlyFire || ev.Attacker.IsFriendlyFireEnabled;
            if (ev.Attacker != null && ev.Player.Role.Type == RoleTypeId.Tutorial && ev.Attacker.IsScp == true)
                ev.IsAllowed = Server.FriendlyFire || ev.Attacker.IsFriendlyFireEnabled;

        }

        private void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (Check(ev.Player) && CheckItem(ev.Pickup))
                ev.IsAllowed = false;
        }



        private bool CheckItem(Pickup pickup)
        {
            return CustomItem.TryGet(pickup, out CustomItem customItem) && BlacklistedItems.Contains(customItem.Name) ||
                   BlacklistedItems.Contains(pickup.Type.ToString());
        }

        private IEnumerator<float> Appearance(Player player)
        {
            for (; ; )
            {
                yield return Timing.WaitForSeconds(20f);

                if (RoleVar == 1)
                {
                    player.ChangeAppearance(VisibleRole_FacilityGuard, true);
                    player.CustomInfo = $"<color=#A0A0A0>{player.Nickname}\n设施警卫</color>";
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Nickname;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Role;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.PowerStatus;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.UnitName;
                }


                else if (RoleVar == 2)
                {
                    player.ChangeAppearance(VisibleRole_Scientist, true);
                    player.CustomInfo = $"<color=#FAFF86>{player.Nickname}\n科学家</color>";
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Nickname;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Role;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.PowerStatus;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.UnitName;
                }


                else if (RoleVar == 3)
                {
                    player.ChangeAppearance(VisibleRole_ClassD, true);
                    player.CustomInfo = $"<color=#FF9966>{player.Nickname}\nD级人员</color>";
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Nickname;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Role;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.PowerStatus;
                    player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.UnitName;
                }
            }
        }

        private IEnumerator<float> Corrosion(Player player)
        {
            for (; ; )
            {
                yield return Timing.WaitForSeconds(1f);
                player.Hurt(new UniversalDamageHandler(DamagePerTick, DeathTranslations.Poisoned));
            }
        }

        public int PlayerRoleVar
        {
            get { return RoleVar; }
            set { RoleVar = value; }
        }

/**        private IEnumerator<float> Heal(Player player)
        {
            for (; ; )
            {
                if (player != null && manager.PlayersHurt.Contains(player))
                {
                    if (RoleVar == 1 || RoleVar == 2 || RoleVar == 3)
                    {
                        yield return Timing.WaitForSeconds(3f);
                        player.Heal(10);
                        Log.Info($"{player} Healed");
                    }
                }
                else
                {
                    yield return 0;
                }
            }
        } **/
    }
}

