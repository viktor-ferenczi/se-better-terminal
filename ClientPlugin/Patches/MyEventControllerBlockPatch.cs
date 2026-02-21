using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Game.Gui;
using SpaceEngineers.Game.Entities.Blocks;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyEventControllerBlock))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyEventControllerBlockPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateTerminalControls")]
        private static void CreateTerminalControlsPostfix()
        {
            foreach (var control in MyTerminalControlFactory.GetControls(typeof(MyEventControllerBlock)))
            {
                if (control is not MyTerminalControlListbox<MyEventControllerBlock> listbox)
                    continue;

                switch (listbox.Id)
                {
                    // Double-click on available block to add it
                    case "AvailableBlocks":
                        listbox.ItemDoubleClicked = (block, items) => block.SelectButton();
                        break;

                    // Double-click on selected block to remove it
                    case "SelectedBlocks":
                        listbox.ItemDoubleClicked = (block, items) => block.RemoveButton();
                        break;
                }
            }
        }
    }
}
