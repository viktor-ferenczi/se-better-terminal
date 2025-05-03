using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Extensions
{
    public static class MyGuiScreenTerminalAccessors
    {
        private static readonly AccessTools.FieldRef<MyGuiScreenTerminal, MyGuiControlButton> _groupSave =
            AccessTools.FieldRefAccess<MyGuiScreenTerminal, MyGuiControlButton>("m_groupSave");

        private static readonly AccessTools.FieldRef<MyGuiScreenTerminal, MyGuiControlButton> _groupDelete =
            AccessTools.FieldRefAccess<MyGuiScreenTerminal, MyGuiControlButton>("m_groupDelete");

        private static readonly AccessTools.FieldRef<MyGuiScreenTerminal, MyGuiControlTextbox> _groupName =
            AccessTools.FieldRefAccess<MyGuiScreenTerminal, MyGuiControlTextbox>("m_groupName");

        public static MyGuiControlButton GetGroupSave(this MyGuiScreenTerminal terminal) => _groupSave(terminal);
        public static MyGuiControlButton GetGroupDelete(this MyGuiScreenTerminal terminal) => _groupDelete(terminal);
        public static MyGuiControlTextbox GetGroupName(this MyGuiScreenTerminal terminal) => _groupName(terminal);
    }
}