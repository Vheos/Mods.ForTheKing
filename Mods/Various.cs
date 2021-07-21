namespace Vheos.Mods.ForTheKing
{
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    public class Various : AMod
    {
        // Setting
        static private ModSetting<bool> _skipStartup;
        override protected void Initialize()
        {
            _skipStartup = CreateSetting(nameof(_skipStartup), false);
        }
        override protected void SetFormatting()
        {
            _skipStartup.Format("Skip startup");
        }

        // Hooks
        [HarmonyPatch(typeof(SplashScreen), "GetAnyButton"), HarmonyPostfix]
        static void SplashScreen_GetAnyButton_Post(ref bool __result)
        {
            #region quit
            if (!_skipStartup)
                return;
            #endregion

            __result = true;
        }

        [HarmonyPatch(typeof(uiStartGame), "Update"), HarmonyPostfix]
        static void uiStartGame_Update_Post(uiStartGame __instance)
        {
            #region quit
            if (!_skipStartup)
                return;
            #endregion

            if (__instance.m_PrepareToDie.gameObject.activeSelf)
                __instance.m_PrepareToDie.OnButton();
        }

    }
}