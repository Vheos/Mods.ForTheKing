namespace Vheos.Mods.ForTheKing
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using Mods.Core;
    using Tools.Extensions.General;
    using Tools.Extensions.DumpN;
    using Tools.UtilityN;
    using Tools.Extensions.UnityObjects;
    using GridEditor;
    public class Debug : AMod, IUpdatable
    {
        // Setting
        override protected void Initialize()
        {
        }
        override protected void SetFormatting()
        {
        }
        public void OnUpdate()
        {
            if (KeyCode.Keypad0.Pressed())
            {
                Log.Debug($"ID\t{   typeof(FTK_weaponStats2).Dump(null, MemberData.Names) }");
                Log.Debug($"enum\t{  typeof(FTK_weaponStats2).Dump(null, MemberData.Types) }");
                foreach (var itemID in Utility.GetEnumValues<FTK_itembase.ID>())
                    if (FTK_weaponStats2DB.Get(itemID).TryNonNull(out var weaponStats))
                        Log.Debug($"{itemID}\t{weaponStats.Dump(typeof(FTK_weaponStats2))}");
            }
        }
        override protected string SectionOverride
        => ModSections.Development;

        // Hooks
    }
}