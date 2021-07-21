namespace Vheos.Mods.ForTheKing
{
    using System;
    using System.Reflection;
    using BepInEx;
    using Tools.ModdingCore;
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
        override public Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        override public void Initialize()
        {
        }
        override public Type[] ModsOrderingList => new[]
        {
            typeof(Various),
            typeof(Cheats),
            typeof(Debug),
        };
    }
}