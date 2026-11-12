using System;
using System.Collections.Generic;
using System.Linq;
using ClientPlugin.Extensions;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Screens.Helpers;
using VRage.Game;
using VRage.ObjectBuilders.Private;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Organizer
{
    // Renames, groups and hides the blocks of a ship and sets up its cockpit toolbar by the
    // planner's rules, after saving a backup. Revert restores the ship from the latest backup.
    // All changes go through the calls the terminal itself uses, so they reach the server.
    public static class ShipOrganizer
    {
        // The cockpit's button taking control of the remote control: page 9, slot 8
        private const int RemoteControlSlot = 8 * Planner.SlotsPerPage + 7;

        // Each step of loading a projection may take this long
        private const int StepTimeoutSeconds = 15;

        // Returns a summary for the player
        public static string Organize(MyCubeGrid grid)
        {
            var scanner = new ShipScanner(grid);
            if (scanner.Blocks.Count == 0)
                return "No blocks you could change on this ship";

            var terminalSystem = scanner.MainGrid.GridSystems.TerminalSystem;
            var plan = Planner.Plan(scanner.Ship, Config.Current.OrganizerPrefixShipName);

            var backup = OrganizerBackup.Capture(scanner, terminalSystem, plan);
            var projector =
                plan.RepairProjectorId != 0
                    ? scanner.Blocks[plan.RepairProjectorId] as MyProjectorBase
                    : null;
            if (projector != null)
                backup.CaptureProjector(projector);
            backup.Save();

            var renamed = 0;
            foreach (var pair in plan.Blocks)
            {
                var block = scanner.Blocks[pair.Key];
                if (
                    SetBlock(
                        block,
                        pair.Value.Name,
                        pair.Value.ShowInTerminal,
                        pair.Value.ShowInInventory
                    )
                )
                    renamed++;
            }

            foreach (var groupPlan in plan.Groups)
                SetGroup(
                    terminalSystem,
                    groupPlan.Name,
                    groupPlan.BlockIds.Select(id => scanner.Blocks[id])
                );

            if (plan.CockpitId != 0)
                SetToolbars(scanner, plan);

            var summary =
                $"Organized {scanner.MainGrid.GetSafeName()}: {renamed} blocks renamed, {plan.Groups.Count} groups, {plan.CockpitSlots.Count} toolbar slots";
            if (projector != null)
            {
                LoadProjection(
                    projector,
                    BuildBlueprint(scanner, projector),
                    Vector3I.Zero,
                    FindAlignedRotation(projector)
                );
                summary += ", repair projector reloaded";
            }

            MyLog.Default.Info($"{Plugin.Name}: {summary}; backup: {backup.Path}");
            return summary;
        }

        public static bool HasBackup(MyCubeGrid grid)
        {
            return OrganizerBackup.FindLatest(ShipScanner.GetGrids(grid).Select(g => g.EntityId))
                != null;
        }

        public static string Revert(MyCubeGrid grid)
        {
            var scanner = new ShipScanner(grid);
            var backup = OrganizerBackup.LoadLatest(scanner.Grids.Select(g => g.EntityId));
            if (backup == null)
                return "This ship has no backup to revert to";

            var terminalSystem = scanner.MainGrid.GridSystems.TerminalSystem;

            var restored = 0;
            foreach (var block in backup.Blocks)
            {
                // Blocks removed since the backup are skipped
                if (
                    scanner.Blocks.TryGetValue(block.Id, out var terminalBlock)
                    && SetBlock(
                        terminalBlock,
                        block.Name,
                        block.ShowInTerminal,
                        block.ShowInInventory
                    )
                )
                    restored++;
            }

            foreach (var name in backup.AddedGroups)
            {
                var group = terminalSystem.BlockGroups.Find(g => g.Name.ToString() == name);
                if (group != null)
                    terminalSystem.RemoveGroup(group, true);
            }

            foreach (var group in backup.Groups)
            {
                var blocks = group
                    .BlockIds.Where(scanner.Blocks.ContainsKey)
                    .Select(id => scanner.Blocks[id])
                    .ToList();
                var current = terminalSystem.BlockGroups.Find(g => g.Name.ToString() == group.Name);
                if (blocks.Count != 0 && (current == null || !current.Blocks.SetEquals(blocks)))
                    SetGroup(terminalSystem, group.Name, blocks);
            }

            foreach (var toolbarBackup in backup.Toolbars)
            {
                if (!scanner.Blocks.TryGetValue(toolbarBackup.BlockId, out var block))
                    continue;

                var builder = OrganizerBackup.DeserializeToolbar(toolbarBackup.Xml);
                var slots = (
                    builder?.Slots ?? new List<MyObjectBuilder_Toolbar.Slot>()
                ).ToDictionary(s => s.Index, s => s.Data);
                var toolbar = block.GetToolbar();
                for (var i = 0; i < toolbar.ItemCount; i++)
                {
                    slots.TryGetValue(i, out var data);
                    if (data != null || toolbar.GetItemAtIndex(i) != null)
                        toolbar.SetItemAtIndex(
                            i,
                            data != null ? MyToolbarItemFactory.CreateToolbarItem(data) : null
                        );
                }
            }

            if (
                backup.ProjectorId != 0
                && scanner.Blocks.TryGetValue(backup.ProjectorId, out var projectorBlock)
                && projectorBlock is MyProjectorBase projector
            )
                LoadProjection(
                    projector,
                    backup.LoadProjectorBlueprint(),
                    new Vector3I(
                        backup.ProjectorOffsetX,
                        backup.ProjectorOffsetY,
                        backup.ProjectorOffsetZ
                    ),
                    new Vector3I(
                        backup.ProjectorRotationX,
                        backup.ProjectorRotationY,
                        backup.ProjectorRotationZ
                    )
                );

            backup.Delete();
            var summary =
                $"Reverted {scanner.MainGrid.GetSafeName()} to its state of {backup.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm}: {restored} blocks restored";
            MyLog.Default.Info($"{Plugin.Name}: {summary}");
            return summary;
        }

        private static bool SetBlock(
            MyTerminalBlock block,
            string name,
            bool showInTerminal,
            bool showInInventory
        )
        {
            block.ShowInTerminal = showInTerminal;
            block.ShowInInventory = showInInventory;
            if (block.CustomName.ToString() == name)
                return false;

            block.SetCustomName(name);
            return true;
        }

        private static void SetGroup(
            MyGridTerminalSystem terminalSystem,
            string name,
            IEnumerable<MyTerminalBlock> blocks
        )
        {
            var group = new MyBlockGroup();
            group.Name.Append(name);
            group.Blocks.UnionWith(blocks);
            terminalSystem.AddUpdateGroup(group, true, true);
        }

        private static void SetToolbars(ShipScanner scanner, OrganizerPlan plan)
        {
            var cockpit = scanner.Blocks[plan.CockpitId];
            var toolbar = cockpit.GetToolbar();

            var slots = plan.CockpitSlots.ToDictionary(s => s.Index);
            foreach (var page in plan.OwnedPages)
            {
                for (
                    var i = page * Planner.SlotsPerPage;
                    i < (page + 1) * Planner.SlotsPerPage && i < toolbar.ItemCount;
                    i++
                )
                {
                    slots.TryGetValue(i, out var slot);
                    if (slot != null || toolbar.GetItemAtIndex(i) != null)
                        toolbar.SetItemAtIndex(i, slot != null ? CreateItem(cockpit, slot) : null);
                }
            }

            if (plan.RemoteControlId == 0)
                return;

            // The remote control gets the cockpit's toolbar as it is now, only then
            // comes the cockpit's button to take control of the remote control
            var remoteControl = scanner.Blocks[plan.RemoteControlId];
            var remoteToolbar = remoteControl.GetToolbar();
            for (var i = 0; i < toolbar.ItemCount && i < remoteToolbar.ItemCount; i++)
            {
                var builder = toolbar.GetItemAtIndex(i)?.GetObjectBuilder();
                if (builder == null && remoteToolbar.GetItemAtIndex(i) == null)
                    continue;

                // Group slots name the block they are on
                if (builder is MyObjectBuilder_ToolbarItemTerminalGroup groupItem)
                    groupItem.BlockEntityId = remoteControl.EntityId;

                remoteToolbar.SetItemAtIndex(
                    i,
                    builder != null ? MyToolbarItemFactory.CreateToolbarItem(builder) : null
                );
            }

            toolbar.SetItemAtIndex(
                RemoteControlSlot,
                CreateItem(
                    cockpit,
                    new SlotPlan { BlockId = remoteControl.EntityId, Action = "Control" }
                )
            );
        }

        private static MyToolbarItem CreateItem(MyTerminalBlock owner, SlotPlan slot)
        {
            if (slot.GroupName != null)
            {
                var groupItem =
                    MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalGroup>();
                groupItem.GroupName = slot.GroupName;
                groupItem.BlockEntityId = owner.EntityId;
                groupItem._Action = slot.Action;
                return MyToolbarItemFactory.CreateToolbarItem(groupItem);
            }

            var blockItem =
                MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalBlock>();
            blockItem.BlockEntityId = slot.BlockId;
            blockItem._Action = slot.Action;
            return MyToolbarItemFactory.CreateToolbarItem(blockItem);
        }

        // The ship as a blueprint, the way copying it to the clipboard builds it
        private static List<MyObjectBuilder_CubeGrid> BuildBlueprint(
            ShipScanner scanner,
            MyProjectorBase projector
        )
        {
            // The vanilla projector takes one grid and warns about the rest. Multigrid Projector takes all.
            var multigrid = AppDomain
                .CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name.StartsWith("MultigridProjector"));
            var grids = new List<MyObjectBuilder_CubeGrid>();
            foreach (
                var grid in multigrid
                    ? scanner.Grids.OrderByDescending(g => g == projector.CubeGrid).ToList()
                    : new List<MyCubeGrid> { projector.CubeGrid }
            )
            {
                var builder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder(true);
                builder.GridGeneralDamageModifier = 1f;
                builder.Immune = false;
                projector.Clipboard.RemovePilots(builder);
                foreach (var block in builder.CubeBlocks)
                {
                    // A projection inside the projection would grow the blueprint on every reload
                    if (block is MyObjectBuilder_ProjectorBase projectorBuilder)
                    {
                        projectorBuilder.ProjectedGrid = null;
                        projectorBuilder.ProjectedGrids = null;
                    }
                }

                grids.Add(builder);
            }

            // The projection puts the first block of the first grid on the projector.
            // With the projector itself as that block the projection covers the ship.
            var blocks = grids[0].CubeBlocks;
            var index = blocks.FindIndex(b => b.EntityId == projector.EntityId);
            if (index > 0)
            {
                var projectorBuilder = blocks[index];
                blocks.RemoveAt(index);
                blocks.Insert(0, projectorBuilder);
            }

            return grids;
        }

        // The rotation setting of the projector which turns the projection back from the
        // projector's own orientation to the grid's, so a projection of the ship covers the ship
        private static Vector3I FindAlignedRotation(MyProjectorBase projector)
        {
            projector.Orientation.GetMatrix(out var orientation);
            var wanted = Matrix.Transpose(orientation);
            var step = MathHelper.ToRadians(projector.BlockDefinition.RotationAngleStepDeg);
            for (var x = -2; x <= 2; x++)
            for (var y = -2; y <= 2; y++)
            for (var z = -2; z <= 2; z++)
            {
                var rotation = Matrix.CreateFromQuaternion(
                    Quaternion.CreateFromYawPitchRoll(x * step, y * step, z * step)
                );
                if (rotation.EqualsFast(ref wanted, 0.01f))
                    return new Vector3I(x, y, z);
            }

            return projector.ProjectionRotation;
        }

        // Loads a blueprint into the projector over a few frames, like the blueprint screen does:
        // remove the old projection, wait until its preview grids are gone (a client has to wait
        // for the server with that), then load the new one. A repair projector is usually kept
        // turned off, it is turned on for the time of the loading.
        private static void LoadProjection(
            MyProjectorBase projector,
            List<MyObjectBuilder_CubeGrid> grids,
            Vector3I offset,
            Vector3I rotation
        )
        {
            var wasEnabled = projector.Enabled;
            projector.Enabled = true;

            var step = 0;
            var deadline = DateTime.UtcNow.AddSeconds(StepTimeoutSeconds);
            MyCubeGrid[] oldPreviews = null;
            List<MyObjectBuilder_CubeGrid> oldBuilders = null;

            void Later() => MySandboxGame.Static.Invoke(Step, "BetterTerminal.LoadProjection");

            void Next()
            {
                step++;
                deadline = DateTime.UtcNow.AddSeconds(StepTimeoutSeconds);
                Later();
            }

            void Finish(string problem)
            {
                projector.Enabled = wasEnabled;
                if (problem == null)
                    return;

                MyLog.Default.Warning($"{Plugin.Name}: {projector.GetDebugName()}: {problem}");
                Sandbox.ModAPI.MyAPIGateway.Utilities?.ShowMessage(
                    "Better Terminal",
                    $"{projector.CustomName}: {problem}"
                );
            }

            void Step()
            {
                try
                {
                    if (projector.Closed)
                        return;

                    var timeout = DateTime.UtcNow > deadline;
                    switch (step)
                    {
                        case 0:
                            // Wait for power. Turning the projector on starts activating its old projection,
                            // which has to finish too: removing it meanwhile blocks any later activation.
                            if (!projector.IsWorking || projector.IsActivating)
                            {
                                if (timeout)
                                    Finish(
                                        "The projector is not working, its blueprint was not loaded"
                                    );
                                else
                                    Later();
                                return;
                            }

                            oldPreviews = projector.Clipboard.PreviewGrids.ToArray();
                            if (projector.Clipboard.HasCopiedGrids())
                                projector.SendRemoveProjection();
                            Next();
                            return;

                        case 1: // Wait for the old projection to go
                            if (
                                projector.IsActivating
                                || projector.Clipboard.HasCopiedGrids()
                                || oldPreviews.Any(g => !g.Closed)
                            )
                            {
                                if (timeout)
                                    Finish(
                                        "The old projection was not removed, the new blueprint was not loaded"
                                    );
                                else
                                    Later();
                                return;
                            }

                            projector.SendNewOffset(
                                offset,
                                rotation,
                                projector.Scale,
                                projector.m_showOnlyBuildable
                            );
                            if (grids == null || grids.Count == 0)
                            {
                                Finish(null);
                                return;
                            }

                            // The grid size check of InitFromObjectBuilder reads the clipboard
                            oldBuilders = projector.m_originalGridBuilders;
                            projector.Clipboard.SetGridFromBuilders(
                                grids.ToArray(),
                                Vector3.Zero,
                                0f
                            );
                            projector.InitFromObjectBuilder(grids);
                            Next();
                            return;

                        case 2:
                            // The blueprint is prepared on a worker thread, then the projection is activated on another.
                            // Turning the projector off before the projection is up would leave it stuck in activation.
                            if (
                                projector.m_originalGridBuilders == null
                                || projector.m_originalGridBuilders == oldBuilders
                                || projector.IsActivating
                                || !projector.Clipboard.IsActive
                            )
                            {
                                if (timeout)
                                {
                                    // Stays turned on: turning it off during activation would leave it stuck
                                    wasEnabled = true;
                                    Finish(
                                        "The projection did not come up in time, the projector was left turned on"
                                    );
                                }
                                else
                                    Later();
                                return;
                            }

                            Finish(null);
                            return;
                    }
                }
                catch (Exception e)
                {
                    // Nobody catches the exception of a later frame, the game would crash on it
                    MyLog.Default.Error(
                        $"{Plugin.Name}: Failed to load the projection of {projector.GetDebugName()}: {e}"
                    );
                }
            }

            Later();
        }
    }
}
