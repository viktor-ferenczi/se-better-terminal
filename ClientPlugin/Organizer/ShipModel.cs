using System.Collections.Generic;

// Pure data model of the ship organizer. No game types here, so the planner
// can be tested outside the game on blueprint files (see the Tests folder).
namespace ClientPlugin.Organizer
{
    // Directions relative to the main cockpit
    public enum ShipDirection
    {
        None,
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom,
    }

    public enum BlockKind
    {
        Other,
        Thruster,
        Gyro,
        Battery,
        Light,
        Spotlight,
        Drill,
        Welder,
        Grinder,
        Wheel,
        Camera,
        Connector,
        Ejector,
        LandingGear,
        Cockpit,
        Seat,
        RemoteControl,
        Projector,
        JumpDrive,
        GasTank,
        GasGenerator,
        HydrogenEngine,
        Reactor,
        SolarPanel,
        WindTurbine,
        Cargo,
        Parachute,
        Turret,
        Gun,
        Decoy,
        Door,
        AirVent,
        Sorter,
        Collector,
        ButtonPanel,
    }

    public class BlockInfo
    {
        public long EntityId;
        public string TypeId; // Without the MyObjectBuilder_ prefix, like "Thrust"
        public string SubtypeId;
        public string DefaultName; // Display name of the block definition
        public string CurrentName;
        public BlockKind Kind;

        // Thruster type from the definition: Ion, Hydrogen, Atmospheric, Prototech
        public string ThrusterType;

        // Gas stored by a tank: Hydrogen or Oxygen
        public string GasType;

        // Thrusters: the direction the thruster accelerates the ship to.
        // Cameras, spotlights, connectors, landing gears: the direction they face.
        public ShipDirection Direction;

        // Position in the cockpit's frame in blocks: X right, Y up, Z back. Orders the numbering.
        public int X,
            Y,
            Z;

        public bool HasInventory;

        // Reachable from the ship's main conveyor network
        public bool ConveyorConnected = true;

        public bool IsMainCockpit;
        public bool IsRepairProjector;
    }

    public class ShipInfo
    {
        public string ShipName;
        public readonly List<BlockInfo> Blocks = new List<BlockInfo>();
    }

    public class BlockPlan
    {
        public string Name;
        public bool ShowInTerminal;
        public bool ShowInInventory;
    }

    public class GroupPlan
    {
        public string Name;
        public readonly List<long> BlockIds = new List<long>();
    }

    public class SlotPlan
    {
        public int Index; // page * 9 + slot, both from 0
        public string GroupName; // Either a group...
        public long BlockId; // ...or a single block
        public string Action;
    }

    public class OrganizerPlan
    {
        public readonly Dictionary<long, BlockPlan> Blocks = new Dictionary<long, BlockPlan>();
        public readonly List<GroupPlan> Groups = new List<GroupPlan>();

        public long CockpitId;
        public readonly List<SlotPlan> CockpitSlots = new List<SlotPlan>();

        // Toolbar pages (from 0) the organizer fills. All their slots are cleared first.
        public readonly HashSet<int> OwnedPages = new HashSet<int>();

        // Gets the cockpit toolbar copied as-is, the cockpit then gets a button to control it
        public long RemoteControlId;

        // Reloaded with the ship in its new state as the last step
        public long RepairProjectorId;
    }
}
