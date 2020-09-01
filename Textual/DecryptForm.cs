using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textual
{
    public partial class DecryptForm : Form
    {
        private Form1 parent;
        private string filename;

        public DecryptForm(Form1 parent, string filename)
        {
            InitializeComponent();
            this.parent = parent;
            this.filename = filename;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            decryptClicked();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                decryptClicked();
            }
        }

        private void decryptClicked()
        {
            string password = textBox1.Text;

            if (password.Equals(""))
            {
                MessageBox.Show("Dont't leave it blank!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                parent.decryptAndOpen(password, filename);
                this.Dispose();
            }
        }
    }
}
