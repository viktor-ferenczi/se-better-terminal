using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using ITerminalAction = Sandbox.ModAPI.Interfaces.ITerminalAction;
using ITerminalProperty = Sandbox.ModAPI.Interfaces.ITerminalProperty;

namespace ClientPlugin.Logic
{
    public class ContextMenuLogic
    {
        private readonly MyTerminalControlPanel controlPanel;
        private MyGuiControlContextMenu contextMenu;

        // The screen controls collection to add the context menu to lazily.
        // We defer adding to ensure the context menu is on top of all other controls.
        private MyGuiControls deferredScreenControls;
        private bool addedToScreenControls;

        // Two-phase activation following the G menu pattern (MyGuiScreenToolbarConfigBase):
        // Phase 1 (right-click press): populate the menu and set Enabled = true
        // Phase 2 (right-click release): actually activate/show the menu
        private bool contextMenuPending;

        private enum ContextMenuAction
        {
            OpenInventory,
            ToggleUseInventory,
            ToggleShowInTerminal,
            ToggleOnOff,
            ToggleShowOnHud,
            InvokeTerminalAction,
            ToggleBoolProperty,
        }

        private class ContextMenuItemData
        {
            public ContextMenuAction Action;
            public MyTerminalBlock Block;
            public ITerminalAction TerminalAction;
            public ITerminalProperty<bool> BoolProperty;
        }

        // Action IDs already covered by existing menu items
        private static readonly HashSet<string> SkippedActionIds = new HashSet<string>
        {
            "OnOff", "OnOff_On", "OnOff_Off",
            "ShowOnHUD", "ShowOnHUD_On", "ShowOnHUD_Off",
            "UseConveyor",
            "PreserveAspectRatio",
            "Discharge_On", "Discharge_Off",
            "Recharge_On", "Recharge_Off",
            "SwitchLock",
        };

        // Bool property IDs already covered by existing menu items
        private static readonly HashSet<string> SkippedPropertyIds = new HashSet<string>
        {
            "ShowInTerminal",
            "ShowInToolbarConfig",
            "ShowOnHUD",
            "OnOff",
            "PreserveAspectRatio",
        };

        // Fix action display names: replace state labels with command verbs
        private static readonly Dictionary<string, string> ActionNameFixes = new Dictionary<string, string>
        {
            { "Closed", "Close" },
            { "Enabled", "Enable" },
            { "Disabled", "Disable" },
            { "Auto On/Off", "Auto" },
            { "Enable Auto", "Auto" },
            { "Discharge On/Off", "Discharge" },
            { "Recharge On/Off", "Recharge" },
        };

        // Property and action IDs that only make sense for blocks with inventory
        private static readonly HashSet<string> InventoryOnlyIds = new HashSet<string>
        {
            "ShowInInventory",
        };

        // Battery charge mode action IDs (mutually exclusive modes, not independent toggles)
        private static readonly HashSet<string> BatteryChargeModeIds = new HashSet<string>
        {
            "Auto",
            "Discharge",
            "Recharge",
        };

        // Connector lock action IDs (state-dependent, show only the relevant one)
        private static readonly HashSet<string> ConnectorLockActionIds = new HashSet<string>
        {
            "Lock",
            "Unlock",
        };

        // Reusable buffers to reduce allocations
        private readonly List<ITerminalAction> actionBuffer = new List<ITerminalAction>();
        private readonly List<ITerminalProperty> propertyBuffer = new List<ITerminalProperty>();
        private readonly HashSet<string> actionIds = new HashSet<string>();
        private readonly Dictionary<string, bool> boolPropertyValues = new Dictionary<string, bool>();

        public ContextMenuLogic(MyTerminalControlPanel controlPanel)
        {
            this.controlPanel = controlPanel;
        }

        public void CreateContextMenu(MyGuiControls screenControls)
        {
            if (contextMenu != null)
                return;

            contextMenu = new MyGuiControlContextMenu();
            contextMenu.ItemClicked += OnContextMenuItemClicked;
            contextMenu.Deactivate();

            // Defer adding to screen controls until activation.
            // During Init, the tab control hasn't been added yet,
            // so adding now would place the context menu behind it.
            deferredScreenControls = screenControls;
            addedToScreenControls = false;
        }

