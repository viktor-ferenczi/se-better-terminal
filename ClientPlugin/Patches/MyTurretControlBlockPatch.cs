using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Game.Gui;
using SpaceEngineers.Game.Entities.Blocks;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyTurretControlBlock))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyTurretControlBlockPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateTerminalControls")]
        private static void CreateTerminalControlsPostfix()
        {
            foreach (var control in MyTerminalControlFactory.GetControls(typeof(MyTurretControlBlock)))
            {
                if (control is not MyTerminalControlListbox<MyTurretControlBlock> listbox)
                    continue;

                switch (listbox.Id)
                {
                    // Double-click on available tool to add it
                    case "ToolList":
                        listbox.ItemDoubleClicked = (block, items) => block.SendToolSelectionPressed();
                        break;

                    // Double-click on selected tool to remove it
                    case "SelectedToolsList":
                        listbox.ItemDoubleClicked = (block, items) => block.SendToolUnselectionPressed();
                        break;
                }
            }
        }
    }
}
