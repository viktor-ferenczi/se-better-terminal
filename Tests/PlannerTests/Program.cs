// Tests of the ship organizer's planner.
//
// Run the built-in checks on a synthetic ship:
//   dotnet run --project Tests/PlannerTests
//
// Print the plan for a blueprint, to review the rules on a real ship:
//   dotnet run --project Tests/PlannerTests -- <game>/Content/Data <blueprint folder>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ClientPlugin.Organizer;

static class Program
{
    private static int failures;

    static int Main(string[] args)
    {
        if (args.Length == 2)
        {
            Print(Blueprint.Load(args[0], args[1]));
            return 0;
        }

        TestNaming();
        TestPlan();
        Console.WriteLine(failures == 0 ? "PASS" : $"FAIL: {failures} checks failed");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string what)
    {
        if (condition)
            return;

        failures++;
        Console.WriteLine("FAILED: " + what);
    }

    private static void Equal(object expected, object actual, string what) =>
        Check(Equals(expected, actual), $"{what}: expected '{expected}', got '{actual}'");

    private static void TestNaming()
    {
        Equal("Hydrogen Thruster", Naming.Shorten("Hydrogen Thruster"), "short names stay");
        Equal(
            "Large Atmo Thruster",
            Naming.Shorten("Sci-Fi Large Atmospheric Thruster"),
            "style word and abbreviation"
        );
        Equal(
            "Large Hydrogen Thruster",
            Naming.Shorten("Industrial Large Hydrogen Thruster"),
            "industrial thruster"
        );
        Equal(
            "Short Wheel 5x5 Right",
            Naming.Shorten("Offroad Short Wheel Suspension 5x5 Right"),
            "wheel"
        );
        Equal("Large Cargo Container", Naming.Shorten("Large Industrial Cargo Container"), "cargo");
        Equal("Batteries", Naming.Plural("Battery"), "plural y");
        Equal("Gyroscopes", Naming.Plural("Gyroscope"), "plural");
        Equal(ShipDirection.Front, Naming.DirectionOf(0, 0, -1), "front");
        Equal(ShipDirection.Bottom, Naming.DirectionOf(0.1, -0.9, 0.2), "bottom");
        Equal(
            BlockKind.Ejector,
            Naming.Classify("ShipConnector", "ConnectorSmall", false),
            "ejector"
        );
        Equal(BlockKind.Seat, Naming.Classify("Cockpit", "PassengerSeatLarge", false), "seat");
    }