        public void PrepareContextMenu(MyTerminalBlock block)
        {
            if (contextMenu == null || block == null)
                return;

            contextMenu.CreateNewContextMenu();
            PopulateContextMenu(block);

            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Enabled = true;
                contextMenuPending = true;
            }
        }

        public void ActivateContextMenu(MyGuiScreenBase screen)
        {
            if (!contextMenuPending || contextMenu == null)
                return;

            // Lazily add to screen controls on first activation,
            // ensuring it's the last control (drawn on top of everything).
            if (!addedToScreenControls && deferredScreenControls != null)
            {
                deferredScreenControls.Add(contextMenu);
                addedToScreenControls = true;
            }

            contextMenuPending = false;
            contextMenu.Enabled = false;
            contextMenu.Activate(autoPositionOnMouseTip: true);
            screen.FocusedControl = contextMenu.GetInnerList();
        }

        public bool IsContextMenuActive => contextMenu != null && contextMenu.IsActiveControl;

        private void PopulateContextMenu(MyTerminalBlock block)
        {
            var modApiBlock = block as IMyTerminalBlock;
            if (modApiBlock == null)
                return;

            // Open Inventory (only for blocks with inventory)
            if (Config.Current.ContextMenuShowInventory && HasInventory(modApiBlock))
            {
                contextMenu.AddItem(
                    new StringBuilder("Open Inventory"),
                    "Switch to the inventory screen for this block",
                    userData: new ContextMenuItemData { Action = ContextMenuAction.OpenInventory, Block = block }
                );
            }

            // ON/OFF toggle (only for functional blocks)
            if (Config.Current.ContextMenuShowOnOff && modApiBlock is IMyFunctionalBlock functionalBlock)
            {
                var toggleText = functionalBlock.Enabled ? "Turn OFF" : "Turn ON";
                contextMenu.AddItem(
                    new StringBuilder(toggleText),
                    "Toggle this block ON or OFF",
                    userData: new ContextMenuItemData { Action = ContextMenuAction.ToggleOnOff, Block = block }
                );
            }

            // Terminal visibility toggle
            if (Config.Current.ContextMenuShowTerminal)
            {
                var toggleText = modApiBlock.ShowInTerminal ? "Hide from Terminal" : "Show in Terminal";
                contextMenu.AddItem(
                    new StringBuilder(toggleText),
                    "Toggle whether this block appears in the terminal list",
                    userData: new ContextMenuItemData { Action = ContextMenuAction.ToggleShowInTerminal, Block = block }
                );
            }

            // Show on HUD toggle
            if (Config.Current.ContextMenuShowHud)
            {
                var toggleText = modApiBlock.ShowOnHUD ? "Hide from HUD" : "Show on HUD";
                contextMenu.AddItem(
                    new StringBuilder(toggleText),
                    "Toggle whether this block is shown on the HUD",
                    userData: new ContextMenuItemData { Action = ContextMenuAction.ToggleShowOnHud, Block = block }
                );
            }

            // Use Conveyor toggle (only for blocks with conveyor system)
            if (Config.Current.ContextMenuShowInventory)
            {
                var useConveyor = GetInventoryUseConveyorState(modApiBlock);
                if (useConveyor.HasValue)
                {
                    var toggleText = useConveyor.Value ? "Disable Use Conveyor" : "Enable Use Conveyor";
                    contextMenu.AddItem(
                        new StringBuilder(toggleText),
                        "Toggle whether this block uses conveyor system for inventory",
                        userData: new ContextMenuItemData { Action = ContextMenuAction.ToggleUseInventory, Block = block }
                    );
                }
            }

            // Fetch bool property values for state-aware action filtering and toggles
            propertyBuffer.Clear();
            if (Config.Current.ContextMenuShowActions || Config.Current.ContextMenuShowToggles)
                modApiBlock.GetProperties(propertyBuffer);

            boolPropertyValues.Clear();
            foreach (var prop in propertyBuffer)
            {
                if (prop.TypeName != "Boolean")
                    continue;

                var boolProp = prop as ITerminalProperty<bool>;
                if (boolProp != null)
                    boolPropertyValues[prop.Id] = boolProp.GetValue(block);
            }

            // Dynamic terminal actions (sorted alphabetically by display name)
            if (Config.Current.ContextMenuShowActions)
            {
                actionBuffer.Clear();
                modApiBlock.GetActions(actionBuffer);

                // Collect action IDs for On/Off triplet detection (exclude already-skipped IDs)
                actionIds.Clear();
                foreach (var action in actionBuffer)
                {
                    if (!SkippedActionIds.Contains(action.Id))
                        actionIds.Add(action.Id);
                }

                var hasInventory = HasInventory(modApiBlock);

                // Detect battery current charge mode to show only non-active modes
                string currentChargeModeId = null;
                if (modApiBlock is IMyBatteryBlock battery)
                {
                    switch (battery.ChargeMode)
                    {
                        case Sandbox.ModAPI.Ingame.ChargeMode.Auto:
                            currentChargeModeId = "Auto";
                            break;
                        case Sandbox.ModAPI.Ingame.ChargeMode.Recharge:
                            currentChargeModeId = "Recharge";
                            break;
                        case Sandbox.ModAPI.Ingame.ChargeMode.Discharge:
                            currentChargeModeId = "Discharge";
                            break;
                    }
                }

                // Detect connector state to show only the relevant lock action
                string activeConnectorActionId = null;
                if (modApiBlock is IMyShipConnector connector)
                {
                    switch (connector.Status)
                    {
                        case Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected:
                            // Locked — only "Unlock" is relevant, hide "Lock"
                            activeConnectorActionId = "Lock";
                            break;
                        case Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connectable:
                            // In proximity — only "Lock" is relevant, hide "Unlock"
                            activeConnectorActionId = "Unlock";
                            break;
                        case Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Unconnected:
                            // Not near anything — hide both Lock and Unlock
                            activeConnectorActionId = "*";
                            break;
                    }
                }

                // Filter to eligible actions
                var filteredActions = new List<ITerminalAction>();
                foreach (var action in actionBuffer)
                {
                    if (SkippedActionIds.Contains(action.Id) || action.Name == null || action.Name.Length == 0)
                        continue;

                    if (action.Id.StartsWith("Increase") || action.Id.StartsWith("Decrease"))
                        continue;

                    if (!hasInventory && InventoryOnlyIds.Contains(action.Id))
                        continue;

                    // Battery charge modes: skip the currently active mode
                    if (currentChargeModeId != null && BatteryChargeModeIds.Contains(action.Id) && action.Id == currentChargeModeId)
                        continue;

                    // Connector lock actions: show only the action relevant to the current state
                    if (activeConnectorActionId != null && ConnectorLockActionIds.Contains(action.Id))
                    {
                        if (activeConnectorActionId == "*" || action.Id == activeConnectorActionId)
                            continue;
                    }

                    if (!ShouldShowAction(action.Id))
                        continue;

                    // Skip base toggle actions that have a corresponding bool property
                    // (the property is shown as a more informative toggle, e.g. "Disable Control Gyros"
                    // instead of the ambiguous "Control Gyros On/Off")
                    // Exception: battery charge modes and connector lock actions are handled separately
                    if (Config.Current.ContextMenuShowToggles &&
                        boolPropertyValues.ContainsKey(action.Id) &&
                        !BatteryChargeModeIds.Contains(action.Id) &&
                        !ConnectorLockActionIds.Contains(action.Id))
                        continue;

                    filteredActions.Add(action);
                }

                // Sort alphabetically by display name (case insensitive)
                filteredActions.Sort((a, b) => string.Compare(FixActionName(a.Name.ToString()), FixActionName(b.Name.ToString()), StringComparison.OrdinalIgnoreCase));

                foreach (var action in filteredActions)
                {
                    var actionName = FixActionName(action.Name.ToString());

                    contextMenu.AddItem(
                        new StringBuilder(actionName),
                        "",
                        userData: new ContextMenuItemData
                        {
                            Action = ContextMenuAction.InvokeTerminalAction,
                            Block = block,
                            TerminalAction = action,
                        }
                    );
                }
            }

            // Dynamic boolean property toggles (sorted alphabetically by display name)
            if (Config.Current.ContextMenuShowToggles)
            {
                var hasInventory = HasInventory(modApiBlock);
                var isBattery = modApiBlock is IMyBatteryBlock;
                var isConnector = modApiBlock is IMyShipConnector;

                // Filter to eligible bool properties
                var filteredToggles = new List<ITerminalProperty<bool>>();
                foreach (var prop in propertyBuffer)
                {
                    if (prop.TypeName != "Boolean" || SkippedPropertyIds.Contains(prop.Id))
                        continue;

                    if (!hasInventory && InventoryOnlyIds.Contains(prop.Id))
                        continue;

                    // Battery charge modes are handled as actions, not toggles
                    if (isBattery && BatteryChargeModeIds.Contains(prop.Id))
                        continue;

                    // Connector lock actions are handled as state-dependent actions, not toggles
                    if (isConnector && ConnectorLockActionIds.Contains(prop.Id))
                        continue;

                    // Skip bool properties that have corresponding _On/_Off actions
                    // (already shown in the actions section, showing them as toggles is redundant)
                    if (Config.Current.ContextMenuShowActions &&
                        (actionIds.Contains(prop.Id + "_On") || actionIds.Contains(prop.Id + "_Off")))
                        continue;

                    var boolProp = prop as ITerminalProperty<bool>;
                    if (boolProp == null)
                        continue;

                    filteredToggles.Add(boolProp);
                }

                // Sort alphabetically by formatted property name (case insensitive)
                filteredToggles.Sort((a, b) => string.Compare(FormatPropertyName(a.Id), FormatPropertyName(b.Id), StringComparison.OrdinalIgnoreCase));

                foreach (var boolProp in filteredToggles)
                {
                    var currentValue = boolPropertyValues.TryGetValue(boolProp.Id, out var val) && val;
                    var displayName = FormatPropertyName(boolProp.Id);
                    var itemText = currentValue ? $"Disable {displayName}" : $"Enable {displayName}";

                    contextMenu.AddItem(
                        new StringBuilder(itemText),
                        "",
                        userData: new ContextMenuItemData
                        {
                            Action = ContextMenuAction.ToggleBoolProperty,
                            Block = block,
                            BoolProperty = boolProp,
                        }
                    );
                }
            }
        }

