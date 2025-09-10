using System.Drawing;
using System.Windows.Forms;

namespace TeacherApp
{
    public class FullScreenForm : Form
    {
        private PictureBox pb;

        public FullScreenForm(Image img)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    this.Close();
            };

            pb = new PictureBox
            {
                Image = img,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pb);
        }
    }
}