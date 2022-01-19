namespace Vheos.Mods.ForTheKing
{
    using System;
    using System.Reflection;
    using BepInEx;
    using Mods.Core;
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BepInExEntryPoint
    {
        #region SETTINGS
        public const string GUID = "Vheos.Mods.ForTheKing";
        public const string NAME = "Vheos Mod Pack";
        public const string VERSION = "0.1.0";
        #endregion

        // User logic
        override protected Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        override protected void Initialize()
        {
        }
        override protected Type[] ModsOrderingList => new[]
        {
            typeof(Various),
            typeof(Stats),
            typeof(Restores),
            typeof(Cheats),
            typeof(Debug),
        };
    }
}