using System;
using System.Collections.Generic;
using System.Linq;

namespace ClientPlugin.Organizer
{
    // Decides the new names, visibility, groups and toolbar slots of a ship.
    // Changes nothing, the plan is applied by PlanApplier.
    public static class Planner
    {
        public const int SlotsPerPage = 9;

        // Kinds which are always grouped and hidden from the terminal's block list:
        // the player uses them through their group, not block by block.
        private static readonly Dictionary<BlockKind, string> GroupLabels = new Dictionary<
            BlockKind,
            string
        >
        {
            { BlockKind.Gyro, "Gyroscope" },
            { BlockKind.Battery, "Battery" },
            { BlockKind.Light, "Light" },
            { BlockKind.Spotlight, "Spotlight" },
            { BlockKind.Drill, "Drill" },
            { BlockKind.Welder, "Welder" },
            { BlockKind.Grinder, "Grinder" },
            { BlockKind.Wheel, "Wheel" },
            { BlockKind.Camera, "Camera" },
            { BlockKind.LandingGear, "Landing Gear" },
            { BlockKind.JumpDrive, "Jump Drive" },
            { BlockKind.GasGenerator, "O2/H2 Generator" },
            { BlockKind.HydrogenEngine, "Hydrogen Engine" },
            { BlockKind.Reactor, "Reactor" },
            { BlockKind.SolarPanel, "Solar Panel" },
            { BlockKind.WindTurbine, "Wind Turbine" },
            { BlockKind.Cargo, "Cargo Container" },
            { BlockKind.Parachute, "Parachute" },
            { BlockKind.Turret, "Turret" },
            { BlockKind.Gun, "Gun" },
            { BlockKind.Decoy, "Decoy" },
            { BlockKind.Door, "Door" },
            { BlockKind.AirVent, "Air Vent" },
        };

        // Hidden from the terminal's block list without a group: nothing to set up there
        private static readonly HashSet<BlockKind> HiddenKinds = new HashSet<BlockKind>
        {
            BlockKind.Cockpit,
            BlockKind.Seat,
            BlockKind.ButtonPanel,
            BlockKind.Lcd,
            BlockKind.RefillStation,
        };

        // Have an inventory, but items only pass through them
        private static readonly HashSet<BlockKind> NoStorageKinds = new HashSet<BlockKind>
        {
            BlockKind.Drill,
            BlockKind.Welder,
            BlockKind.Grinder,
            BlockKind.Connector,
            BlockKind.Ejector,
            BlockKind.Sorter,
            BlockKind.Collector,
            BlockKind.GasTank,
        };

        // The name tells where these face
        private static readonly HashSet<BlockKind> FacingKinds = new HashSet<BlockKind>
        {
            BlockKind.Camera,
            BlockKind.Spotlight,
            BlockKind.Connector,
            BlockKind.LandingGear,
        };

        private static readonly ShipDirection[] Directions =
        {
            ShipDirection.Front,
            ShipDirection.Back,
            ShipDirection.Left,
            ShipDirection.Right,
            ShipDirection.Top,
            ShipDirection.Bottom,
        };

        private static readonly string[] ThrusterTypeOrder = { "Atmospheric", "Hydrogen", "Ion" };

        private static int Square(int value) => value * value;

        public static bool IsDefaultGridName(string name)
        {
            return string.IsNullOrWhiteSpace(name)
                || name.StartsWith("Small Grid")
                || name.StartsWith("Large Grid")
                || name.StartsWith("Static Grid");
        }

        public static OrganizerPlan Plan(ShipInfo ship, bool prefixWithShipName)
        {
            var plan = new OrganizerPlan();
            var prefix =
                prefixWithShipName && !IsDefaultGridName(ship.ShipName)
                    ? ship.ShipName.Trim() + " - "
                    : "";

            PlanBlocks(ship, plan, prefix);
            var groups = PlanGroups(ship, plan, prefix);
            PlanToolbar(ship, plan, groups);
            return plan;
        }