        private static string FixActionName(string name)
        {
            name = name.Trim();
            return ActionNameFixes.TryGetValue(name, out var fix) ? fix : name;
        }

        // For On/Off action triplets (X, X_On, X_Off):
        // - Skip the toggle variant X if both X_On and X_Off exist
        // - Show X_On only when the property is currently off
        // - Show X_Off only when the property is currently on
        private bool ShouldShowAction(string id)
        {
            if (id.EndsWith("_On"))
            {
                var baseId = id.Substring(0, id.Length - 3);
                if (actionIds.Contains(baseId + "_Off"))
                {
                    // Has a paired _Off action, show only if currently off
                    if (boolPropertyValues.TryGetValue(baseId, out var isOn) && isOn)
                        return false;
                }
                return true;
            }

            if (id.EndsWith("_Off"))
            {
                var baseId = id.Substring(0, id.Length - 4);
                if (actionIds.Contains(baseId + "_On"))
                {
                    // Has a paired _On action, show only if currently on
                    if (boolPropertyValues.TryGetValue(baseId, out var isOn) && !isOn)
                        return false;
                }
                return true;
            }

            // Skip toggle action if both _On and _Off variants exist
            if (actionIds.Contains(id + "_On") && actionIds.Contains(id + "_Off"))
                return false;

            return true;
        }

