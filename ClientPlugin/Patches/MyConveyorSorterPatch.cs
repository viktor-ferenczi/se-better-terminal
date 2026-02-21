using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyConveyorSorter))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyConveyorSorterPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateTerminalControls")]
        private static void CreateTerminalControlsPostfix()
        {
            // Double-click on "Add new filter" list item to add it to active filters
            if (MyConveyorSorter.candidates != null)
            {
                MyConveyorSorter.candidates.ItemDoubleClicked =
                    (MyConveyorSorter block, List<MyGuiControlListbox.Item> items) =>
                    {
                        block.AddToCurrentList();
                    };
            }

            // Double-click on "Active filters" list item to remove it
            if (MyConveyorSorter.currentList != null)
            {
                MyConveyorSorter.currentList.ItemDoubleClicked =
                    (MyConveyorSorter block, List<MyGuiControlListbox.Item> items) =>
                    {
                        block.RemoveFromCurrentList();
                    };
            }
        }
    }
}
