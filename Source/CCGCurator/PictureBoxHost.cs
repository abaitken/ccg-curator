using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace CCGCurator
{
    public class PictureBoxHost : WindowsFormsHost
    {
        public PictureBoxHost()
        {
            var control = new PictureBox();
            control.Dock = DockStyle.Fill;
            Child = control;
        }

        public new PictureBox Child
        {
            get
            {
                return (PictureBox)base.Child;
            }

            set
            {
                base.Child = value;
            }
        }
    }
}