        private static string FormatPropertyName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return id;

            var sb = new StringBuilder(id.Length + 4);
            for (var i = 0; i < id.Length; i++)
            {
                if (i > 0 && char.IsUpper(id[i]))
                {
                    var prevIsLower = char.IsLower(id[i - 1]);
                    var nextIsLower = i + 1 < id.Length && char.IsLower(id[i + 1]);
                    if (prevIsLower || nextIsLower)
                        sb.Append(' ');
                }
                sb.Append(id[i]);
            }
            return sb.ToString();
        }

        private void OnContextMenuItemClicked(MyGuiControlContextMenu menu, MyGuiControlContextMenu.EventArgs args)
        {
            if (args.UserData is not ContextMenuItemData itemData)
                return;

            var block = itemData.Block;
            var modApiBlock = block as IMyTerminalBlock;
            if (modApiBlock == null)
                return;

            switch (itemData.Action)
            {
                case ContextMenuAction.OpenInventory:
                    OpenInventory(block);
                    break;

                case ContextMenuAction.ToggleUseInventory:
                    ToggleUseInventory(modApiBlock);
                    break;

                case ContextMenuAction.ToggleShowInTerminal:
                    modApiBlock.ShowInTerminal = !modApiBlock.ShowInTerminal;
                    break;

                case ContextMenuAction.ToggleOnOff:
                    if (modApiBlock is IMyFunctionalBlock functionalBlock)
                        functionalBlock.Enabled = !functionalBlock.Enabled;
                    break;

                case ContextMenuAction.ToggleShowOnHud:
                    modApiBlock.ShowOnHUD = !modApiBlock.ShowOnHUD;
                    break;

                case ContextMenuAction.InvokeTerminalAction:
                    itemData.TerminalAction?.Apply(block);
                    break;

                case ContextMenuAction.ToggleBoolProperty:
                    if (itemData.BoolProperty != null)
                        itemData.BoolProperty.SetValue(block, !itemData.BoolProperty.GetValue(block));
                    break;
            }
        }

        public static void OpenInventory(MyTerminalBlock block)
        {
            if (!MyGuiScreenTerminal.IsOpen)
                return;

            MyGuiScreenTerminal.SwitchToInventory(block);

            // Switch the right inventory panel to Grid mode (connected inventories)
            // so the block's inventory is actually shown instead of the character inventory
            var controller = MyGuiScreenTerminal.m_instance?.m_controllerInventory;
            if (controller != null)
            {
                controller.m_rightTypeGroup.SelectedIndex = 1;
            }
        }

        private void ToggleUseInventory(IMyTerminalBlock block)
        {
            var useConveyor = GetInventoryUseConveyorState(block);
            if (useConveyor.HasValue)
            {
                SetInventoryUseConveyorState(block, !useConveyor.Value);
            }
        }

        private bool HasInventory(IMyTerminalBlock block)
        {
            return block.HasInventory && block.InventoryCount > 0;
        }

        private bool HasShowOnHud(IMyTerminalBlock block)
        {
            // All terminal blocks have ShowOnHUD property
            // Some specific types might not use it effectively, but the property always exists
            return true;
        }

        private bool? GetInventoryUseConveyorState(IMyTerminalBlock block)
        {
            // Try common interfaces that have UseConveyorSystem property
            if (block is IMyProductionBlock productionBlock)
                return productionBlock.UseConveyorSystem;
            
            if (block is IMyGasGenerator gasGenerator)
                return gasGenerator.UseConveyorSystem;
            
            if (block is IMyReactor reactor)
                return reactor.UseConveyorSystem;
            
            return null;
        }

        private void SetInventoryUseConveyorState(IMyTerminalBlock block, bool value)
        {
            // Set the property for common interfaces
            if (block is IMyProductionBlock productionBlock)
                productionBlock.UseConveyorSystem = value;
            else if (block is IMyGasGenerator gasGenerator)
                gasGenerator.UseConveyorSystem = value;
            else if (block is IMyReactor reactor)
                reactor.UseConveyorSystem = value;
        }

        public void Close()
        {
            if (contextMenu != null)
            {
                contextMenu.ItemClicked -= OnContextMenuItemClicked;
                contextMenu.Deactivate();
                contextMenu = null;
            }

            contextMenuPending = false;
        }
    }
}
