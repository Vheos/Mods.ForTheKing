namespace Vheos.Mods.ForTheKing
{
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.UtilityNS;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;
    using Tools.Extensions.Math.Unity;
    public class Various : AMod
    {
        // Setting
        static private ModSetting<bool> _skipStartup;
        static private ModSetting<bool> _removeDiscardOption;
        override protected void Initialize()
        {
            _skipStartup = CreateSetting(nameof(_skipStartup), false);
            _removeDiscardOption = CreateSetting(nameof(_removeDiscardOption), false);
        }
        override protected void SetFormatting()
        {
            _skipStartup.Format("Skip startup");
            _removeDiscardOption.Format("Remove \"Discard\" option");
        }

        // Logic
#pragma warning disable IDE0051 // Remove unused private members

        #region Skip startup
        [HarmonyPatch(typeof(SplashScreen), "GetAnyButton"), HarmonyPostfix]
        static private void SplashScreen_GetAnyButton_Post(ref bool __result)
        {
            #region quit
            if (!_skipStartup)
                return;
            #endregion

            __result = true;
        }

        [HarmonyPatch(typeof(uiStartGame), "Update"), HarmonyPostfix]
        static private void uiStartGame_Update_Post(uiStartGame __instance)
        {
            #region quit
            if (!_skipStartup)
                return;
            #endregion

            if (__instance.m_PrepareToDie.gameObject.activeSelf)
                __instance.m_PrepareToDie.OnButton();
        }
        #endregion

        #region Remove "Discard"
        [HarmonyPatch(typeof(EncounterSessionMC), "VoteNextQueue"), HarmonyPostfix]
        static private void EncounterSessionMC_VoteNextQueue_Post(ref bool __result)
        {
            #region quit
            if (!_removeDiscardOption)
                return;
            #endregion

            __result = false;
        }

        [HarmonyPatch(typeof(VoteButtonContainer), "_show"), HarmonyPrefix]
        static private void VoteButtonContainer__show_Pre()
        => Google2u.TextMenu.Instance.Rows[121]._en = _removeDiscardOption ? "Pass" : "Discard";
        #endregion
    }
}