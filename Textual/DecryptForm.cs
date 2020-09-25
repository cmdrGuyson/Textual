using System;
using System.Windows.Forms;

namespace Textual
{
    public partial class DecryptForm : Form
    {
        private MainForm parent;
        private string filename;

        public DecryptForm(MainForm parent, string filename)
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
            //If enter is pressed
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                decryptClicked();
            }
        }

        //When decrypt button is clicked
        private void decryptClicked()
        {
            string password = textBox1.Text;

            //Empty field
            if (password.Equals(""))
            {
                MessageBox.Show("Dont't leave it blank!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                //Call decryptAndOpen method on parent form
                parent.decryptAndOpen(password, filename);
                this.Dispose();
            }
        }
    }
}
