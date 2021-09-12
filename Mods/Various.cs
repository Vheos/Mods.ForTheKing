namespace Vheos.Mods.ForTheKing
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.UtilityN;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;
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
        static private ModSetting<bool> _equipCostsAction;
        static private ModSetting<FreeEquipSpots> _freeEquipSpots;
        static private ModSetting<bool> _equipRequiresActiveTurn;
        static private ModSetting<bool> _freeLoreShop;
        override protected void Initialize()
        {
            _skipStartup = CreateSetting(nameof(_skipStartup), false);
            _removeDiscardOption = CreateSetting(nameof(_removeDiscardOption), false);
            _overrideLorePayout = CreateSetting(nameof(_overrideLorePayout), -1, IntRange(-1, 200));
            _overrideInflation = CreateSetting(nameof(_overrideInflation), -1, IntRange(-1, 200));
            _smoothInflationProgress = CreateSetting(nameof(_smoothInflationProgress), false);
            _overridePipePrices = CreateSetting(nameof(_overridePipePrices), -1.ToVector3());
            _equipCostsAction = CreateSetting(nameof(_equipCostsAction), false);
            _freeEquipSpots = CreateSetting(nameof(_freeEquipSpots), (FreeEquipSpots)0);
            _equipRequiresActiveTurn = CreateSetting(nameof(_equipRequiresActiveTurn), false);
            _freeLoreShop = CreateSetting(nameof(_freeLoreShop), false);
        }
        override protected void SetFormatting()
        {
            _skipStartup.Format("Skip startup");
            _removeDiscardOption.Format("Remove \"Discard\" option");
            _overrideLorePayout.Format("Override lore payout");
            _overrideInflation.Format("Override inflation");
            _smoothInflationProgress.Format("Smooth inflation progress");
            _overridePipePrices.Format("Override pipe prices");
            _equipCostsAction.Format("\"Equip\" costs action");
            Indent++;
            {
                _freeEquipSpots.Format("free spots", _equipCostsAction);
                _equipRequiresActiveTurn.Format("requires active turn", _equipCostsAction);
                Indent--;
            }
            _freeLoreShop.Format("Free lore shop");
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

        #region Equip cost
        [System.Flags]
        private enum FreeEquipSpots
        {
            Town = 1 << 1,
            Camp = 1 << 2,
            Dungeon = 1 << 3,
        }
        static bool HasAnyActionPoints(CharacterOverworld character)
        => character.m_CharacterStats.m_ActionPoints > 0;
        static bool CheckActiveTurnRequirement(CharacterOverworld character)
        => !_equipRequiresActiveTurn || character.m_CharacterStats.m_IsMyTurn;
        static bool IsInFreeEquipSpot(CharacterOverworld character)
        => _freeEquipSpots.Value.HasFlag(FreeEquipSpots.Town) && character.IsInTown()
        || _freeEquipSpots.Value.HasFlag(FreeEquipSpots.Camp) && character.IsInSafeCamp()
        || _freeEquipSpots.Value.HasFlag(FreeEquipSpots.Dungeon) && character.IsInDungeon();
        [HarmonyPatch(typeof(uiPopupMenu), "ShowButton"), HarmonyPostfix]
        static private void uiPopupMenu_ShowButton_Post(uiPopupMenu __instance, uiPopupMenu.Action _a, bool _interactable)
        {
            #region quit
            if (!_equipCostsAction || _a != uiPopupMenu.Action.Equip || !_interactable)
                return;
            #endregion

            uiPopupMenuButton button = __instance.m_Buttons[_a][0];
            bool isInFreeEquipSpot = IsInFreeEquipSpot(__instance.m_Cow);
            if (CheckActiveTurnRequirement(__instance.m_Cow)
            && (HasAnyActionPoints(__instance.m_Cow) || isInFreeEquipSpot))
            {
                if (!isInFreeEquipSpot)
                    button.m_Text.color = Color.red;
                return;
            }
            button.SetEnable(false);
        }

        [HarmonyPatch(typeof(uiPopupMenu), "ActionEquip"), HarmonyPostfix]
        static private void uiPopupMenu_ActionEquip_Post(uiPopupMenu __instance)
        {
            #region quit
            if (!_equipCostsAction || IsInFreeEquipSpot(__instance.m_Cow))
                return;
            #endregion

            __instance.m_Cow.UpdatePlayerAction(-1);
        }
        #endregion

        #region Free lore shop
        [HarmonyPatch(typeof(FTK_loreItem), "GetLoreCost"), HarmonyPostfix]
        static private void FTK_loreItem_CanAfford_Post(ref int __result)
        {
            #region quit
            if (!_freeLoreShop)
                return;
            #endregion

            __result = 0;
        }
        #endregion
    }
}