        private static void PlanBlocks(ShipInfo ship, OrganizerPlan plan, string prefix)
        {
            var cockpitCount = ship.Blocks.Count(b => b.Kind == BlockKind.Cockpit);

            var byName = new Dictionary<string, List<BlockInfo>>();
            foreach (var block in ship.Blocks)
            {
                var name = Naming.Shorten(block.DefaultName);
                if (block.IsRepairProjector)
                    name += " Repair";
                else if (block.IsMainCockpit && cockpitCount > 1)
                    name += " Main";
                else if (FacingKinds.Contains(block.Kind) && block.Direction != ShipDirection.None)
                    name += " " + block.Direction;

                if (!byName.TryGetValue(name, out var blocks))
                    byName[name] = blocks = new List<BlockInfo>();
                blocks.Add(block);
            }

            foreach (var pair in byName)
            {
                // Thrusters of the same direction get consecutive numbers, then front to back, top to bottom, left to right
                var blocks = pair
                    .Value.OrderBy(b =>
                        b.Kind == BlockKind.Thruster ? b.Direction : ShipDirection.None
                    )
                    .ThenBy(b => b.Z)
                    .ThenByDescending(b => b.Y)
                    .ThenBy(b => b.X)
                    .ThenBy(b => b.EntityId)
                    .ToList();
                var digits = blocks.Count.ToString().Length;
                for (var i = 0; i < blocks.Count; i++)
                {
                    var block = blocks[i];
                    var number =
                        blocks.Count > 1 ? " " + (i + 1).ToString().PadLeft(digits, '0') : "";
                    plan.Blocks[block.EntityId] = new BlockPlan
                    {
                        Name = prefix + pair.Key + number,
                        ShowInTerminal = !(
                            block.Kind == BlockKind.Thruster
                            || block.Kind == BlockKind.GasTank
                            || GroupLabels.ContainsKey(block.Kind)
                            || HiddenKinds.Contains(block.Kind)
                        ),
                        ShowInInventory =
                            !block.HasInventory
                            || block.ConveyorConnected && !NoStorageKinds.Contains(block.Kind),
                    };
                }
            }
        }

        // Returns the planned group names by key, the keys are internal to the planner
        private static Dictionary<string, string> PlanGroups(
            ShipInfo ship,
            OrganizerPlan plan,
            string prefix
        )
        {
            var groups = new Dictionary<string, GroupPlan>();

            void Add(string key, string name, BlockInfo block)
            {
                if (!groups.TryGetValue(key, out var group))
                {
                    groups[key] = group = new GroupPlan { Name = prefix + name };
                    plan.Groups.Add(group);
                }

                group.BlockIds.Add(block.EntityId);
            }

            // Stable group order: by kind, then as the keys sort
            foreach (
                var block in ship
                    .Blocks.OrderBy(b => b.Kind)
                    .ThenBy(b => b.ThrusterType ?? b.GasType ?? "")
                    .ThenBy(b => b.Direction)
            )
            {
                switch (block.Kind)
                {
                    case BlockKind.Thruster:
                        var label = Naming.Plural(Naming.ThrusterLabel(block.ThrusterType));
                        Add("Thruster", "Thrusters", block);
                        // The braking thrusters of all types: turned off to cruise without burning fuel
                        if (block.Direction == ShipDirection.Back)
                            Add("Reverse", "Thrusters (Backward)", block);
                        Add("Thruster:" + block.ThrusterType, label, block);
                        if (block.Direction != ShipDirection.None)
                            Add(
                                $"Thruster:{block.ThrusterType}:{block.Direction}",
                                $"{label} ({Naming.ThrustDirectionName(block.Direction)})",
                                block
                            );
                        break;

                    case BlockKind.GasTank:
                        Add("GasTank:" + block.GasType, $"{block.GasType} Tanks".Trim(), block);
                        break;

                    case BlockKind.Connector:
                        Add("Connector", "Connectors", block);
                        break;

                    default:
                        if (GroupLabels.TryGetValue(block.Kind, out var kindLabel))
                            Add(block.Kind.ToString(), Naming.Plural(kindLabel), block);
                        break;
                }
            }

            // The groups of all thrusters would repeat the groups of the only thruster type, a single connector needs no group
            var thrusterTypes = groups.Keys.Count(k =>
                k.StartsWith("Thruster:") && k.Split(':').Length == 2
            );
            if (thrusterTypes == 1)
            {
                Remove("Thruster");
                if (groups.ContainsKey("Reverse"))
                    Remove("Reverse");
            }
            if (
                groups.TryGetValue("Connector", out var connectors)
                && connectors.BlockIds.Count < 2
            )
                Remove("Connector");

            void Remove(string key)
            {
                plan.Groups.Remove(groups[key]);
                groups.Remove(key);
            }

            return groups.ToDictionary(p => p.Key, p => p.Value.Name);
        }

