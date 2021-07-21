namespace Vheos.Mods.ForTheKing
{
    using HarmonyLib;
    using Tools.ModdingCore;
    public class Cheats : AMod
    {
        // Setting
        static private ModSetting<bool> _alwaysMaxHealth, _alwaysMaxFocus;
        override protected void Initialize()
        {
            _alwaysMaxHealth = CreateSetting(nameof(_alwaysMaxHealth), false);
            _alwaysMaxFocus = CreateSetting(nameof(_alwaysMaxFocus), false);
        }
        override protected void SetFormatting()
        {
            _alwaysMaxHealth.Format("Always max health");
            _alwaysMaxFocus.Format("Always max focus");
        }

        // Hooks
        [HarmonyPatch(typeof(CharacterStats), "Update"), HarmonyPrefix]
        static void CharacterStats_Update_Pre(CharacterStats __instance)
        {
            if (_alwaysMaxHealth)
                __instance.m_HealthCurrent = __instance.MaxHealth;
            if (_alwaysMaxFocus)
                __instance.m_FocusPoints = __instance.MaxFocus;
        }
    }
}