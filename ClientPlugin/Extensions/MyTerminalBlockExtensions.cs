using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Utils;
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
                TooltipText.Append("\nGrid: ");
                TooltipText.Append(grid.GetSafeName());
            }

            var integrity = (int)Math.Round(100.0f * block.SlimBlock.Integrity / Math.Max(0.001f, block.SlimBlock.MaxIntegrity));
            TooltipText.Append("\nIntegrity: ");
            TooltipText.Append(integrity);
            TooltipText.Append("%");

#if DEBUG
            TooltipText.Append("\nBlock position: ");
            TooltipText.Append(block.Position.Format());

            TooltipText.Append("\nBlock min position: ");
            TooltipText.Append(block.Min.Format());

            TooltipText.Append("\nBlock size in cubes: ");
            TooltipText.Append(block.BlockDefinition.Size.Format());
#endif
            
            return TooltipText.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendDefaultCustomName(this MyTerminalBlock block, StringBuilder sb)
        {
            // See #5: Workaround for dirty read of StringBuilder state during terminal block initialization
            string defaultName = null;
            for (var retry = 0; retry < 11; retry++)
            {
                try
                {
                    defaultName = block.m_defaultCustomName.ToString();
                    break;
                }
                catch (Exception e)
                {
                    MyLog.Default.Warning($"BetterTerminal: Prevented crash on dirty read of StringBuilder instance in AppendDefaultCustomName. This is HARMLESS and retried 10 times with a short delay. This issue affects only the Terminal dialog on clients and has no effect on servers. See #5; Exception: {e.Message}\n---\n{e.StackTrace}---\n");
                    Thread.Sleep(1);
                }
            }

            // Fallback information if the block name could not be acquired with 10 retries (this should never happen)
            if (defaultName == null)
                defaultName = "!See Better Terminal Issue #5!";

            sb.Append(defaultName.TrimEnd());
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