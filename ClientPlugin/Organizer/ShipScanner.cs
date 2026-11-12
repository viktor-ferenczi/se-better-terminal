using System;
using System.Collections.Generic;
using System.Linq;
using ClientPlugin.Extensions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Game.ModAPI;
using VRageMath;
using IMyInventory = VRage.Game.ModAPI.Ingame.IMyInventory;

namespace ClientPlugin.Organizer
{
    // Reads a ship in the game into the planner's model
    public class ShipScanner
    {
        public readonly ShipInfo Ship = new ShipInfo();
        public readonly Dictionary<long, MyTerminalBlock> Blocks =
            new Dictionary<long, MyTerminalBlock>();

        // All grids of the ship: the mechanically connected ones, the largest first.
        // Ships docked by connectors are not part of it.
        public readonly List<MyCubeGrid> Grids;

        public MyCubeGrid MainGrid => Grids[0];

        public ShipScanner(MyCubeGrid grid)
        {
            Grids = GetGrids(grid);
            Ship.ShipName = MainGrid.GetSafeName();

            // Only the blocks the player may change, the server refuses the changes of the rest
            var blocks = Grids
                .SelectMany(g => g.GetFatBlocks<MyTerminalBlock>())
                .Where(b => b.HasLocalPlayerAccess())
                .ToList();
            var cockpit = FindMainCockpit(blocks);
            var frame = cockpit?.WorldMatrix ?? MainGrid.WorldMatrix;
            var storage = FindMainStorage(blocks, cockpit);
            var repairProjector = FindRepairProjector(blocks);

            foreach (var block in blocks)
            {
                var id = block.BlockDefinition.Id;
                var typeId = id.TypeId.ToString().Replace("MyObjectBuilder_", "");
                var canControlShip =
                    (block.BlockDefinition as MyShipControllerDefinition)?.EnableShipControl
                    ?? true;
                var position =
                    (block.PositionComp.GetPosition() - frame.Translation) / MainGrid.GridSize;
                var info = new BlockInfo
                {
                    EntityId = block.EntityId,
                    TypeId = typeId,
                    SubtypeId = id.SubtypeName,
                    DefaultName = block.BlockDefinition.DisplayNameText,
                    CurrentName = block.CustomName.ToString(),
                    Kind = Naming.Classify(typeId, id.SubtypeName, canControlShip),
                    ThrusterType = (block as MyThrust)?.BlockDefinition.ThrusterType.String,
                    GasType = (block.BlockDefinition as MyGasTankDefinition)
                        ?.StoredGasId
                        .SubtypeName,
                    X = (int)Math.Round(position.Dot(frame.Right)),
                    Y = (int)Math.Round(position.Dot(frame.Up)),
                    Z = (int)Math.Round(position.Dot(frame.Backward)),
                    HasInventory = block.HasInventory,
                    ConveyorConnected = IsConnected(block, storage),
                    IsMainCockpit = block == cockpit,
                    IsRepairProjector = block == repairProjector,
                };

                Vector3D? facing = null;
                switch (info.Kind)
                {
                    case BlockKind.Thruster:
                        // The flame points forward, the ship is pushed the other way
                        facing = block.WorldMatrix.Backward;
                        break;
                    case BlockKind.LandingGear:
                        facing = block.WorldMatrix.Down;
                        break;
                    case BlockKind.Camera:
                    case BlockKind.Spotlight:
                    case BlockKind.Connector:
                        facing = block.WorldMatrix.Forward;
                        break;
                }

                if (facing.HasValue && cockpit != null)
                    info.Direction = Naming.DirectionOf(
                        facing.Value.Dot(frame.Right),
                        facing.Value.Dot(frame.Up),
                        facing.Value.Dot(frame.Backward)
                    );

                Ship.Blocks.Add(info);
                Blocks[block.EntityId] = block;
            }
        }

        public static List<MyCubeGrid> GetGrids(MyCubeGrid grid)
        {
            return MyCubeGridGroups
                .Static.GetGroups(GridLinkTypeEnum.Mechanical)
                .GetGroupNodes(grid)
                .OrderByDescending(g => g.BlocksCount)
                .ToList();
        }

        private MyShipController FindMainCockpit(List<MyTerminalBlock> blocks)
        {
            var cockpits = blocks
                .OfType<MyShipController>()
                .Where(c => !(c is MyRemoteControl) && c.BlockDefinition.EnableShipControl)
                .ToList();
            // A drone has no cockpit, the sides of the ship are those of its remote control then
            return cockpits.FirstOrDefault(c => c.IsMainCockpit)
                ?? cockpits.FirstOrDefault(c => c.CubeGrid == MainGrid)
                ?? cockpits.FirstOrDefault()
                ?? blocks.OfType<MyRemoteControl>().FirstOrDefault();
        }

        // The inventory standing for the ship's main conveyor network: its largest cargo container
        private static MyInventory FindMainStorage(
            List<MyTerminalBlock> blocks,
            MyShipController cockpit
        )
        {
            var cargo = blocks
                .OfType<MyCargoContainer>()
                .Where(c => c.HasInventory)
                .OrderByDescending(c => (double)c.GetInventory().MaxVolume)
                .FirstOrDefault();
            if (cargo != null)
                return cargo.GetInventory();

            return cockpit != null && cockpit.HasInventory ? cockpit.GetInventory() : null;
        }

        private static bool IsConnected(MyTerminalBlock block, MyInventory storage)
        {
            if (storage == null || !block.HasInventory)
                return true;

            var inventory = block.GetInventory();
            return inventory == storage || ((IMyInventory)inventory).IsConnectedTo(storage);
        }

        // The repair projector projects the ship itself: most of its blueprint is already built.
        // A projector named so by the player counts too.
        private static MyProjectorBase FindRepairProjector(List<MyTerminalBlock> blocks)
        {
            var projectors = blocks.OfType<MyProjectorBase>().ToList();
            return projectors.FirstOrDefault(p =>
                    p.CustomName.ToString().IndexOf("repair", StringComparison.OrdinalIgnoreCase)
                    >= 0
                )
                ?? projectors.FirstOrDefault(p =>
                    p.m_totalBlocks > 0 && p.m_remainingBlocks * 2 < p.m_totalBlocks
                );
        }
    }
}
