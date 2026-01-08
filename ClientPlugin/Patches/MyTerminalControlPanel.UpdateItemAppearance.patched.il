// fd3adc92
ldarg.2
callvirt System.Text.StringBuilder Sandbox.Graphics.GUI.Item::get_Text()
callvirt System.Text.StringBuilder System.Text.StringBuilder::Clear()
pop
ldarg.1
ldarg.2
call static System.Void ClientPlugin.Patches.MyTerminalControlPanelPatch::UpdateItemAppearance_DefaultNameImplementation(Sandbox.Game.Entities.Cube.MyTerminalBlock block, Sandbox.Graphics.GUI.Item item)
ldarg.1
callvirt virtual System.Boolean Sandbox.Game.Entities.MyCubeBlock::get_IsFunctional()
brtrue.s L0
ldarg.2
ldsfld VRageMath.Vector4 Sandbox.Game.Gui.MyTerminalControlPanel::RED_TEXT_COLOR
newobj System.Void System.Nullable`1<VRageMath.Vector4>::.ctor(VRageMath.Vector4 value)
stfld System.Nullable`1<VRageMath.Vector4> Sandbox.Graphics.GUI.Item::ColorMask
ldarg.2
callvirt System.Text.StringBuilder Sandbox.Graphics.GUI.Item::get_Text()
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Terminal_BlockIncomplete
call static System.Text.StringBuilder VRage.MyTexts::Get(VRage.Utils.MyStringId id)
call static System.Text.StringBuilder System.Text.StringBuilderExtensions_Format::AppendStringBuilder(System.Text.StringBuilder stringBuilder, System.Text.StringBuilder otherStringBuilder)
pop
ret
L0:
ldarg.0
ldfld Sandbox.Game.World.MyPlayer Sandbox.Game.Gui.MyTerminalControlPanel::m_controller
brfalse L1
ldarg.0
ldfld Sandbox.Game.World.MyPlayer Sandbox.Game.Gui.MyTerminalControlPanel::m_controller
callvirt Sandbox.Game.World.MyIdentity Sandbox.Game.World.MyPlayer::get_Identity()
brfalse L2
ldarg.1
ldarg.0
ldfld Sandbox.Game.World.MyPlayer Sandbox.Game.Gui.MyTerminalControlPanel::m_controller
callvirt Sandbox.Game.World.MyIdentity Sandbox.Game.World.MyPlayer::get_Identity()
callvirt System.Int64 Sandbox.Game.World.MyIdentity::get_IdentityId()
ldc.i4.0
callvirt Sandbox.Game.Entities.Cube.AccessRightsResult Sandbox.Game.Entities.Cube.MyTerminalBlock::HasPlayerAccessReason(System.Int64 identityId, VRage.Game.MyRelationsBetweenPlayerAndBlock defaultNoUser)
dup
stloc.0
brfalse L3
ldarg.2
ldsfld VRageMath.Vector4 Sandbox.Game.Gui.MyTerminalControlPanel::RED_TEXT_COLOR
newobj System.Void System.Nullable`1<VRageMath.Vector4>::.ctor(VRageMath.Vector4 value)
stfld System.Nullable`1<VRageMath.Vector4> Sandbox.Graphics.GUI.Item::ColorMask
ldloc.0
ldc.i4.2
bne.un.s L4
ldarg.1
callvirt Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.MyCubeBlock::get_BlockDefinition()
brfalse L5
ldarg.1
callvirt Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.MyCubeBlock::get_BlockDefinition()
callvirt System.String[] VRage.Game.MyDefinitionBase::get_DLCs()
brfalse L6
ldarg.1
callvirt Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.MyCubeBlock::get_BlockDefinition()
callvirt System.String[] VRage.Game.MyDefinitionBase::get_DLCs()
stloc.1
ldc.i4.0
stloc.2
br.s L7
L9:
ldloc.1
ldloc.2
ldelem.ref
ldloca.s 3 (VRage.Game.Definitions.MyDlcDefinition)
call static System.Boolean Sandbox.Game.MyDLCs::TryGetDLC(System.String name, VRage.Game.Definitions.MyDlcDefinition& dlc)
brfalse.s L8
ldarg.2
callvirt System.Text.StringBuilder Sandbox.Graphics.GUI.Item::get_Text()
ldstr " ("
callvirt System.Text.StringBuilder System.Text.StringBuilder::Append(System.String value)
ldsfld VRage.Utils.MyStringId MyCommonTexts::RequiresAnyDlc
call static System.Text.StringBuilder VRage.MyTexts::Get(VRage.Utils.MyStringId id)
callvirt System.Text.StringBuilder System.Text.StringBuilder::Append(System.Object value)
ldstr ")"
callvirt System.Text.StringBuilder System.Text.StringBuilder::Append(System.String value)
pop
L8:
ldloc.2
ldc.i4.1
add
stloc.2
L7:
ldloc.2
ldloc.1
ldlen
conv.i4
blt.s L9
ret
L4:
ldarg.2
callvirt System.Text.StringBuilder Sandbox.Graphics.GUI.Item::get_Text()
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Terminal_BlockAccessDenied
call static System.Text.StringBuilder VRage.MyTexts::Get(VRage.Utils.MyStringId id)
call static System.Text.StringBuilder System.Text.StringBuilderExtensions_Format::AppendStringBuilder(System.Text.StringBuilder stringBuilder, System.Text.StringBuilder otherStringBuilder)
pop
ret
L1:
L2:
L3:
ldarg.1
callvirt virtual System.Boolean Sandbox.Game.Entities.Cube.MyTerminalBlock::get_ShowInTerminal()
brtrue.s L10
ldarg.0
ldfld Sandbox.Game.Screens.Helpers.MyGridColorHelper Sandbox.Game.Gui.MyTerminalControlPanel::m_colorHelper
ldarg.1
callvirt Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyCubeBlock::get_CubeGrid()
callvirt System.Nullable`1<VRageMath.Color> Sandbox.Game.Screens.Helpers.MyGridColorHelper::GetGridColor(Sandbox.Game.Entities.MyCubeGrid grid)
stloc.s 4 (System.Nullable`1[VRageMath.Color])
ldarg.2
ldc.r4 0.6
ldloca.s 4 (System.Nullable`1[VRageMath.Color])
call System.Boolean System.Nullable`1<VRageMath.Color>::get_HasValue()
brtrue.s L11
ldsfld VRageMath.Vector4 VRageMath.Vector4::One
br.s L12
L11:
ldloca.s 4 (System.Nullable`1[VRageMath.Color])
call VRageMath.Color System.Nullable`1<VRageMath.Color>::get_Value()
stloc.s 5 (VRageMath.Color)
ldloca.s 5 (VRageMath.Color)
call virtual VRageMath.Vector4 VRageMath.Color::ToVector4()
L12:
call static VRageMath.Vector4 VRageMath.Vector4::op_Multiply(System.Single scaleFactor, VRageMath.Vector4 value1)
newobj System.Void System.Nullable`1<VRageMath.Vector4>::.ctor(VRageMath.Vector4 value)
stfld System.Nullable`1<VRageMath.Vector4> Sandbox.Graphics.GUI.Item::ColorMask
ldarg.2
ldnull
stfld System.String Sandbox.Graphics.GUI.Item::FontOverride
ret
L10:
ldarg.1
callvirt Sandbox.Game.Entities.MyIDModule Sandbox.Game.Entities.MyCubeBlock::get_IDModule()
brtrue.s L13
ldarg.1
callvirt System.Boolean Sandbox.Game.Entities.Cube.MyTerminalBlock::HasLocalPlayerAccessToBlockWithoutOwnership()
brtrue.s L14
ldarg.2
ldsfld VRageMath.Vector4 Sandbox.Game.Gui.MyTerminalControlPanel::RED_TEXT_COLOR
newobj System.Void System.Nullable`1<VRageMath.Vector4>::.ctor(VRageMath.Vector4 value)
stfld System.Nullable`1<VRageMath.Vector4> Sandbox.Graphics.GUI.Item::ColorMask
ldarg.2
callvirt System.Text.StringBuilder Sandbox.Graphics.GUI.Item::get_Text()
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Terminal_BlockAccessDenied
call static System.Text.StringBuilder VRage.MyTexts::Get(VRage.Utils.MyStringId id)
call static System.Text.StringBuilder System.Text.StringBuilderExtensions_Format::AppendStringBuilder(System.Text.StringBuilder stringBuilder, System.Text.StringBuilder otherStringBuilder)
pop
ret
L13:
L14:
ldarg.0
ldfld Sandbox.Game.Screens.Helpers.MyGridColorHelper Sandbox.Game.Gui.MyTerminalControlPanel::m_colorHelper
ldarg.1
callvirt Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyCubeBlock::get_CubeGrid()
callvirt System.Nullable`1<VRageMath.Color> Sandbox.Game.Screens.Helpers.MyGridColorHelper::GetGridColor(Sandbox.Game.Entities.MyCubeGrid grid)
stloc.s 6 (System.Nullable`1[VRageMath.Color])
ldloca.s 6 (System.Nullable`1[VRageMath.Color])
call System.Boolean System.Nullable`1<VRageMath.Color>::get_HasValue()
brfalse.s L15
ldarg.2
ldloca.s 6 (System.Nullable`1[VRageMath.Color])
call VRageMath.Color System.Nullable`1<VRageMath.Color>::get_Value()
stloc.s 5 (VRageMath.Color)
ldloca.s 5 (VRageMath.Color)
call virtual VRageMath.Vector4 VRageMath.Color::ToVector4()
newobj System.Void System.Nullable`1<VRageMath.Vector4>::.ctor(VRageMath.Vector4 value)
stfld System.Nullable`1<VRageMath.Vector4> Sandbox.Graphics.GUI.Item::ColorMask
L15:
ldarg.2
ldnull
stfld System.String Sandbox.Graphics.GUI.Item::FontOverride
L5:
L6:
ret
