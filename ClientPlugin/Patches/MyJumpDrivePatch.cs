using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyJumpDrive))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyJumpDrivePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateTerminalControls")]
        private static void CreateTerminalControlsPostfix()
        {
            foreach (var control in MyTerminalControlFactory.GetControls(typeof(MyJumpDrive)))
            {
                if (control is not MyTerminalControlListbox<MyJumpDrive> listbox)
                    continue;

                switch (listbox.Id)
                {
                    // Double-click on GPS entry or beacon to select it as target
                    case "GpsList":
                    case "BeaconList":
                        listbox.ItemDoubleClicked = (block, items) => block.SelectTarget();
                        break;

                    // Double-click on selected target to remove it
                    case "SelectedTarget":
                        listbox.ItemDoubleClicked = (block, items) => block.RemoveSelected();
                        break;
                }
            }
        }
    }
}