    private static void TestPlan()
    {
        var ship = new ShipInfo { ShipName = "Mole" };
        long id = 0;
        BlockInfo Add(
            string type,
            string name,
            ShipDirection direction = ShipDirection.None,
            int z = 0,
            string subtype = ""
        )
        {
            var block = new BlockInfo
            {
                EntityId = ++id,
                TypeId = type,
                SubtypeId = subtype,
                DefaultName = name,
                CurrentName = "x" + id,
                Kind = Naming.Classify(type, subtype, true),
                Direction = direction,
                Z = z,
            };
            ship.Blocks.Add(block);
            return block;
        }

        var cockpit = Add("Cockpit", "Cockpit");
        cockpit.IsMainCockpit = true;
        var up1 = Add("Thrust", "Hydrogen Thruster", ShipDirection.Top, 2);
        up1.ThrusterType = "Hydrogen";
        var back = Add("Thrust", "Hydrogen Thruster", ShipDirection.Back);
        back.ThrusterType = "Hydrogen";
        var up2 = Add("Thrust", "Hydrogen Thruster", ShipDirection.Top, 1);
        up2.ThrusterType = "Hydrogen";
        var ion = Add("Thrust", "Ion Thruster", ShipDirection.Front);
        ion.ThrusterType = "Ion";
        var drill = Add("Drill", "Drill");
        drill.HasInventory = true;
        var cargo = Add("CargoContainer", "Large Cargo Container");
        cargo.HasInventory = true;
        var isolated = Add("CargoContainer", "Small Cargo Container");
        isolated.HasInventory = true;
        isolated.ConveyorConnected = false;
        var camera = Add("CameraBlock", "Camera", ShipDirection.Front);
        var connector = Add("ShipConnector", "Connector", ShipDirection.Back, subtype: "Connector");
        connector.HasInventory = true;
        var tank = Add("OxygenTank", "Hydrogen Tank");
        tank.GasType = "Hydrogen";
        var battery = Add("BatteryBlock", "Battery");
        var remote = Add("RemoteControl", "Remote Control");
        var projector = Add("Projector", "Projector");
        projector.IsRepairProjector = true;
        var timer = Add("TimerBlock", "Timer Block");

        var plan = Planner.Plan(ship, true);
        string Name(BlockInfo block) => plan.Blocks[block.EntityId].Name;

        // Naming: prefix, numbering from 1 in thrust direction order, suffixes
        Equal("Mole - Cockpit", Name(cockpit), "single cockpit has no suffix");
        Equal("Mole - Hydrogen Thruster 1", Name(back), "thruster numbering by direction");
        Equal("Mole - Hydrogen Thruster 2", Name(up2), "thruster numbering by position");
        Equal("Mole - Hydrogen Thruster 3", Name(up1), "thruster numbering by position");
        Equal("Mole - Ion Thruster", Name(ion), "single block has no number");
        Equal("Mole - Camera Front", Name(camera), "camera suffix");
        Equal("Mole - Connector Back", Name(connector), "connector suffix");
        Equal("Mole - Projector Repair", Name(projector), "repair projector");
        Check(
            plan.Blocks.Values.Select(b => b.Name).Distinct().Count() == plan.Blocks.Count,
            "names are unique"
        );

        // Visibility
        Check(!plan.Blocks[up1.EntityId].ShowInTerminal, "thrusters are hidden from the terminal");
        Check(
            !plan.Blocks[cockpit.EntityId].ShowInTerminal,
            "the cockpit is hidden from the terminal"
        );
        Check(plan.Blocks[timer.EntityId].ShowInTerminal, "timers stay in the terminal");
        Check(plan.Blocks[cargo.EntityId].ShowInInventory, "cargo stays in the inventory screen");
        Check(
            !plan.Blocks[isolated.EntityId].ShowInInventory,
            "unreachable cargo is hidden from the inventory screen"
        );
        Check(
            !plan.Blocks[drill.EntityId].ShowInInventory,
            "drills are hidden from the inventory screen"
        );

        // Groups
        var groups = plan.Groups.ToDictionary(g => g.Name, g => g.BlockIds);
        Check(
            groups.ContainsKey("Mole - Thrusters") && groups["Mole - Thrusters"].Count == 4,
            "all thrusters group"
        );
        Check(
            groups.ContainsKey("Mole - Hydrogen Thrusters (Up)")
                && groups["Mole - Hydrogen Thrusters (Up)"].Count == 2,
            "thrusters by direction"
        );
        Check(groups.ContainsKey("Mole - Ion Thrusters (Forward)"), "ion thrusters by direction");
        Check(
            groups.ContainsKey("Mole - Batteries")
                && groups.ContainsKey("Mole - Drills")
                && groups.ContainsKey("Mole - Hydrogen Tanks"),
            "kind groups"
        );
        Check(!groups.ContainsKey("Mole - Connectors"), "a single connector has no group");
        foreach (var block in ship.Blocks)
        {
            var grouped = plan.Groups.Any(g => g.BlockIds.Contains(block.EntityId));
            var hidden = !plan.Blocks[block.EntityId].ShowInTerminal;
            Check(
                !hidden || grouped || block.Kind == BlockKind.Cockpit,
                $"{block.DefaultName}: hidden blocks are reachable by a group"
            );
        }

        // Toolbar
        Equal(cockpit.EntityId, plan.CockpitId, "cockpit");
        Equal(remote.EntityId, plan.RemoteControlId, "remote control");
        Equal(projector.EntityId, plan.RepairProjectorId, "repair projector");
        SlotPlan At(int page, int slot) =>
            plan.CockpitSlots.SingleOrDefault(s => s.Index == (page - 1) * 9 + slot - 1);
        Equal("Mole - Drills", At(1, 1)?.GroupName, "1.1 is the tool");
        Equal("DecreaseOverride", At(1, 2)?.Action, "1.2 lowers the lift");
        Equal(camera.EntityId, At(1, 4)?.BlockId, "1.4 is the front camera");
        Equal(connector.EntityId, At(1, 9)?.BlockId, "1.9 locks the connector");
        Equal("Recharge", At(2, 3)?.Action, "2.3 recharges the batteries");
        Equal("Mole - Thrusters", At(2, 6)?.GroupName, "2.6 turns all thrusters off");
        Equal(projector.EntityId, At(9, 9)?.BlockId, "9.9 is the repair projector");
        Check(
            plan.OwnedPages.SetEquals(new[] { 0, 1, 3, 4, 8 }),
            "owned pages: " + string.Join(",", plan.OwnedPages)
        );
        Check(
            plan.CockpitSlots.Select(s => s.Index).Distinct().Count() == plan.CockpitSlots.Count,
            "one item per slot"
        );

        // No prefix for a default grid name
        ship.ShipName = "Small Grid 1234";
        Equal(
            "Cockpit",
            Planner.Plan(ship, true).Blocks[cockpit.EntityId].Name,
            "no prefix from a default grid name"
        );
    }

