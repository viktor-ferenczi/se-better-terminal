using System;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace ClientPlugin.Organizer
{
    // The ways the player starts the organizer: the terminal's block context menu
    // (with a confirmation) and the /bt chat command (without one).
    public static class OrganizerCommands
    {
        private const string Caption = "Better Terminal: Ship Organizer";
        private const string Usage =
            "/bt organize | /bt revert - for the ship you control, or add a grid name or entity ID";

        private static MySession hookedSession;

        // Called on every frame. The chat event handlers do not survive a session, hook each one.
        public static void Update()
        {
            var session = MySession.Static;
            if (session == hookedSession)
                return;

            hookedSession = session;
            if (session == null)
                return;

            IMyUtilities utilities = MyAPIUtilities.Static;
            utilities.MessageEntered -= OnMessageEntered;
            utilities.MessageEntered += OnMessageEntered;
        }

        public static void ConfirmOrganize(MyCubeGrid grid)
        {
            Confirm(
                "Rename, group and hide the blocks of this ship, set up the toolbar of its main cockpit and remote control, then reload its repair projector?\n\n"
                    + "A backup is saved first. Revert Organizing in this menu restores the ship from it.",
                () => ShipOrganizer.Organize(grid)
            );
        }

        public static void ConfirmRevert(MyCubeGrid grid)
        {
            Confirm(
                "Restore the block names, visibility, groups, toolbars and the repair projector of this ship from the backup made before it was last organized?",
                () => ShipOrganizer.Revert(grid)
            );
        }

        private static void Confirm(string question, Func<string> action)
        {
            MyGuiSandbox.AddScreen(
                MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder(question),
                    messageCaption: new StringBuilder(Caption),
                    callback: result =>
                    {
                        if (result != MyGuiScreenMessageBox.ResultEnum.YES)
                            return;

                        MyGuiSandbox.AddScreen(
                            MyGuiSandbox.CreateMessageBox(
                                messageText: new StringBuilder(Run(action)),
                                messageCaption: new StringBuilder(Caption)
                            )
                        );
                    }
                )
            );
        }

        private static string Run(Func<string> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                MyLog.Default.Error($"{Plugin.Name}: Ship organizer failed: {e}");
                return "Failed, see the game log for the details: " + e.Message;
            }
        }

        private static void OnMessageEntered(string text, ref bool sendToOthers)
        {
            if (
                !Config.Current.EnableShipOrganizer
                || text == null
                || !(text == "/bt" || text.StartsWith("/bt "))
            )
                return;

            sendToOthers = false;
            var args = text.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            var command = args.Length > 1 ? args[1] : "";
            if (command != "organize" && command != "revert")
            {
                Show(Usage);
                return;
            }

            var grid = FindGrid(args.Length > 2 ? args[2] : null);
            if (grid == null)
            {
                Show("No such ship. " + Usage);
                return;
            }

            Show(
                Run(() =>
                    command == "organize"
                        ? ShipOrganizer.Organize(grid)
                        : ShipOrganizer.Revert(grid)
                )
            );
        }

        private static MyCubeGrid FindGrid(string nameOrId)
        {
            if (nameOrId == null)
                return (MySession.Static.ControlledEntity as MyCubeBlock)?.CubeGrid;

            if (long.TryParse(nameOrId, out var id))
                return MyEntities.GetEntityById(id) as MyCubeGrid;

            foreach (MyEntity entity in MyEntities.GetEntities())
            {
                if (entity is MyCubeGrid grid && grid.DisplayName == nameOrId)
                    return grid;
            }

            return null;
        }

        private static void Show(string message)
        {
            MyAPIGateway.Utilities.ShowMessage("Better Terminal", message);
        }
    }
}
