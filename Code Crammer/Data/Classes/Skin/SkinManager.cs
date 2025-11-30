#nullable enable

namespace Code_Crammer.Data.Classes.Skin
{
    public static class SkinManager
    {
        public enum Skin
        {
            Light,
            Dark
        }

        private static class DarkPalette
        {
            public static readonly Color Background = Color.FromArgb(45, 45, 48);
            public static readonly Color ControlBack = Color.FromArgb(30, 30, 30);
            public static readonly Color Text = Color.FromArgb(241, 241, 241);

            public static readonly Color Border = Color.FromArgb(63, 63, 70);
            public static readonly Color MenuBack = Color.FromArgb(27, 27, 28);
        }

        public static void ApplySkin(Form form, Skin skin)
        {
            bool isDark = skin == Skin.Dark;
            Color backColor = isDark ? DarkPalette.Background : SystemColors.Control;
            Color foreColor = isDark ? DarkPalette.Text : SystemColors.ControlText;
            Color controlBack = isDark ? DarkPalette.ControlBack : SystemColors.Window;
            Color controlText = isDark ? DarkPalette.Text : SystemColors.WindowText;

            form.BackColor = backColor;
            form.ForeColor = foreColor;

            foreach (Control c in form.Controls)
            {
                ApplySkinToControl(c, isDark, backColor, foreColor, controlBack, controlText);
            }

            ApplyRenderer(form, isDark);
        }

        public static void ApplySkin(ContextMenuStrip menu, Skin skin)
        {
            bool isDark = skin == Skin.Dark;
            menu.BackColor = isDark ? DarkPalette.MenuBack : SystemColors.Control;
            menu.ForeColor = isDark ? DarkPalette.Text : SystemColors.ControlText;
            menu.Renderer = isDark ? new DarkModeRenderer() : new ToolStripProfessionalRenderer();

            foreach (ToolStripItem item in menu.Items)
            {
                UpdateMenuItem(item, isDark);
            }
        }

        private static void ApplySkinToControl(Control c, bool isDark, Color back, Color fore, Color ctrlBack, Color ctrlText)
        {
            if (c.Name == "rtbLog") return;

            if (c.Name == "lblTokenCount")
            {
                c.BackColor = isDark ? ctrlBack : Color.White;
                c.ForeColor = isDark ? fore : Color.Black;
                return;
            }

            if (c.HasChildren)
            {
                foreach (Control child in c.Controls)
                {
                    ApplySkinToControl(child, isDark, back, fore, ctrlBack, ctrlText);
                }
            }

            if (c is TextBox || c is RichTextBox || c is ListBox || c is TreeView || c is CheckedListBox)
            {
                c.BackColor = ctrlBack;
                c.ForeColor = ctrlText;
            }
            else if (c is Button btn)
            {
                btn.BackColor = isDark ? DarkPalette.ControlBack : SystemColors.Control;
                btn.ForeColor = fore;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = isDark ? DarkPalette.Border : SystemColors.ControlDark;
            }
            else if (c is GroupBox gpb)
            {
                gpb.ForeColor = fore;
            }
            else if (c is Label lbl)
            {
                lbl.ForeColor = fore;
            }
        }

        private static void ApplyRenderer(Form form, bool isDark)
        {
            ToolStripManager.Renderer = isDark ? new DarkModeRenderer() : new ToolStripProfessionalRenderer();

            foreach (var c in form.Controls)
            {
                if (c is ToolStrip ts)
                {
                    ts.Renderer = ToolStripManager.Renderer;
                    ts.BackColor = isDark ? DarkPalette.MenuBack : SystemColors.Control;
                    ts.ForeColor = isDark ? DarkPalette.Text : SystemColors.ControlText;

                    foreach (ToolStripItem item in ts.Items)
                    {
                        UpdateMenuItem(item, isDark);
                    }
                }
            }
        }

        private static void UpdateMenuItem(ToolStripItem item, bool isDark)
        {
            item.ForeColor = isDark ? DarkPalette.Text : SystemColors.ControlText;
            item.BackColor = isDark ? DarkPalette.MenuBack : SystemColors.Control;

            if (item is ToolStripDropDownItem dropDown)
            {
                if (dropDown.DropDown != null)
                {
                    dropDown.DropDown.BackColor = isDark ? DarkPalette.MenuBack : SystemColors.Control;
                    dropDown.DropDown.ForeColor = isDark ? DarkPalette.Text : SystemColors.ControlText;
                }
                foreach (ToolStripItem child in dropDown.DropDownItems)
                {
                    UpdateMenuItem(child, isDark);
                }
            }
        }

        private class DarkModeRenderer : ToolStripProfessionalRenderer
        {
            public DarkModeRenderer() : base(new DarkColors())
            {
            }
        }

        private class DarkColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(62, 62, 64);
            public override Color MenuItemBorder => Color.FromArgb(62, 62, 64);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(45, 45, 48);
            public override Color MenuBorder => Color.FromArgb(63, 63, 70);
            public override Color ToolStripDropDownBackground => Color.FromArgb(27, 27, 28);
            public override Color ImageMarginGradientBegin => Color.FromArgb(27, 27, 28);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(27, 27, 28);
            public override Color ImageMarginGradientEnd => Color.FromArgb(27, 27, 28);
        }
    }
}