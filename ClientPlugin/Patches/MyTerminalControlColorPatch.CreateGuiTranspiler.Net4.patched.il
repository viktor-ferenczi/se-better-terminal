// b381e75c
ldarg.0
ldarg.0
ldfld
call static System.Text.StringBuilder VRage.MyTexts::Get(VRage.Utils.MyStringId id)
callvirt virtual System.String System.Object::ToString()
ldc.r4 0.95
ldsfld
call static VRageMath.Color VRageMath.Color::get_White()
call static VRageMath.Color VRageMath.Color::get_White()
ldsfld
ldc.i4.1
ldstr "Blue"
ldarg.0
ldfld
ldarg.0
ldfld
stloc.0
ldarg.0
ldfld
ldloc.0
newobj System.Void Sandbox.Graphics.GUI.MyGuiControlColor::.ctor(System.String text, System.Single textScale, VRageMath.Vector2 position, VRageMath.Color color, VRageMath.Color defaultColor, VRage.Utils.MyStringId dialogAmountCaption, System.Boolean placeSlidersVertically, System.String font, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled, System.Single maxTitleWidth)
stfld
ldarg.0
ldarg.0
ldftn System.Void Sandbox.Game.Gui.MyTerminalControlColor`1<Sandbox.Game.Gui.TBlock>::OnChangeColor(Sandbox.Graphics.GUI.MyGuiControlColor obj)
newobj System.Void System.Action`1<Sandbox.Graphics.GUI.MyGuiControlColor>::.ctor(System.Object object, System.IntPtr method)
stfld
ldarg.0
ldfld
ldarg.0
ldfld
callvirt System.Void ClientPlugin.Logic.MyGuiControlColorHex::add_OnChange(System.Action`1<ClientPlugin.Logic.MyGuiControlColorHex> value)
ldarg.0
ldfld
ldsfld
ldarg.0
ldfld
callvirt VRageMath.Vector2 Sandbox.Graphics.GUI.MyGuiControlBase::get_Size()
ldfld
newobj System.Void VRageMath.Vector2::.ctor(System.Single x, System.Single y)
callvirt System.Void Sandbox.Graphics.GUI.MyGuiControlBase::set_Size(VRageMath.Vector2 value)
ldsfld
ldsfld
ldarg.0
ldfld
ldc.i4.1
ldc.i4.1
ldc.i4.0
ldc.i4.0
ldc.r4 Infinity
newobj System.Void Sandbox.Graphics.GUI.MyGuiControlBlockProperty::.ctor(System.String title, System.String tooltip, Sandbox.Graphics.GUI.MyGuiControlBase propertyControl, Sandbox.Graphics.GUI.MyGuiControlBlockPropertyLayoutEnum layout, System.Boolean showExtraInfo, System.Boolean isAutoEllipsisEnabled, System.Boolean isAutoScaleEnabled, System.Single max_Width)
ret
