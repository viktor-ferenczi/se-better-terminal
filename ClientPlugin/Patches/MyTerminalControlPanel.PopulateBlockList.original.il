// c3da4457
ldarg.0
call Sandbox.Game.GameSystems.MyGridTerminalSystem Sandbox.Game.Gui.MyTerminalControlPanel::get_TerminalSystem()
brtrue.s L0
ret
L0:
ldarg.0
call Sandbox.Game.GameSystems.MyGridTerminalSystem Sandbox.Game.Gui.MyTerminalControlPanel::get_TerminalSystem()
callvirt System.Collections.Generic.List`1<Sandbox.Game.GameSystems.MyBlockGroup> Sandbox.Game.GameSystems.MyGridTerminalSystem::get_BlockGroups()
brtrue.s L1
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "m_terminalSystem.BlockGroups is null"
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
L1:
ldarg.0
call Sandbox.Game.GameSystems.MyGridTerminalSystem Sandbox.Game.Gui.MyTerminalControlPanel::get_TerminalSystem()
callvirt VRage.Collections.HashSetReader`1<Sandbox.Game.Entities.Cube.MyTerminalBlock> Sandbox.Game.GameSystems.MyGridTerminalSystem::get_Blocks()
stloc.0
ldloca.s 0 (VRage.Collections.HashSetReader`1[Sandbox.Game.Entities.Cube.MyTerminalBlock])
call System.Boolean VRage.Collections.HashSetReader`1<Sandbox.Game.Entities.Cube.MyTerminalBlock>::get_IsValid()
brtrue.s L2
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "m_terminalSystem.Blocks.IsValid is false"
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
L2:
ldarg.0
call System.Collections.Generic.HashSet`1<Sandbox.Game.Entities.Cube.MyTerminalBlock> Sandbox.Game.Gui.MyTerminalControlPanel::get_CurrentBlocks()
brtrue.s L3
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "CurrentBlocks is null"
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
L3:
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlListbox Sandbox.Game.Gui.MyTerminalControlPanel::m_blockListbox
brtrue.s L4
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "m_blockListbox is null"
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
L4:
ldarg.0
call Sandbox.Game.GameSystems.MyGridTerminalSystem Sandbox.Game.Gui.MyTerminalControlPanel::get_TerminalSystem()
callvirt System.Collections.Generic.List`1<Sandbox.Game.GameSystems.MyBlockGroup> Sandbox.Game.GameSystems.MyGridTerminalSystem::get_BlockGroups()
callvirt Sandbox.Game.GameSystems.MyBlockGroup[] System.Collections.Generic.List`1<Sandbox.Game.GameSystems.MyBlockGroup>::ToArray()
dup
ldsfld Sandbox.Game.Screens.Helpers.MyTerminalComparer Sandbox.Game.Screens.Helpers.MyTerminalComparer::Static
call static System.Void System.Array::Sort(Sandbox.Game.GameSystems.MyBlockGroup[] array, System.Collections.Generic.IComparer`1<Sandbox.Game.GameSystems.MyBlockGroup> comparer)
stloc.1
ldc.i4.0
stloc.2
br.s L5
L6:
ldloc.1
ldloc.2
ldelem.ref
stloc.3
ldarg.0
ldloc.3
ldloca.s 4 (System.Nullable`1[System.Int32])
initobj System.Nullable`1[System.Int32]
ldloc.s 4 (System.Nullable`1[System.Int32])
call System.Void Sandbox.Game.Gui.MyTerminalControlPanel::AddGroupToList(Sandbox.Game.GameSystems.MyBlockGroup group, System.Nullable`1<System.Int32> position)
ldloc.2
ldc.i4.1
add
stloc.2
L5:
ldloc.2
ldloc.1
ldlen
conv.i4
blt.s L6
ldarg.0
call Sandbox.Game.GameSystems.MyGridTerminalSystem Sandbox.Game.Gui.MyTerminalControlPanel::get_TerminalSystem()
callvirt VRage.Collections.HashSetReader`1<Sandbox.Game.Entities.Cube.MyTerminalBlock> Sandbox.Game.GameSystems.MyGridTerminalSystem::get_Blocks()
stloc.0
ldloca.s 0 (VRage.Collections.HashSetReader`1[Sandbox.Game.Entities.Cube.MyTerminalBlock])
call Sandbox.Game.Entities.Cube.MyTerminalBlock[] VRage.Collections.HashSetReader`1<Sandbox.Game.Entities.Cube.MyTerminalBlock>::ToArray()
dup
ldsfld Sandbox.Game.Screens.Helpers.MyTerminalComparer Sandbox.Game.Screens.Helpers.MyTerminalComparer::Static
call static System.Void System.Array::Sort(Sandbox.Game.Entities.Cube.MyTerminalBlock[] array, System.Collections.Generic.IComparer`1<Sandbox.Game.Entities.Cube.MyTerminalBlock> comparer)
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlListbox Sandbox.Game.Gui.MyTerminalControlPanel::m_blockListbox
ldfld System.Collections.Generic.List`1<Sandbox.Graphics.GUI.Item> Sandbox.Graphics.GUI.MyGuiControlListbox::SelectedItems
callvirt virtual System.Void System.Collections.Generic.List`1<Sandbox.Graphics.GUI.Item>::Clear()
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlListbox Sandbox.Game.Gui.MyTerminalControlPanel::m_blockListbox
ldc.i4.1
callvirt System.Void Sandbox.Graphics.GUI.MyGuiControlListbox::set_IsInBulkInsert(System.Boolean value)
stloc.s 5 (Sandbox.Game.Entities.Cube.MyTerminalBlock[])
ldc.i4.0
stloc.2
br.s L7
L11:
ldloc.s 5 (Sandbox.Game.Entities.Cube.MyTerminalBlock[])
ldloc.2
ldelem.ref
stloc.s 6 (Sandbox.Game.Entities.Cube.MyTerminalBlock)
ldarg.0
ldloc.s 6 (Sandbox.Game.Entities.Cube.MyTerminalBlock)
ldloc.s 6 (Sandbox.Game.Entities.Cube.MyTerminalBlock)
ldarg.0
ldfld Sandbox.Game.Entities.Cube.MyTerminalBlock Sandbox.Game.Gui.MyTerminalControlPanel::m_originalBlock
beq.s L8
ldloc.s 6 (Sandbox.Game.Entities.Cube.MyTerminalBlock)
callvirt virtual System.Boolean Sandbox.Game.Entities.Cube.MyTerminalBlock::get_ShowInTerminal()
brtrue.s L9
ldsfld System.Boolean Sandbox.Game.Gui.MyTerminalControlPanel::m_showAllTerminalBlocks
br.s L10
L8:
L9:
ldc.i4.1
L10:
newobj System.Void System.Nullable`1<System.Boolean>::.ctor(System.Boolean value)
call Sandbox.Graphics.GUI.Item Sandbox.Game.Gui.MyTerminalControlPanel::AddBlockToList(Sandbox.Game.Entities.Cube.MyTerminalBlock block, System.Nullable`1<System.Boolean> visibility)
pop
ldloc.2
ldc.i4.1
add
stloc.2
L7:
ldloc.2
ldloc.s 5 (Sandbox.Game.Entities.Cube.MyTerminalBlock[])
ldlen
conv.i4
blt.s L11
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlListbox Sandbox.Game.Gui.MyTerminalControlPanel::m_blockListbox
ldc.i4.0
callvirt System.Void Sandbox.Graphics.GUI.MyGuiControlListbox::set_IsInBulkInsert(System.Boolean value)
ldarg.1
brtrue.s L12
ldarg.0
call System.Collections.Generic.HashSet`1<Sandbox.Game.Entities.Cube.MyTerminalBlock> Sandbox.Game.Gui.MyTerminalControlPanel::get_CurrentBlocks()
callvirt virtual System.Int32 System.Collections.Generic.HashSet`1<Sandbox.Game.Entities.Cube.MyTerminalBlock>::get_Count()
ldc.i4.0
ble.s L13
ldarg.0
call System.Void Sandbox.Game.Gui.MyTerminalControlPanel::SelectBlocks()
ret
L13:
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlListbox Sandbox.Game.Gui.MyTerminalControlPanel::m_blockListbox
ldfld VRage.Collections.ObservableCollection`1<Sandbox.Graphics.GUI.Item> Sandbox.Graphics.GUI.MyGuiControlListbox::Items
callvirt VRage.Collections.Enumerator<Sandbox.Graphics.GUI.Item> VRage.Collections.ObservableCollection`1<Sandbox.Graphics.GUI.Item>::GetEnumerator()
stloc.s 7 (VRage.Collections.ObservableCollection`1+Enumerator[Sandbox.Graphics.GUI.MyGuiControlListbox+Item])
[EX_BeginException]
br.s L14
L17:
ldloca.s 7 (VRage.Collections.ObservableCollection`1+Enumerator[Sandbox.Graphics.GUI.MyGuiControlListbox+Item])
call virtual Sandbox.Graphics.GUI.Item VRage.Collections.Enumerator<Sandbox.Graphics.GUI.Item>::get_Current()
stloc.s 8 (Sandbox.Graphics.GUI.MyGuiControlListbox+Item)
ldloc.s 8 (Sandbox.Graphics.GUI.MyGuiControlListbox+Item)
ldfld System.Object Sandbox.Graphics.GUI.Item::UserData
isinst Sandbox.Game.Entities.Cube.MyTerminalBlock
brfalse.s L15
ldarg.0
ldc.i4.1
newarr Sandbox.Game.Entities.Cube.MyTerminalBlock
dup
ldc.i4.0
ldloc.s 8 (Sandbox.Graphics.GUI.MyGuiControlListbox+Item)
ldfld System.Object Sandbox.Graphics.GUI.Item::UserData
castclass Sandbox.Game.Entities.Cube.MyTerminalBlock
stelem.ref
call System.Void Sandbox.Game.Gui.MyTerminalControlPanel::SelectBlocks(Sandbox.Game.Entities.Cube.MyTerminalBlock[] blocks)
leave.s L16
L14:
L15:
ldloca.s 7 (VRage.Collections.ObservableCollection`1+Enumerator[Sandbox.Graphics.GUI.MyGuiControlListbox+Item])
call virtual System.Boolean VRage.Collections.Enumerator<Sandbox.Graphics.GUI.Item>::MoveNext()
brtrue.s L17
leave.s L18
[EX_BeginFinally]
ldloca.s 7 (VRage.Collections.ObservableCollection`1+Enumerator[Sandbox.Graphics.GUI.MyGuiControlListbox+Item])
constrained. VRage.Collections.ObservableCollection`1+Enumerator[Sandbox.Graphics.GUI.MyGuiControlListbox+Item]
callvirt abstract virtual System.Void System.IDisposable::Dispose()
[EX_EndException]
endfinally
L12:
ldarg.0
ldarg.1
call System.Void Sandbox.Game.Gui.MyTerminalControlPanel::SelectBlocks(Sandbox.Game.Entities.Cube.MyTerminalBlock[] blocks)
L16:
L18:
ret
