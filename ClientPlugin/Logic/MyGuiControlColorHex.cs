using System;
using ClientPlugin.Tools;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Logic
{
    public class MyGuiControlColorHex : MyGuiControlColor
    {
        private new Color m_color;
        private Color color
        {
            get => m_color;
            set => base.m_color = m_color = value;
        }

        public new void SetColor(Vector3 color) => SetColor(new Color(color));
        public new void SetColor(Vector4 color) => SetColor(new Color(color));

        private new void SetColor(Color newColor)
        {
            color = newColor;
            hexTextbox.Text = color.ToHexStringRgb();
        }

        public event Action<MyGuiControlColor> MyGuiControlColorHex_OnChange;

        private readonly MyGuiControlLabel label;
        private readonly MyGuiControlTextbox hexTextbox;

        // Defaults copied from MyGuiControlColor's constructor
        public MyGuiControlColorHex(
            string text,
            float textScale,
            Vector2 position,
            Color color,
            Color defaultColor,
            MyStringId dialogAmountCaption,
            bool placeSlidersVertically = false,
            string font = "Blue",
            bool isAutoscaleEnabled = false,
            bool isAutoEllipsisEnabled = false,
            float maxTitleWidth = 1)
            : base(null, textScale, position, color, defaultColor, dialogAmountCaption, placeSlidersVertically, font, isAutoscaleEnabled, isAutoEllipsisEnabled, maxTitleWidth)
        {
            this.color = color;
            label = new MyGuiControlLabel(Vector2.Zero, Vector2.Zero, text, ColorMask, 0.8f * textScale, font, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, isAutoEllipsisEnabled, maxTitleWidth, isAutoscaleEnabled);
            hexTextbox = new MyGuiControlTextbox(defaultText: color.ToHexStringRgb(), maxLength: 6)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };
            hexTextbox.TextChanged += OnTextChanged;
        }

        private void OnTextChanged(MyGuiControlTextbox obj)
        {
            if (hexTextbox.Text.TryParseColorFromHexRgb(out var newColor) && newColor != color)
            {
                hexTextbox.Text = newColor.ToHexStringRgb();
                color = newColor;
                MyGuiControlColorHex_OnChange?.Invoke(this);
            }
        }

        public override void OnSizeChanged()
        {
            base.OnSizeChanged();
            LayoutControls();
        }

        private void LayoutControls()
        {
            var w = Size.X;
            
            label.Position = new Vector2(-0.5f * w, 0f);
            label.Size = new Vector2(0.5f * w, label.Size.Y);
            
            hexTextbox.Position = new Vector2(0f, 0f);
            hexTextbox.Size = new Vector2(0.5f * w, hexTextbox.Size.Y);
        }
    }
}