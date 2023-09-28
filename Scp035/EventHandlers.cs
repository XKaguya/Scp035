namespace Scp035
{
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.EventArgs.Server;
    using Exiled.Events.EventArgs.Warhead;
    using GameCore;
    using MEC;
    using PlayerRoles;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Exiled.CustomRoles.API.Features;

    /// <summary>
    /// Handles general events for this plugin.
    /// </summary>
    public class EventHandlers
    {
        private readonly Plugin _plugin;

        internal EventHandlers(Plugin plugin) => this._plugin = plugin;

        internal void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
        {
            if (_plugin.StopRagdollsList.Contains(ev.Player))
                ev.IsAllowed = false;
        }

        public class HurtManager
        {
            private static HurtManager instance = null;

            public List<Player> PlayersHurt { get; set; }

            private HurtManager()
            {
                PlayersHurt = new List<Player>();
            }

            public static HurtManager Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new HurtManager();
                    }
                    return instance;
                }
            }
        }

        public HurtManager manager = HurtManager.Instance;
        Scp035Role scp035RoleInstance = new Scp035Role();

        public void InitializeScp035Role()
        {
            if (scp035RoleInstance == null)
            {
                Exiled.API.Features.Log.Error("Scp035Role is not initialized.");
                return;
            }

            int roleVarValue = scp035RoleInstance.RoleVar;
        }

        private bool isHealing = false;
        private float lastHurtTime = 0f;
        private float healingInterval = 3f;
        public float HealAmount { get; set; } = 10f;

        public void OnHurt_Heal(HurtingEventArgs ev)
        {
            if (ev == null || ev.Player == null)
            {
                Exiled.API.Features.Log.Debug("Invalid event or player.");
                return;
            }

            if (ev.Amount > 0)
            {
                if (!manager.PlayersHurt.Contains(ev.Player))
                {
                    manager.PlayersHurt.Add(ev.Player);
                    Exiled.API.Features.Log.Debug("Player is hurt, adding to healing list.");
                }
                else
                {
                    Exiled.API.Features.Log.Debug("Player already in list.");
                }

                lastHurtTime = Time.time;

                if (!isHealing)
                {
                    isHealing = true;
                    Timing.RunCoroutine(StartContinuousHeal());
                }
            }
        }

        private IEnumerator<float> StartContinuousHeal()
        {
            while (isHealing)
            {
                float timeSinceLastHurt = Time.time - lastHurtTime;

                if (timeSinceLastHurt >= 3f)
                {
                    List<Player> playersToHeal = manager.PlayersHurt.ToList();

                    foreach (var player in playersToHeal)
                    {
                        if (player != null && player.Role.Type == RoleTypeId.Tutorial)
                        {
                            float healAmount = HealAmount;
                            int missingHealth = Mathf.FloorToInt(player.MaxHealth) - Mathf.FloorToInt(player.Health);

                            if (missingHealth > 0)
                            {
                                int healAmountInt = Mathf.FloorToInt(healAmount);
                                if (healAmountInt > missingHealth)
                                {
                                    healAmountInt = missingHealth;
                                }

                                player.Heal(healAmountInt);
                                Exiled.API.Features.Log.Debug($"Player healed for {healAmountInt} HP. Remaining missing health: {missingHealth - healAmountInt}");
                            }
                            else
                            {
                                manager.PlayersHurt.Remove(player);
                            }
                        }
                    }

                    if (manager.PlayersHurt.Count == 0)
                    {
                        isHealing = false;
                        yield break;
                    }

                    lastHurtTime = Time.time;
                }

                yield return 0;
            }
        }

        internal void OnEndingRound(EndingRoundEventArgs ev)
        {
            bool human = false;
            bool scps = false;
            CustomRole role = CustomRole.Get(typeof(Scp035Role));

            if (role == null)
            {
                Exiled.API.Features.Log.Debug($"{nameof(OnEndingRound)}: Custom role is null, returning.");
                return;
            }

            foreach (Player player in Player.List)
            {
                if (player == null)
                {
                    Exiled.API.Features.Log.Debug($"{nameof(OnEndingRound)}: Skipping a null player.");
                    continue;
                }

                if (role.Check(player) || player.Role.Side == Side.Scp)
                {
                    Exiled.API.Features.Log.Debug($"{nameof(OnEndingRound)}: Found an SCP player.");
                    scps = true;
                }
                else if (player.Role.Side == Side.Mtf || player.Role == RoleTypeId.ClassD)
                {
                    Exiled.API.Features.Log.Debug($"{nameof(OnEndingRound)}: Found a Human player.");
                    human = true;
                }

                if (scps && human)
                {
                    Exiled.API.Features.Log.Debug($"{nameof(OnEndingRound)}: Both humans and scps detected.");
                    break;
                }
            }

            Exiled.API.Features.Log.Debug($"{nameof(OnEndingRound)}: Should event be blocked: {(human && scps)} -- Should round end: {(human && scps)}");
            if (human && scps)
            {
                ev.IsRoundEnded = false;
            }
        }
    }
}
