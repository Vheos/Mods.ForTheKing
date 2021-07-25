namespace Vheos.Mods.ForTheKing
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.UtilityNS;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;
    public class Restores : AMod
    {
        // Setting
        static private ModSetting<int> _innHealthGain, _innFocusGain;
        static private ModSetting<bool> _innHealthGainPercentOfMissing;
        static private ModSetting<int> _levelUpHealthGain, _levelUpFocusGain;
        static private ModSetting<int> _reviveHealthGain, _reviveFocusGain;
        static private ModSetting<SanctumRestores> _sanctumRestores;
        override protected void Initialize()
        {
            // Inn
            _innHealthGain = CreateSetting(nameof(_innHealthGain), 70, IntRange(0, 100));
            _innHealthGainPercentOfMissing = CreateSetting(nameof(_innHealthGainPercentOfMissing), false);
            _innFocusGain = CreateSetting(nameof(_innFocusGain), 2, IntRange(0, 10));
            // Level up
            _levelUpHealthGain = CreateSetting(nameof(_levelUpHealthGain), 50, IntRange(0, 100));
            _levelUpFocusGain = CreateSetting(nameof(_levelUpFocusGain), 2, IntRange(0, 10));
            // Revive
            _reviveHealthGain = CreateSetting(nameof(_reviveHealthGain), 33, IntRange(0, 100));
            _reviveFocusGain = CreateSetting(nameof(_reviveFocusGain), 1, IntRange(0, 10));
            // Sanctum
            _sanctumRestores = CreateSetting(nameof(_sanctumRestores), (SanctumRestores)~0);
        }
        override protected void SetFormatting()
        {
            // Inn
            _innHealthGain.Format("Inn health gain");
            _innHealthGain.Description = "How much health (% of max) you get when resting at inn or camp";
            Indent++;
            {
                _innHealthGainPercentOfMissing.Format("is % of missing health");
                _innHealthGainPercentOfMissing.Description = "Use missing health (instead of max) as the coefficient";
                Indent--;
            }
            _innFocusGain.Format("Inn focus gain");
            _innFocusGain.Description = "How many focus points you get when resting at inn or camp";
            // Level up
            _levelUpHealthGain.Format("Level up health gain");
            _levelUpHealthGain.Description = "How much health (% of missing) you get when leveling up\n" +
                                             "Using 0 will still heal for the amount of gained max health\n" +
                                             "\n" +
                                             "Vanilla:\n" +
                                             "50% for Journeyman difficulty\n" +
                                             "75% for Master difficulty";
            _levelUpFocusGain.Format("Level up focus gain");
            _levelUpFocusGain.Description = "How many focus points you get when leveling up";
            // Revive
            _reviveHealthGain.Format("Revive health gain");
            _reviveHealthGain.Description = "How much health (% of max) you get after being revived";
            _reviveFocusGain.Format("Revive focus gain");
            _reviveFocusGain.Description = "How many focus points you get after being revived";
            // Sanctum
            _sanctumRestores.Format("Sanctum restores");
        }
        override protected string SectionOverride
        => ModSections.Rebalance;

        // Logic
#pragma warning disable IDE0051 // Remove unused private members

        #region Inn
        [HarmonyPatch(typeof(CharacterOverworld), "GetInnHealAmount"), HarmonyPostfix]
        static private void CharacterOverworld_GetInnHealAmount_Post(CharacterOverworld __instance, ref int __result)
        {
            GameFlow.Instance.GameDif.m_InnMaxHealthGain = _innHealthGain / 100f;
            GameFlow.Instance.GameDif.m_InnFocusGain = _innFocusGain;

            CharacterStats stats = __instance.m_CharacterStats;
            int healthRef = _innHealthGainPercentOfMissing
                          ? stats.MaxHealth - stats.m_HealthCurrent
                          : stats.MaxHealth;
            __result = (healthRef * GameFlow.Instance.GameDif.m_InnMaxHealthGain).Round();
        }
        #endregion

        #region Level up
        [HarmonyPatch(typeof(CharacterStats), "TallyCharacterDefense"), HarmonyPostfix]
        static private void CharacterStats_TallyCharacterDefense_Post(CharacterStats __instance)
        {
            GameFlow.Instance.GameDif.m_LevelUpHealthDifference = 1 - _levelUpHealthGain / 100f;
            GameFlow.Instance.GameDif.m_LevelUpFocusGain = _levelUpFocusGain;
        }
        #endregion

        #region Revive
        static private void SetReviveGains()
        {
            GameFlow.Instance.GameDif.m_ReviveMaxHealthGain = _reviveHealthGain / 100f;
            GameFlow.Instance.GameDif.m_ReviveFocusGain = _reviveFocusGain;
        }

        [HarmonyPatch(typeof(CharacterOverworld), "RespawnRPC"), HarmonyPrefix]
        static private void CharacterOverworld_RespawnRPC_Pre()
        => SetReviveGains();

        [HarmonyPatch(typeof(CharacterStats), "DioramaSanctumRevive"), HarmonyPrefix]
        static private void CharacterStats_DioramaSanctumRevive_Pre()
        => SetReviveGains();
        #endregion

        #region Sanctum
        [System.Flags]
        private enum SanctumRestores
        {
            Health = 1 << 1,
            Focus = 1 << 2,
            Poison = 1 << 3,
            Disease = 1 << 4,
            Curse = 1 << 5,
        }

        [HarmonyPatch(typeof(CharacterStats), "PlayerFullHeal4"), HarmonyPrefix]
        static private bool CharacterStats_PlayerFullHeal4_Pre(CharacterStats __instance, bool _broadcast)
        {
            if (!Utility.GetStackMethod(3).DeclaringType.Name.Contains("Sanctum"))
                return true;

            if (_sanctumRestores.Value.HasFlag(SanctumRestores.Health))
                __instance.SetSpecificHealth(__instance.MaxHealth, _broadcast);
            if (_sanctumRestores.Value.HasFlag(SanctumRestores.Focus))
                __instance.UpdateFocusPoints(__instance.MaxFocus, _broadcast);
            if (_sanctumRestores.Value.HasFlag(SanctumRestores.Poison))
                __instance.SetPoison(-3, _broadcast, false);
            if (_sanctumRestores.Value.HasFlag(SanctumRestores.Disease))
                __instance.SetDisease(string.Empty, -100, true, _broadcast, false);
            if (_sanctumRestores.Value.HasFlag(SanctumRestores.Curse))
                __instance.RemoveAllActiveCurses(_broadcast);

            return false;
        }
        #endregion
    }
}