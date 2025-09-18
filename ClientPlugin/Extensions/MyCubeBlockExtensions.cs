using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using SpaceEngineers.Game.Entities.Blocks;

namespace ClientPlugin.Extensions
{
    public static class MyCubeBlockExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSafeName(this MyCubeBlock block)
        {
            return block?.DisplayNameText ?? block?.DisplayName ?? block?.Name ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDebugName(this MyCubeBlock block)
        {
            return $"{block.GetSafeName()} [{block.EntityId}]";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MyToolbar GetToolbar(this MyTerminalBlock block)
        {
            switch (block)
            {
                case MySensorBlock b:
                    return b.Toolbar;
                case MyButtonPanel b:
                    return b.Toolbar;
                case MyEventControllerBlock b:
                    return b.Toolbar;
                case MyFlightMovementBlock b:
                    return b.Toolbar;
                case MyShipController b:
                    return b.Toolbar;
                case MyTimerBlock b:
                    return b.Toolbar;
                case MyDefensiveCombatBlock b:
                    return b.GetWaypointActionsToolbar();
            }

            return null;
        }

        private static readonly FieldInfo WaypointActionsToolbarField = AccessTools.DeclaredField(typeof(MyDefensiveCombatBlock), "m_waypointActionsToolbar");

        private static MyToolbar GetWaypointActionsToolbar(this MyDefensiveCombatBlock defensiveCombatBlock)
        {
            return (MyToolbar)WaypointActionsToolbarField.GetValue(defensiveCombatBlock);
        }        
    }
}