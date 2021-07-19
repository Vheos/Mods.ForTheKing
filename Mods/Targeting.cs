namespace Vheos.Mods.ForTheKing
{
    using Vheos.Tools.ModdingCore;
    public class TestMod : AMod
    {
        // Setting
        static private ModSetting<bool> _testBool;
        static private ModSetting<float> _testFloat;
        override protected void Initialize()
        {
            _testBool = CreateSetting(nameof(_testBool), false);
            _testFloat = CreateSetting(nameof(_testFloat), 0f, FloatRange(0f, 100f));
        }
        override protected void SetFormatting()
        {
            _testBool.Format("Test bool");
            _testFloat.Format("Test float", _testBool);
        }
        override protected string Description
        => "Description test";
    }
}