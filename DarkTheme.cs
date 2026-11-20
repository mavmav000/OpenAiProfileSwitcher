using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OpenAIProfileSwitcher;

/// <summary>
/// Permanent Windows dark palette. No user setting or theme toggle is required.
/// We use WinForms controls plus the Windows DWM dark-caption flag.
/// </summary>
internal static class DarkTheme
{
    public static readonly Color Background = Color.FromArgb(23, 24, 29);
    public static readonly Color Surface = Color.FromArgb(31, 33, 39);
    public static readonly Color SurfaceAlternate = Color.FromArgb(35, 37, 44);
    public static readonly Color Input = Color.FromArgb(40, 42, 50);
    public static readonly Color Header = Color.FromArgb(42, 44, 53);
    public static readonly Color Text = Color.FromArgb(238, 240, 245);
    public static readonly Color SecondaryText = Color.FromArgb(165, 170, 182);
    public static readonly Color Border = Color.FromArgb(66, 70, 82);
    public static readonly Color Hover = Color.FromArgb(53, 58, 70);
    public static readonly Color Accent = Color.FromArgb(48, 112, 219);
    public static readonly Color AccentHover = Color.FromArgb(60, 128, 236);
    public static readonly Color Selected = Color.FromArgb(42, 67, 103);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref int value, int valueSize);

    public static void StyleForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.HandleCreated += (_, _) => SetDarkCaption(form.Handle);
        if (form.IsHandleCreated)
        {
            SetDarkCaption(form.Handle);
        }
    }

    private static void SetDarkCaption(IntPtr handle)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            const int dark = 1;
            var value = dark;
            // DWMWA_USE_IMMERSIVE_DARK_MODE is 20 on current Windows builds,
            // with 19 used by some older Windows 10 releases.
            if (DwmSetWindowAttribute(handle, 20, ref value, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, 19, ref value, sizeof(int));
            }
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    public static void StyleButton(Button button, bool primary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Accent : Input;
        button.ForeColor = Text;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Hover;
        button.FlatAppearance.MouseDownBackColor = primary ? Accent : SurfaceAlternate;
    }

    public static void StyleComboBox(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = Input;
        combo.ForeColor = Text;
    }

    public static void StyleTextBox(TextBox box, bool border = true)
    {
        box.BackColor = Input;
        box.ForeColor = Text;
        box.BorderStyle = border ? BorderStyle.FixedSingle : BorderStyle.None;
    }

    public static void StyleCheckBox(CheckBox check)
    {
        check.FlatStyle = FlatStyle.Flat;
        check.BackColor = Background;
        check.ForeColor = Text;
    }

    public static void StyleListView(ListView list)
    {
        list.BackColor = Surface;
        list.ForeColor = Text;
        list.BorderStyle = BorderStyle.FixedSingle;
        list.GridLines = false;
        list.OwnerDraw = true;

        list.DrawColumnHeader += (_, e) =>
        {
            using var fill = new SolidBrush(Header);
            using var divider = new Pen(Border);
            e.Graphics.FillRectangle(fill, e.Bounds);
            var bounds = Rectangle.Inflate(e.Bounds, -10, 0);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, list.Font, bounds, Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            e.Graphics.DrawLine(divider, e.Bounds.Right - 1, e.Bounds.Top,
                e.Bounds.Right - 1, e.Bounds.Bottom);
            e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1,
                e.Bounds.Right, e.Bounds.Bottom - 1);
        };

        // In Details view the subitem handler paints every cell.
        list.DrawItem += (_, _) => { };
        list.DrawSubItem += (_, e) =>
        {
            var isSelected = e.Item?.Selected == true;
            var background = isSelected ? Selected :
                e.ItemIndex % 2 == 0 ? Surface : SurfaceAlternate;
            using var fill = new SolidBrush(background);
            using var divider = new Pen(Border);
            e.Graphics.FillRectangle(fill, e.Bounds);
            var bounds = Rectangle.Inflate(e.Bounds, -10, 0);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? string.Empty,
                e.SubItem?.Font ?? list.Font, bounds, Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1,
                e.Bounds.Right, e.Bounds.Bottom - 1);
        };
    }

    /// <summary>
    /// Dark equivalent of the simple confirmations/warnings used by this app.
    /// Windows' built-in MessageBox may otherwise show a light dialog.
    /// </summary>
    public static DialogResult ShowMessage(IWin32Window owner, string message,
        string caption, MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        using var dialog = new Form
        {
            Text = caption,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = new Font("Segoe UI", 9F)
        };
        StyleForm(dialog);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20, 18, 20, 14),
            BackColor = Background
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        dialog.Controls.Add(content);

        var messageLabel = new Label
        {
            Text = message,
            AutoSize = true,
            MaximumSize = new Size(492, 0),
            ForeColor = Text,
            BackColor = Background,
            Margin = new Padding(0, 0, 0, 12)
        };
        content.Controls.Add(messageLabel, 0, 0);

        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Background,
            Margin = new Padding(0)
        };
        content.Controls.Add(actionRow, 0, 1);

        void AddAction(string label, DialogResult result, bool primary = false)
        {
            var button = new Button
            {
                Text = label,
                DialogResult = result,
                Size = new Size(94, 32),
                Margin = new Padding(8, 4, 0, 0)
            };
            StyleButton(button, primary);
            actionRow.Controls.Add(button);
            if (primary) dialog.AcceptButton = button;
            if (result == DialogResult.Cancel || result == DialogResult.No)
                dialog.CancelButton = button;
        }

        switch (buttons)
        {
            case MessageBoxButtons.YesNo:
                AddAction("No", DialogResult.No);
                AddAction("Yes", DialogResult.Yes, primary: true);
                break;
            case MessageBoxButtons.OKCancel:
                AddAction("Cancel", DialogResult.Cancel);
                AddAction("OK", DialogResult.OK, primary: true);
                break;
            default:
                AddAction("OK", DialogResult.OK, primary: true);
                break;
        }

        dialog.ClientSize = new Size(550,
            Math.Max(165, Math.Min(520, messageLabel.PreferredHeight + 100)));
        return dialog.ShowDialog(owner);
    }
}