    private static void Print(ShipInfo ship)
    {
        var plan = Planner.Plan(ship, true);
        var names = ship.Blocks.ToDictionary(b => b.EntityId, b => plan.Blocks[b.EntityId].Name);
        Console.WriteLine($"== {ship.ShipName}: {ship.Blocks.Count} blocks");
        foreach (var block in ship.Blocks.OrderBy(b => names[b.EntityId]))
        {
            var blockPlan = plan.Blocks[block.EntityId];
            Console.WriteLine(
                $"  {(blockPlan.ShowInTerminal ? ' ' : 'T')}{(blockPlan.ShowInInventory ? ' ' : 'I')} {blockPlan.Name, -45} <= {block.CurrentName}"
            );
        }

        foreach (var group in plan.Groups)
            Console.WriteLine($"  GROUP {group.Name} ({group.BlockIds.Count})");

        foreach (var slot in plan.CockpitSlots.OrderBy(s => s.Index))
            Console.WriteLine(
                $"  SLOT {slot.Index / 9 + 1}.{slot.Index % 9 + 1} {slot.GroupName ?? names[slot.BlockId]} {slot.Action}"
            );

        var loose = ship.Blocks.Count(b =>
            plan.Blocks[b.EntityId].ShowInTerminal
            && !plan.Groups.Any(g => g.BlockIds.Contains(b.EntityId))
        );
        Console.WriteLine($"  visible in the terminal and in no group: {loose}");
    }
}

// Reads the first grid of a blueprint into the planner's model, with the block definitions
// and English texts of the game. Conveyor connections are not known here.
static class Blueprint
{
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private static readonly Dictionary<string, (int X, int Y, int Z)> Vectors = new()
    {
        { "Forward", (0, 0, -1) },
        { "Backward", (0, 0, 1) },
        { "Left", (-1, 0, 0) },
        { "Right", (1, 0, 0) },
        { "Up", (0, 1, 0) },
        { "Down", (0, -1, 0) },
    };

