namespace Vheos.Mods.ForTheKing
{
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.UtilityNS;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;
    using Tools.Extensions.Math.Unity;
    using GridEditor;

    public class Various : AMod
    {
        // Setting
        static private ModSetting<bool> _skipStartup;
        static private ModSetting<bool> _removeDiscardOption;
        static private ModSetting<int> _overrideLorePayout;
        static private ModSetting<int> _overrideInflation;
        static private ModSetting<bool> _smoothInflationProgress;
        static private ModSetting<Vector3> _overridePipePrices;
        override protected void Initialize()
        {
            _skipStartup = CreateSetting(nameof(_skipStartup), false);
            _removeDiscardOption = CreateSetting(nameof(_removeDiscardOption), false);
            _overrideLorePayout = CreateSetting(nameof(_overrideLorePayout), -1, IntRange(-1, 200));
            _overrideInflation = CreateSetting(nameof(_overrideInflation), -1, IntRange(-1, 200));
            _smoothInflationProgress = CreateSetting(nameof(_smoothInflationProgress), false);
            _overridePipePrices = CreateSetting(nameof(_overridePipePrices), -1.ToVector3());
        }
        override protected void SetFormatting()
        {
            _skipStartup.Format("Skip startup");
            _removeDiscardOption.Format("Remove \"Discard\" option");
            _overrideLorePayout.Format("Override lore payout");
            _overrideInflation.Format("Override inflation");
            _smoothInflationProgress.Format("Smooth inflation progress");
            _overridePipePrices.Format("Override pipe prices");
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
        static private void EncounterSessionMC_VoteNextQueue_Post(EncounterSessionMC __instance, ref bool __result)
        {
            #region quit
            if (!_removeDiscardOption || __instance.m_VoteType != EncounterSessionMC.VoteType.Loot)
                return;
            #endregion

            __result = false;
        }

        [HarmonyPatch(typeof(VoteButtonContainer), "_show"), HarmonyPrefix]
        static private void VoteButtonContainer__show_Pre()
        => Google2u.TextMenu.Instance.Rows[121]._en = _removeDiscardOption ? "Pass" : "Discard";
        #endregion

        #region Override lore payout
        [HarmonyPatch(typeof(GameFlow), "GetLoreMultipiler"), HarmonyPostfix]
        static private void GameFlow_GetLoreMultipiler_Post(ref float __result)
        {
            #region quit
            if (_overrideLorePayout == -1)
                return;
            #endregion

            __result = _overrideLorePayout / 100f;
        }
        #endregion

        #region Inflation
        [HarmonyPatch(typeof(FTKUtil), "Price"), HarmonyPrefix]
        static private void FTKUtil_Price_Pre()
        {
            #region quit
            if (_overrideInflation == -1)
                return;
            #endregion

            GameFlow.Instance.m_Rules.m_Inflation = _overrideInflation / 100f;
        }

        [HarmonyPatch(typeof(FTKUtil), "Price"), HarmonyPrefix]
        static private bool FTKUtil_Price_Pre2(ref int __result, int _basecost, float _multiplier, bool _priceScale)
        {
            #region quit
            if (!_smoothInflationProgress)
                return true;
            #endregion

            float inflation = 1f;
            if (_priceScale)
            {
                GameStage gameStage = GameLogic.Instance.GetGameDef().GetGameStage();
                float progress = (float)gameStage.GetCurrentProgressionTier() + gameStage.GetStagePassedPercent();
                inflation = GameFlow.Instance.Inflation.Add(1).Pow(progress);
            }
            __result = (_basecost * inflation * _multiplier).Round();
            return false;
        }
        #endregion

        #region Override pipe prices
        [HarmonyPatch(typeof(FTK_itembase), "GetCost"), HarmonyPrefix]
        static private void FTK_itembase_GetCost_Pre(FTK_itembase __instance)
        {
            int overrideValue = -1;
            switch (FTK_itembase.GetEnum(__instance.m_ID))
            {
                case FTK_itembase.ID.pipe02: overrideValue = _overridePipePrices.Value.x.Round(); break;
                case FTK_itembase.ID.pipe03: overrideValue = _overridePipePrices.Value.y.Round(); break;
                case FTK_itembase.ID.pipe04: overrideValue = _overridePipePrices.Value.z.Round(); break;
            }
            if (overrideValue != -1)
                __instance._goldValue = overrideValue;
        }
        #endregion
    }
}