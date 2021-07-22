namespace Vheos.Mods.ForTheKing
{
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    public class Cheats : AMod
    {
        // Setting
        static private ModSetting<int> _setHealth, _setFocus, _setGold;
        static private ModSetting<SetStatsFrequency> _setStatsFrequency;
        override protected void Initialize()
        {
            _setHealth = CreateSetting(nameof(_setHealth), 200, IntRange(0, 200));
            _setFocus = CreateSetting(nameof(_setFocus), 10, IntRange(0, 10));
            _setGold = CreateSetting(nameof(_setGold), 2000, IntRange(0, 2000));
            _setStatsFrequency = CreateSetting(nameof(_setStatsFrequency), SetStatsFrequency.Never);

            ConfigHelper.AddEventOnConfigClosed(() =>
            {
                if (_setStatsFrequency == SetStatsFrequency.Once
                || _setStatsFrequency == SetStatsFrequency.OnConfigClosed)
                    foreach (var character in FTKHub.Instance.m_CharacterOverworlds)
                    {
                        SetStats(character.m_CharacterStats);
                        if (_setStatsFrequency == SetStatsFrequency.Once)
                            _setStatsFrequency.Value = SetStatsFrequency.Never;
                    }
            });
        }
        override protected void SetFormatting()
        {
            _setStatsFrequency.Format("Set stats");
            Indent++;
            {
                _setHealth.Format("Health", _setStatsFrequency, SetStatsFrequency.Never, false);
                _setFocus.Format("Focus", _setStatsFrequency, SetStatsFrequency.Never, false);
                _setGold.Format("Gold", _setStatsFrequency, SetStatsFrequency.Never, false);
                Indent--;
            }
        }

        // Utility
        #region ENUM
        private enum SetStatsFrequency
        {
            Never = 0,
            Once,
            OnConfigClosed,
            OnUpdate,
        }
        #endregion
        static private void SetStats(CharacterStats stats)
        {
            stats.m_HealthCurrent = _setHealth.Value.ClampMax(stats.MaxHealth);
            stats.m_FocusPoints = _setFocus.Value.ClampMax(stats.MaxFocus);
            stats.m_Gold = _setGold;
        }

        // Hooks
        [HarmonyPatch(typeof(CharacterStats), "Update"), HarmonyPrefix]
        static private void CharacterStats_Update_Pre(CharacterStats __instance)
        {
            if (_setStatsFrequency == SetStatsFrequency.OnUpdate)
                SetStats(__instance);
        }
    }
}