    public static ShipInfo Load(string contentData, string blueprintFolder)
    {
        var texts = XDocument
            .Load(Path.Combine(contentData, "Localization", "MyTexts.resx"))
            .Root.Elements("data")
            .ToDictionary(e => (string)e.Attribute("name"), e => (string)e.Element("value"));
        var definitions = new Dictionary<string, XElement>();
        foreach (var path in Directory.GetFiles(Path.Combine(contentData, "CubeBlocks"), "*.sbc"))
        foreach (var definition in XDocument.Load(path).Descendants("Definition"))
            definitions[
                Strip((string)definition.Element("Id")?.Element("TypeId"))
                    + "/"
                    + (string)definition.Element("Id")?.Element("SubtypeId")
            ] = definition;

        var grid = XDocument
            .Load(Path.Combine(blueprintFolder, "bp.sbc"))
            .Descendants("CubeGrid")
            .First();
        var ship = new ShipInfo { ShipName = (string)grid.Element("DisplayName") };
        var orientations =
            new Dictionary<BlockInfo, ((int X, int Y, int Z) Forward, (int X, int Y, int Z) Up)>();
        foreach (var block in grid.Element("CubeBlocks").Elements())
        {
            if (block.Element("CustomName") == null && block.Element("ShowInTerminal") == null)
                continue;

            var type = Strip((string)block.Attribute(Xsi + "type"));
            var subtype = (string)block.Element("SubtypeName") ?? "";
            definitions.TryGetValue(type + "/" + subtype, out var definition);
            var displayName = (string)definition?.Element("DisplayName") ?? type;
            var gas = (string)definition?.Element("StoredGasId")?.Element("SubtypeId");
            var info = new BlockInfo
            {
                EntityId = (long?)block.Element("EntityId") ?? 0,
                TypeId = type,
                SubtypeId = subtype,
                DefaultName = texts.GetValueOrDefault(displayName, displayName),
                CurrentName = (string)block.Element("CustomName"),
                Kind = Naming.Classify(
                    type,
                    subtype,
                    (bool?)definition?.Element("EnableShipControl") ?? true
                ),
                ThrusterType = (string)definition?.Element("ThrusterType"),
                GasType = gas,
                IsMainCockpit = (bool?)block.Element("IsMainCockpit") ?? false,
                HasInventory =
                    block.Element("ComponentContainer")?.ToString().Contains("MyInventoryBase")
                    ?? false,
                X = (int?)block.Element("Min")?.Attribute("x") ?? 0,
                Y = (int?)block.Element("Min")?.Attribute("y") ?? 0,
                Z = (int?)block.Element("Min")?.Attribute("z") ?? 0,
            };
            info.IsRepairProjector =
                info.Kind == BlockKind.Projector
                && (info.CurrentName ?? "").IndexOf("repair", StringComparison.OrdinalIgnoreCase)
                    >= 0;
            var orientation = block.Element("BlockOrientation");
            orientations[info] = (
                Vectors[(string)orientation?.Attribute("Forward") ?? "Forward"],
                Vectors[(string)orientation?.Attribute("Up") ?? "Up"]
            );
            ship.Blocks.Add(info);
        }

        var cockpit = ship
            .Blocks.Where(b => b.Kind == BlockKind.Cockpit)
            .OrderByDescending(b => b.IsMainCockpit)
            .FirstOrDefault();
        if (cockpit == null)
            return ship;

        cockpit.IsMainCockpit = true;
        var (f, u) = orientations[cockpit];
        var r = (X: f.Y * u.Z - f.Z * u.Y, Y: f.Z * u.X - f.X * u.Z, Z: f.X * u.Y - f.Y * u.X);
        var origin = (cockpit.X, cockpit.Y, cockpit.Z);
        foreach (var block in ship.Blocks)
        {
            var (forward, up) = orientations[block];
            var facing =
                block.Kind == BlockKind.Thruster ? (-forward.X, -forward.Y, -forward.Z)
                : block.Kind == BlockKind.LandingGear ? (-up.X, -up.Y, -up.Z)
                : forward;
            if (
                block.Kind
                is BlockKind.Thruster
                    or BlockKind.Camera
                    or BlockKind.Spotlight
                    or BlockKind.Connector
                    or BlockKind.LandingGear
            )
                block.Direction = Naming.DirectionOf(
                    Dot(facing, r),
                    Dot(facing, u),
                    -Dot(facing, f)
                );

            var p = (block.X - origin.X, block.Y - origin.Y, block.Z - origin.Z);
            (block.X, block.Y, block.Z) = (Dot(p, r), Dot(p, u), -Dot(p, f));
        }

        return ship;
    }

    private static int Dot((int X, int Y, int Z) a, (int X, int Y, int Z) b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static string Strip(string typeId) => (typeId ?? "").Replace("MyObjectBuilder_", "");
}
