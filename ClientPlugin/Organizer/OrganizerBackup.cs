using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ClientPlugin.Extensions;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Screens.Helpers;
using VRage.FileSystem;
using VRage.Game;
using VRage.ObjectBuilders.Private;

namespace ClientPlugin.Organizer
{
    // What the organizer may change on a ship, as it was before. Saved into a file
    // before any change, so the ship can be reverted even after restarting the game.
    public class OrganizerBackup
    {
        public class Block
        {
            [XmlAttribute]
            public long Id;

            [XmlAttribute]
            public string Name;

            [XmlAttribute]
            public bool ShowInTerminal;

            [XmlAttribute]
            public bool ShowInInventory;
        }

        public class Group
        {
            [XmlAttribute]
            public string Name;
            public List<long> BlockIds = new List<long>();
        }

        public class Toolbar
        {
            [XmlAttribute]
            public long BlockId;
            public string Xml; // MyObjectBuilder_Toolbar
        }

        public long GridId;
        public string GridName;
        public DateTime CreatedUtc;
        public List<Block> Blocks = new List<Block>();

        // All the groups of the ship before the change, and the names of those the organizer added
        public List<Group> Groups = new List<Group>();
        public List<string> AddedGroups = new List<string>();

        public List<Toolbar> Toolbars = new List<Toolbar>();

        // The blueprint the repair projector had is saved next to this file, with the .sbc extension
        public long ProjectorId;
        public int ProjectorOffsetX,
            ProjectorOffsetY,
            ProjectorOffsetZ;
        public int ProjectorRotationX,
            ProjectorRotationY,
            ProjectorRotationZ;

        [XmlIgnore]
        public string Path;

        private static string Folder =>
            System.IO.Path.Combine(MyFileSystem.UserDataPath, "Storage", Plugin.Name, "Backups");

        public string ProjectorBlueprintPath => System.IO.Path.ChangeExtension(Path, ".sbc");

        public static OrganizerBackup Capture(
            ShipScanner scanner,
            MyGridTerminalSystem terminalSystem,
            OrganizerPlan plan
        )
        {
            var backup = new OrganizerBackup
            {
                GridId = scanner.MainGrid.EntityId,
                GridName = scanner.MainGrid.GetSafeName(),
                CreatedUtc = DateTime.UtcNow,
            };
            backup.Path = System.IO.Path.Combine(
                Folder,
                $"{backup.GridId}-{backup.CreatedUtc:yyyyMMdd-HHmmss}.xml"
            );

            foreach (var block in scanner.Blocks.Values)
            {
                backup.Blocks.Add(
                    new Block
                    {
                        Id = block.EntityId,
                        Name = block.CustomName.ToString(),
                        ShowInTerminal = block.ShowInTerminal,
                        ShowInInventory = block.ShowInInventory,
                    }
                );
            }

            foreach (var group in terminalSystem.BlockGroups)
                backup.Groups.Add(
                    new Group
                    {
                        Name = group.Name.ToString(),
                        BlockIds = group.Blocks.Select(b => b.EntityId).ToList(),
                    }
                );

            var existing = new HashSet<string>(backup.Groups.Select(g => g.Name));
            backup.AddedGroups.AddRange(
                plan.Groups.Select(g => g.Name).Where(name => !existing.Contains(name))
            );

            foreach (var id in new[] { plan.CockpitId, plan.RemoteControlId })
            {
                if (id != 0)
                    backup.Toolbars.Add(
                        new Toolbar
                        {
                            BlockId = id,
                            Xml = Serialize(scanner.Blocks[id].GetToolbar().GetObjectBuilder()),
                        }
                    );
            }

            return backup;
        }

        // Call before the projector gets its new blueprint
        public void CaptureProjector(MyProjectorBase projector)
        {
            ProjectorId = projector.EntityId;
            ProjectorOffsetX = projector.ProjectionOffset.X;
            ProjectorOffsetY = projector.ProjectionOffset.Y;
            ProjectorOffsetZ = projector.ProjectionOffset.Z;
            ProjectorRotationX = projector.ProjectionRotation.X;
            ProjectorRotationY = projector.ProjectionRotation.Y;
            ProjectorRotationZ = projector.ProjectionRotation.Z;
            var grids = projector.m_originalGridBuilders ?? projector.m_savedProjections;
            if (grids == null || grids.Count == 0)
                return;

            var blueprint =
                MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();
            blueprint.Id = new MyDefinitionId(
                typeof(MyObjectBuilder_ShipBlueprintDefinition),
                GridName
            );
            blueprint.CubeGrids = grids.ToArray();
            var definitions =
                MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_Definitions>();
            definitions.ShipBlueprints = new[] { blueprint };
            Directory.CreateDirectory(Folder);
            MyObjectBuilderSerializerKeen.SerializeXML(ProjectorBlueprintPath, false, definitions);
        }

        public List<MyObjectBuilder_CubeGrid> LoadProjectorBlueprint()
        {
            if (
                !File.Exists(ProjectorBlueprintPath)
                || !MyObjectBuilderSerializerKeen.DeserializeXML(
                    ProjectorBlueprintPath,
                    out MyObjectBuilder_Definitions definitions
                )
            )
                return null;

            return definitions
                .ShipBlueprints?.SelectMany(b => b.CubeGrids ?? new MyObjectBuilder_CubeGrid[0])
                .ToList();
        }

        public void Save()
        {
            Directory.CreateDirectory(Folder);
            using (var writer = File.CreateText(Path))
                new XmlSerializer(typeof(OrganizerBackup)).Serialize(writer, this);
        }

        // Path of the latest backup of the ship, null if it has none
        public static string FindLatest(IEnumerable<long> gridIds)
        {
            if (!Directory.Exists(Folder))
                return null;

            // The file names are the grid ID and the UTC time, the latter sorts by age
            return gridIds
                .SelectMany(id => Directory.GetFiles(Folder, $"{id}-*.xml"))
                .OrderByDescending(path =>
                {
                    var name = System.IO.Path.GetFileName(path);
                    return name.Substring(name.IndexOf('-'));
                })
                .FirstOrDefault();
        }

        public static OrganizerBackup LoadLatest(IEnumerable<long> gridIds)
        {
            var path = FindLatest(gridIds);
            if (path == null)
                return null;

            using (var reader = File.OpenText(path))
            {
                var backup = (OrganizerBackup)
                    new XmlSerializer(typeof(OrganizerBackup)).Deserialize(reader);
                backup.Path = path;
                return backup;
            }
        }

        // A reverted backup is done with
        public void Delete()
        {
            File.Delete(Path);
            File.Delete(ProjectorBlueprintPath);
        }

        private static string Serialize(MyObjectBuilder_Toolbar toolbar)
        {
            using (var stream = new MemoryStream())
            {
                MyObjectBuilderSerializerKeen.SerializeXML(stream, toolbar);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static MyObjectBuilder_Toolbar DeserializeToolbar(string xml)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                return MyObjectBuilderSerializerKeen.DeserializeXML(
                    stream,
                    out MyObjectBuilder_Toolbar toolbar
                )
                    ? toolbar
                    : null;
        }
    }
}