        private static void PlanToolbar(
            ShipInfo ship,
            OrganizerPlan plan,
            Dictionary<string, string> groups
        )
        {
            var cockpits = ship.Blocks.Where(b => b.Kind == BlockKind.Cockpit).ToList();
            // A drone has no cockpit, its remote control gets the toolbar then
            var cockpit =
                cockpits.FirstOrDefault(b => b.IsMainCockpit)
                ?? cockpits.FirstOrDefault()
                ?? ship.Blocks.FirstOrDefault(b => b.Kind == BlockKind.RemoteControl);
            if (cockpit == null)
                return;

            plan.CockpitId = cockpit.EntityId;

            void Slot(int page, int slot, string groupKey, string action)
            {
                if (groupKey != null && groups.TryGetValue(groupKey, out var groupName))
                    Put(page, slot, new SlotPlan { GroupName = groupName, Action = action });
            }

            void BlockSlot(int page, int slot, BlockInfo block, string action)
            {
                if (block != null)
                    Put(page, slot, new SlotPlan { BlockId = block.EntityId, Action = action });
            }

            void Put(int page, int slot, SlotPlan slotPlan)
            {
                slotPlan.Index = page * SlotsPerPage + slot;
                plan.CockpitSlots.Add(slotPlan);
                plan.OwnedPages.Add(page);
            }

            var thrusterTypes = ship
                .Blocks.Where(b => b.Kind == BlockKind.Thruster)
                .Select(b => b.ThrusterType)
                .Distinct()
                .OrderBy(t =>
                {
                    var i = Array.IndexOf(ThrusterTypeOrder, t);
                    return i < 0 ? ThrusterTypeOrder.Length : i;
                })
                .ThenBy(t => t)
                .ToList();
            var cameras = ship
                .Blocks.Where(b => b.Kind == BlockKind.Camera)
                .OrderBy(b =>
                    b.Direction == ShipDirection.None
                        ? int.MaxValue
                        : Array.IndexOf(Directions, b.Direction)
                )
                .ThenBy(b => plan.Blocks[b.EntityId].Name)
                .ToList();
            var connectors = ship.Blocks.Where(b => b.Kind == BlockKind.Connector).ToList();

            void Docking(int page, int slot)
            {
                if (connectors.Count == 1)
                    BlockSlot(page, slot, connectors[0], "SwitchLock");
                else
                    Slot(page, slot, "Connector", "SwitchLock");
            }

            // The camera to align a connector with: the nearest one facing the same side
            BlockInfo DockingCamera(BlockInfo connector)
            {
                return cameras
                    .Where(c =>
                        c.Direction == connector.Direction && c.Direction != ShipDirection.None
                    )
                    .OrderBy(c =>
                        Square(c.X - connector.X)
                        + Square(c.Y - connector.Y)
                        + Square(c.Z - connector.Z)
                    )
                    .FirstOrDefault();
            }

            var dockingCameras = connectors
                .Select(DockingCamera)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            // The tools or weapons of one block definition as a toolbar item, the most numerous first
            IEnumerable<BlockInfo> Definitions(params BlockKind[] kinds)
            {
                return ship
                    .Blocks.Where(b => kinds.Contains(b.Kind))
                    .GroupBy(b => b.TypeId + "/" + b.SubtypeId)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => g.First());
            }

            void WeaponSlot(int page, int slot, BlockInfo block)
            {
                Put(
                    page,
                    slot,
                    new SlotPlan { WeaponTypeId = block.TypeId, WeaponSubtypeId = block.SubtypeId }
                );
            }

            // The role of the ship decides the first slots of page 1
            var tools = new[] { BlockKind.Drill, BlockKind.Welder, BlockKind.Grinder };
            var isToolShip = ship.Blocks.Any(b => tools.Contains(b.Kind));
            var isFighter = !isToolShip && ship.Blocks.Any(b => b.Kind == BlockKind.Gun);

            // Page 1: flight and operation
            plan.OwnedPages.Add(0);
            var next = 0;
            if (isToolShip)
            {
                // Slot 1 is the main tool to work with, then the switches of the tools
                WeaponSlot(0, next++, Definitions(tools).First());
                foreach (var tool in tools)
                {
                    if (groups.ContainsKey(tool.ToString()))
                        Slot(0, next++, tool.ToString(), "OnOff");
                }
            }
            else if (isFighter)
            {
                // Slot 1 is the main weapon, then the secondary ones
                foreach (var weapon in Definitions(BlockKind.Gun).Take(3))
                    WeaponSlot(0, next++, weapon);
            }

            // Braking thrusters: off for fuel efficient cruising
            var reverse = groups.ContainsKey("Reverse")
                ? "Reverse"
                : thrusterTypes
                    .Select(t => $"Thruster:{t}:{ShipDirection.Back}")
                    .FirstOrDefault(groups.ContainsKey);
            if (reverse != null)
                Slot(0, next++, reverse, "OnOff");

            // A fighter aims with the zoom of its forward cameras, its docking is all on page 2
            if (isFighter)
            {
                foreach (var camera in cameras.Where(c => c.Direction == ShipDirection.Front))
                {
                    if (next < 6)
                        BlockSlot(0, next++, camera, "View");
                }
            }

