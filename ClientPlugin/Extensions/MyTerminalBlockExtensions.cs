using System;
using System.Runtime.CompilerServices;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;

namespace ClientPlugin.Extensions
{
    public static class MyTerminalBlockExtensions
    {
        private static readonly StringBuilder TooltipText = new StringBuilder();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTooltipText(this MyTerminalBlock block)
        {
            TooltipText.Clear();
            TooltipText.Append("Custom name: ");
            TooltipText.Append(block.GetSafeName());

            TooltipText.Append("\nOriginal name: ");
            block.AppendDefaultCustomName(TooltipText);

            var grid = block.CubeGrid;
            if (grid != null)
            {
                TooltipText.Append("\nOn grid: ");
                TooltipText.Append(grid.GetSafeName());
            }

            TooltipText.Append("\nBlock position: ");
            TooltipText.Append(block.Position.Format());

            TooltipText.Append("\nBlock min position: ");
            TooltipText.Append(block.Min.Format());

            TooltipText.Append("\nBlock size in cubes: ");
            TooltipText.Append(block.BlockDefinition.Size.Format());

            var integrity = (int)Math.Round(100.0f * block.SlimBlock.Integrity / Math.Max(0.001f, block.SlimBlock.MaxIntegrity));
            TooltipText.Append("\nIntegrity: ");
            TooltipText.Append(integrity);
            TooltipText.Append("%");

            return TooltipText.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendDefaultCustomName(this MyTerminalBlock block, StringBuilder sb)
        {
            sb.Append(block.m_defaultCustomName.ToString().TrimEnd());
            if (block is MyThrust thruster && thruster.GridThrustDirection != Vector3I.Zero)
            {
                sb.Append(" (");
                sb.Append(thruster.GetDirectionString());
                sb.Append(')');
            }
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
                    return b.m_waypointActionsToolbar;
            }

            return null;
        }
    }
}