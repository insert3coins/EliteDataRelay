using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// A small, non-modal dialog to inform the user they are in "reposition mode" for overlays.
    /// </summary>
    public class RepositionDialog : Form
    {
        public RepositionDialog()
        {
            this.Text = "Reposition Overlays";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(320, 90);
            this.TopMost = true;

            var label = new Label
            {
                Text = "You can now drag the overlays to your desired positions.\n\nClick 'Done' when you are finished.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(10)
            };

            var doneButton = new Button
            {
                Text = "Done",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
            };
            doneButton.Click += (s, e) => this.Close();

            this.Controls.Add(label);
            this.Controls.Add(doneButton);
            this.AcceptButton = doneButton;
        }
    }
}