            // Lift thrusters: override for hovering and cruising in gravity
            var liftType = thrusterTypes.FirstOrDefault(t =>
                groups.ContainsKey($"Thruster:{t}:{ShipDirection.Top}")
            );
            if (liftType != null && next < 5)
            {
                Slot(0, next++, $"Thruster:{liftType}:{ShipDirection.Top}", "DecreaseOverride");
                Slot(0, next++, $"Thruster:{liftType}:{ShipDirection.Top}", "IncreaseOverride");
            }

            Slot(0, 6, BlockKind.Light.ToString(), "OnOff");
            Slot(0, 7, BlockKind.Spotlight.ToString(), "OnOff");
            if (!isFighter)
            {
                // Docking: the cameras on the connectors' sides as far as they fit, all of them are on page 2
                foreach (var camera in dockingCameras)
                {
                    if (next < 6)
                        BlockSlot(0, next++, camera, "View");
                }

                if (connectors.Count != 0)
                    Docking(0, 8);
                else
                    Slot(0, 8, BlockKind.LandingGear.ToString(), "SwitchLock");
            }

            // Page 2: docking. Each connector with its camera, the landing gears on the last slot.
            next = 0;
            foreach (
                var connector in connectors
                    .OrderBy(c => Array.IndexOf(Directions, c.Direction))
                    .ThenBy(c => plan.Blocks[c.EntityId].Name)
            )
            {
                var camera = DockingCamera(connector);
                if (next + (camera != null ? 2 : 1) > 8)
                    break;

                if (camera != null)
                    BlockSlot(1, next++, camera, "View");
                BlockSlot(1, next++, connector, "SwitchLock");
            }

            Slot(1, 8, BlockKind.LandingGear.ToString(), "SwitchLock");

            // Page 3: docked. Recharge, refill and what is turned off while docked, on fixed slots.
            Slot(2, 0, BlockKind.Battery.ToString(), "Recharge");
            Slot(2, 1, "GasTank:Hydrogen", "Stockpile");
            Slot(2, 2, "GasTank:Oxygen", "Stockpile");
            Slot(
                2,
                3,
                thrusterTypes.Count == 1 ? "Thruster:" + thrusterTypes[0] : "Thruster",
                "OnOff"
            );
            Slot(2, 4, BlockKind.Gyro.ToString(), "OnOff");
            Slot(2, 5, BlockKind.HydrogenEngine.ToString(), "OnOff");
            Slot(2, 6, BlockKind.Reactor.ToString(), "OnOff");
            Slot(2, 7, BlockKind.GasGenerator.ToString(), "OnOff");

            // Page 4: jump
            Slot(3, 0, BlockKind.JumpDrive.ToString(), "Jump");
            Slot(3, 1, BlockKind.JumpDrive.ToString(), "Recharge");
            Slot(3, 2, BlockKind.JumpDrive.ToString(), "OnOff");
            Slot(3, 3, BlockKind.JumpDrive.ToString(), "DecreaseJumpDistance");
            Slot(3, 4, BlockKind.JumpDrive.ToString(), "IncreaseJumpDistance");

            // Page 5: cameras
            for (var i = 0; i < cameras.Count && i < SlotsPerPage; i++)
                BlockSlot(4, i, cameras[i], "View");

            // Page 6: systems
            next = 0;
            var systems = thrusterTypes
                .Select(t => "Thruster:" + t)
                .Concat(
                    new[]
                    {
                        BlockKind.Wheel,
                        BlockKind.Turret,
                        BlockKind.Gun,
                        BlockKind.Decoy,
                        BlockKind.SolarPanel,
                        BlockKind.WindTurbine,
                    }.Select(k => k.ToString())
                );
            foreach (var key in systems)
            {
                if (next < SlotsPerPage && groups.ContainsKey(key))
                    Slot(5, next++, key, "OnOff");
            }

            // Page 9: repair and emergency. The repair projector is always the last slot.
            Slot(8, 0, BlockKind.Welder.ToString(), "OnOff");
            Slot(8, 4, BlockKind.Parachute.ToString(), "AutoDeploy");
            Slot(8, 6, BlockKind.Parachute.ToString(), "Open");
            var remoteControl = ship.Blocks.FirstOrDefault(b =>
                b.Kind == BlockKind.RemoteControl && b != cockpit
            );
            if (remoteControl != null)
            {
                // The remote control gets the toolbar before this button is added
                plan.RemoteControlId = remoteControl.EntityId;
                plan.OwnedPages.Add(8);
            }

            var projector = ship.Blocks.FirstOrDefault(b => b.IsRepairProjector);
            if (projector != null)
            {
                plan.RepairProjectorId = projector.EntityId;
                BlockSlot(8, 8, projector, "OnOff");
            }
        }
    }
}
