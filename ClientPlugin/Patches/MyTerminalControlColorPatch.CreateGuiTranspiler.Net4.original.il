// b381e75c
ldarg.0
ldarg.0
ldfld VRage.Utils.MyStringId Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::Title
call static System.Text.StringBuilder VRage.MyTexts::Get(VRage.Utils.MyStringId id)
callvirt virtual System.String System.Object::ToString()
ldc.r4 0.95
ldsfld VRageMath.Vector2 VRageMath.Vector2::Zero
call static VRageMath.Color VRageMath.Color::get_White()
call static VRageMath.Color VRageMath.Color::get_White()
ldsfld VRage.Utils.MyStringId MyCommonTexts::DialogAmount_SetValueCaption
ldc.i4.1
ldstr "Blue"
ldarg.0
ldfld System.Boolean Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_isAutoscaleEnabled
ldarg.0
ldfld System.Single Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_maxTitleWidth
stloc.0
ldarg.0
ldfld System.Boolean Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_isAutoEllipsisEnabled
ldloc.0
newobj System.Void Sandbox.Graphics.GUI.MyGuiControlColor::.ctor(System.String text, System.Single textScale, VRageMath.Vector2 position, VRageMath.Color color, VRageMath.Color defaultColor, VRage.Utils.MyStringId dialogAmountCaption, System.Boolean placeSlidersVertically, System.String font, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled, System.Single maxTitleWidth)
stfld Sandbox.Graphics.GUI.MyGuiControlColor Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_color
ldarg.0
ldarg.0
ldftn System.Void Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::OnChangeColor(Sandbox.Graphics.GUI.MyGuiControlColor obj)
newobj System.Void System.Action`1<Sandbox.Graphics.GUI.MyGuiControlColor>::.ctor(System.Object object, System.IntPtr method)
stfld System.Action`1<Sandbox.Graphics.GUI.MyGuiControlColor> Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_changeColor
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlColor Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_color
ldarg.0
ldfld System.Action`1<Sandbox.Graphics.GUI.MyGuiControlColor> Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_changeColor
callvirt System.Void Sandbox.Graphics.GUI.MyGuiControlColor::add_OnChange(System.Action`1<Sandbox.Graphics.GUI.MyGuiControlColor> value)
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlColor Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_color
ldsfld System.Single Sandbox.Game.Gui.MyTerminalControl`1<Sandbox.Game.Gui.TBlock>::PREFERRED_CONTROL_WIDTH
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlColor Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_color
callvirt VRageMath.Vector2 Sandbox.Graphics.GUI.MyGuiControlBase::get_Size()
ldfld System.Single VRageMath.Vector2::Y
newobj System.Void VRageMath.Vector2::.ctor(System.Single x, System.Single y)
callvirt System.Void Sandbox.Graphics.GUI.MyGuiControlBase::set_Size(VRageMath.Vector2 value)
ldsfld System.String System.String::Empty
ldsfld System.String System.String::Empty
ldarg.0
ldfld Sandbox.Graphics.GUI.MyGuiControlColor Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::m_color
ldc.i4.1
ldc.i4.1
ldc.i4.0
ldc.i4.0
ldc.r4 Infinity
newobj System.Void Sandbox.Graphics.GUI.MyGuiControlBlockProperty::.ctor(System.String title, System.String tooltip, Sandbox.Graphics.GUI.MyGuiControlBase propertyControl, Sandbox.Graphics.GUI.MyGuiControlBlockPropertyLayoutEnum layout, System.Boolean showExtraInfo, System.Boolean isAutoEllipsisEnabled, System.Boolean isAutoScaleEnabled, System.Single max_Width)
ret
