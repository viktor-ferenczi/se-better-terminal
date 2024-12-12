using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyGuiScreenTerminal))]
    public static class MyGuiScreenTerminalPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateControlPanelPageControls")]
        private static void CreateControlPanelPageControlsPostfix(MyGuiControlTabPage page)
        {
            var labelToHide = (MyGuiControlLabel)page.Controls.GetControlByName("ControlLabel");
            var panelToHide = page.Controls[page.Controls.IndexOf(labelToHide) - 1];

            labelToHide.Visible = false;
            panelToHide.Visible = false;

            var showAll = (MyGuiControlLabel)page.Controls.GetControlByName("ShowAll");
            var searchBox = (MyGuiControlLabel)page.Controls.GetControlByName("FunctionalBlockSearch");

            MyGuiControlCheckbox showDefaultNames = new MyGuiControlCheckbox(showAll.Position + new Vector2(-0.0008f, 0.046f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                Name = "ShowDefaultNames"
            };
            page.Controls.Add(showDefaultNames);

            MyGuiControlCombobox modeSelector = new MyGuiControlCombobox(searchBox.Position + new Vector2(0f, 0.05f), searchBox.Size, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, isAutoscaleEnabled: true)
            {
                Name = "ModeSelector"
            };
            page.Controls.Add(modeSelector);
        }
    }
}