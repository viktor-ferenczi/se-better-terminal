using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyGuiControlListbox))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public static class MyGuiControlListboxPatch
    {
        // Suppress tooltip on the block listbox while the context menu is open
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyGuiControlListbox.ShowToolTip))]
        private static bool ShowToolTipPrefix(MyGuiControlListbox __instance)
        {
            // Skip showing tooltip if the context menu is active for this listbox
            if (MyTerminalControlPanelPatch.ShouldSuppressTooltip(__instance))
                return false;

            return true;
        }
    }
}
