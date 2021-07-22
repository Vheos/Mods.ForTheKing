namespace Vheos.Mods.ForTheKing
{
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.UtilityNS;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;
    using Tools.Extensions.Math.Unity;
    public class Stats : AMod
    {
        // Setting
        static private ModSetting<Vector4> _maxHealthFormulaCoeffs;
        static private ModSetting<bool> _customDefenseFormulas;
        override protected void Initialize()
        {
            _maxHealthFormulaCoeffs = CreateSetting(nameof(_maxHealthFormulaCoeffs), new Vector4(24, 1, 10, 1));
            _customDefenseFormulas = CreateSetting(nameof(_customDefenseFormulas), false);

            // Events
            _maxHealthFormulaCoeffs.AddEvent(() => _maxHealthFormulaCoeffs.SetSilently(_maxHealthFormulaCoeffs.Value.RoundDown()));
        }
        override protected void SetFormatting()
        {
            _maxHealthFormulaCoeffs.Format("Max health formula coefficients");
            _maxHealthFormulaCoeffs.Description = "X   -   base value\n" +
                                                  "Y   -   fixed increase per level\n" +
                                                  "Z   -   % of vitality per level\n" +
                                                  "W   -   levels count offset";
            _customDefenseFormulas.Format("Custom defense formulas");
            _customDefenseFormulas.Description = "Gain 1 Armor for every 10 points of Strength above 50\n" +
                                                 "Gain 1 Resistance for every 10 points of Intelligence above 50\n" +
                                                 "Gain 1 Evasion for every 2 points of Speed above 50";
        }

        // Logic
#pragma warning disable IDE0051 // Remove unused private members

        #region Custom max health
        [HarmonyPatch(typeof(CharacterStats), "TallyCharacterHealth"), HarmonyPrefix]
        static private bool CharacterStats_TallyCharacterHealth_Post(CharacterStats __instance, int _level, bool _updateHud, bool _byPassUiTransition)
        {
            Vector4 coeffs = _maxHealthFormulaCoeffs;
            GameFlow.Instance.m_CharacterHpBaseValue = coeffs.x.Round();

            int level = _level + coeffs.w.Round();
            __instance.m_BaseMaxHealth = (GameFlow.Instance.m_CharacterHpBaseValue + level * (coeffs.y + coeffs.z * __instance.Vitality)).Round();
            __instance.m_HealthCurrent = __instance.m_HealthCurrent.ClampMax(__instance.MaxHealth);

            if (_updateHud)
                __instance.m_CharacterOverworld.m_UIPlayMainHud.SetHealthDisplay(__instance.m_HealthCurrent, __instance.MaxHealth, _byPassUiTransition);

            return false;
        }
        #endregion

        #region Custom defense
        [HarmonyPatch(typeof(CharacterStats), "TallyCharacterDefense"), HarmonyPostfix]
        static private void CharacterStats_TallyCharacterDefense_Post2(CharacterStats __instance)
        {
            #region quit
            if (!_customDefenseFormulas)
                return;
            #endregion

            __instance.m_BaseDefensePhysical = __instance.Toughness.Mul(100).Sub(50).Mul(0.1f).RoundDown().ClampMin(0);
            __instance.m_BaseDefenseMagic = __instance.Fortitude.Mul(100).Sub(50).Mul(0.1f).RoundDown().ClampMin(0);
            __instance.m_BaseEvadeRating = __instance.Quickness.Mul(100).Sub(50).Div(2).RoundDown().ClampMin(0).Div(100);
        }
        #endregion
    }
}