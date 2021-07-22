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
    using System.Collections.Generic;

    public class Stats : AMod
    {
        // Setting
        static private ModSetting<Vector4> _maxHealthFormulaCoeffs;
        static private ModSetting<bool> _customDefenseFormulas;
        static private ModSetting<bool> _customFocusAccuracyBonus;
        static private ModSetting<int> _levelUpStats;
        static private ModSetting<int> _levelUpStatsOffset;
        static private ModSetting<bool> _dontModifyLuck;
        static private ModSetting<int> _levelUpDamage;
        static private ModSetting<int> _levelUpDamageOffset;
        static private ModSetting<bool> _dontModifyGlassWeapons;
        override protected void Initialize()
        {
            _maxHealthFormulaCoeffs = CreateSetting(nameof(_maxHealthFormulaCoeffs), new Vector4(24, 1, 10, 1));
            _customDefenseFormulas = CreateSetting(nameof(_customDefenseFormulas), false);
            _customFocusAccuracyBonus = CreateSetting(nameof(_customFocusAccuracyBonus), false);

            _levelUpStats = CreateSetting(nameof(_levelUpStats), 0, IntRange(0, 3));
            _levelUpStatsOffset = CreateSetting(nameof(_levelUpStatsOffset), 0, IntRange(-9, 9));
            _dontModifyLuck = CreateSetting(nameof(_dontModifyLuck), false);

            _levelUpDamage = CreateSetting(nameof(_levelUpDamage), 1, IntRange(0, 3));
            _levelUpDamageOffset = CreateSetting(nameof(_levelUpDamageOffset), 0, IntRange(-9, 9));
            _dontModifyGlassWeapons = CreateSetting(nameof(_dontModifyGlassWeapons), true);

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
            _customFocusAccuracyBonus.Format("Custom focus accuracy bonus");
            _customFocusAccuracyBonus.Description = "Gain 5% Accuracy for all subsequent rolls for every focus point spent";
            _levelUpStats.Format("Level up stats");
            _levelUpStats.Description = "Gain +1 to all stats for every X levels\n" +
                                        "(set to 0 to disable)";
            Indent++;
            {
                _levelUpStatsOffset.Format("level offset", _levelUpStats, () => _levelUpStats != 0);
                _dontModifyLuck.Format("don't modify luck", _levelUpStats, () => _levelUpStats != 0);
                Indent--;
            }
            _levelUpDamage.Format("Level up damage");
            _levelUpDamage.Description = "Gain +1 to all damage for every X levels\n" +
                                         "(set to 0 to disable)";
            Indent++;
            {
                _levelUpDamageOffset.Format("level offset", _levelUpDamage, () => _levelUpDamage != 0);
                _dontModifyGlassWeapons.Format("don't modify glass weapons", _levelUpDamage, () => _levelUpDamage != 0);
                Indent--;
            }

        }
        override protected string SectionOverride
        => ModSections.Rebalance;

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

        #region Custom focus accuracy bonus
        [HarmonyPatch(typeof(CharacterStats), "GetSkillValue"), HarmonyPrefix]
        static private bool CharacterStats_GetSkillValue_Pre(CharacterStats __instance, ref float __result, FTK_weaponStats2.SkillType _skill, bool _focusMod, float _modified)
        {
            #region quit
            if (!_customFocusAccuracyBonus)
                return true;
            #endregion

            float statValue = 0f;
            switch (_skill)
            {
                case FTK_weaponStats2.SkillType.toughness: statValue = __instance.Toughness; break;
                case FTK_weaponStats2.SkillType.fortitude: statValue = __instance.Fortitude; break;
                case FTK_weaponStats2.SkillType.awareness: statValue = __instance.Awareness; break;
                case FTK_weaponStats2.SkillType.vitality: statValue = __instance.Vitality; break;
                case FTK_weaponStats2.SkillType.quickness: statValue = __instance.Quickness; break;
                case FTK_weaponStats2.SkillType.talent: statValue = __instance.Talent; break;
                case FTK_weaponStats2.SkillType.luck: statValue = __instance.Luck; break;
            }

            float accuracyMod = _modified;
            if (_focusMod)
                accuracyMod += __instance.SpentFocus * 5 / 100f;

            __result = (statValue + accuracyMod).Clamp(GameFlow.Instance.m_MinCharacterStat, 1f);
            return false;
        }
        #endregion

        #region Modify stats
        static private void TryLevelUpStat(ref float stat, int level)
        {
            #region quit
            if (_levelUpStats == 0)
                return;
            #endregion

            stat += level.Add(_levelUpStatsOffset).Div(_levelUpStats).RoundDown().Div(100);
            stat = stat.Round(2);
        }

        [HarmonyPatch(typeof(CharacterStats), "RawAwareness", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawAwareness_Post(CharacterStats __instance, ref float __result)
        => TryLevelUpStat(ref __result, __instance.m_PlayerLevel);

        [HarmonyPatch(typeof(CharacterStats), "RawFortitude", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawFortitude_Post(CharacterStats __instance, ref float __result)
        => TryLevelUpStat(ref __result, __instance.m_PlayerLevel);

        [HarmonyPatch(typeof(CharacterStats), "RawQuickness", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawQuickness_Post(CharacterStats __instance, ref float __result)
        => TryLevelUpStat(ref __result, __instance.m_PlayerLevel);

        [HarmonyPatch(typeof(CharacterStats), "RawTalent", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawTalent_Post(CharacterStats __instance, ref float __result)
        => TryLevelUpStat(ref __result, __instance.m_PlayerLevel);

        [HarmonyPatch(typeof(CharacterStats), "RawToughness", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawToughness_Post(CharacterStats __instance, ref float __result)
        => TryLevelUpStat(ref __result, __instance.m_PlayerLevel);

        [HarmonyPatch(typeof(CharacterStats), "RawVitality", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawVitality_Post(CharacterStats __instance, ref float __result)
        => TryLevelUpStat(ref __result, __instance.m_PlayerLevel);

        [HarmonyPatch(typeof(CharacterStats), "RawLuck", MethodType.Getter), HarmonyPostfix]
        static private void CharacterStats_RawLuck_Post(CharacterStats __instance, ref float __result)
        {
            #region quit
            if (_dontModifyLuck)
                return;
            #endregion

            TryLevelUpStat(ref __result, __instance.m_PlayerLevel);
        }
        #endregion

        #region Modify damage
        [HarmonyPatch(typeof(CharacterStats), "GetWeaponMaxDamage"), HarmonyPrefix]
        static private bool CharacterStats_GetWeaponMaxDamage_Pre(CharacterStats __instance, ref int __result, FTK_enemyCombat.EnemyRace[] _againstRaces)
        {
            // Cache
            FTK_weaponStats2 entry = FTK_weaponStats2DB.GetDB().GetEntry(__instance.m_CharacterOverworld.m_WeaponID);
            float damage = 0f;

            // Level
            damage += __instance.m_PlayerLevel.Add(_levelUpDamageOffset).Div(_levelUpDamage).RoundDown();
            // Physical
            if (entry._dmgtype == FTK_weaponStats2.DamageType.magic)
                damage += __instance.m_ModAttackMagic + __instance.m_AugmentedDamageMagic;
            // Magical
            else if (entry._dmgtype == FTK_weaponStats2.DamageType.physical)
                damage += __instance.m_ModAttackPhysical + __instance.m_AugmentedDamagePhysical;
            // Any
            damage += __instance.m_ModAttackAll;
            // Weapon
            damage += entry._maxdmg;

            // Racial
            float raceMod = 0f;
            if (_againstRaces != null)
                foreach (FTK_enemyCombat.EnemyRace key in _againstRaces)
                    if (__instance.m_DamageAgainstBonus.ContainsKey(key))
                        raceMod += __instance.m_DamageAgainstBonus[key];
            damage *= 1f + raceMod;

            // Chaos
            damage *= 1f + GameFlow.Instance.GetChaosAttackDamageMultiplier(-1);

            // ???
            if (__instance.m_IsInCombat)
                damage *= 1f + __instance.m_CharacterOverworld.m_CurrentDummy.AttackDmgMod;

            // Return
            __result = damage.Round();
            return false;
        }
        #endregion
    }
}