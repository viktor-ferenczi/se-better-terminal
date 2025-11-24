using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Extensions
{
    public static class MyTerminalBlockExtensions
    {
        private static readonly ThreadLocal<StringBuilder> TooltipText = new ThreadLocal<StringBuilder>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTooltipText(this MyTerminalBlock block)
        {
            var sb = TooltipText.Value ?? (TooltipText.Value = new StringBuilder());
            
            sb.Clear();
            sb.Append("Custom name: ");
            sb.Append(block.GetSafeName());

            sb.Append("\nOriginal name: ");
            block.AppendDefaultCustomName(sb);

            var grid = block.CubeGrid;
            if (grid != null)
            {
                sb.Append("\nGrid: ");
                sb.Append(grid.GetSafeName());
            }

            var integrity = (int)Math.Round(100.0f * block.SlimBlock.Integrity / Math.Max(0.001f, block.SlimBlock.MaxIntegrity));
            sb.Append("\nIntegrity: ");
            sb.Append(integrity);
            sb.Append("%");

#if DEBUG
            sb.Append("\nBlock position: ");
            sb.Append(block.Position.Format());

            sb.Append("\nBlock min position: ");
            sb.Append(block.Min.Format());

            sb.Append("\nBlock size in cubes: ");
            sb.Append(block.BlockDefinition.Size.Format());
#endif
            
            return sb.ToString();
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
                    // Required since game version 1.208.014, because the default block names are now lazy-loaded
                    // The following lines were copied from the MyTerminalBlock.CustomName getter:
                    if (block.m_defaultCustomName.Length == 0 && MySession.Static != null && MySession.Static.Ready)
                    {
                        block.LoadDefaultCustomName();
                    }
                    
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

            Debug.Assert(defaultName.Length != 0, "See the LoadDefaultCustomName call above, it did not work for some reason");
            
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