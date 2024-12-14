using System.Runtime.CompilerServices;
using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

[assembly: IgnoresAccessChecksTo("Sandbox.Game")]
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

            var showAll = (MyGuiControlButton)page.Controls.GetControlByName("ShowAll");
            var searchBox = (MyGuiControlSearchBox)page.Controls.GetControlByName("FunctionalBlockSearch");

            MyGuiControlCheckbox showDefaultNames = new MyGuiControlCheckbox( showAll.Position + new Vector2(-0.0008f, 0.046f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
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