using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textual
{
    public partial class EncryptForm : Form
    {
        private string rtb_content;
        private Form1 parent;

        public EncryptForm(string rtb_content, Form1 parent)
        {
            InitializeComponent();
            this.rtb_content = rtb_content;
            this.parent = parent;
        }

        //When encrypt file is clicked
        private void button1_Click(object sender, EventArgs e)
        {
            encryptClicked();
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                encryptClicked();
            }
        }

        private void encryptClicked()
        {
            string password = textBox1.Text;
            string confirmPassword = textBox2.Text;

            if (password.Equals("") || confirmPassword.Equals(""))
            {
                MessageBox.Show("Dont't leave it blank!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (password != confirmPassword)
            {
                MessageBox.Show("Passwords don't match", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                //Encrypt content of rich text box
                saveFileDialog.Filter = "Textual Encrypted|*.texx";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Task task = new Task(() =>
                    {
                        try
                        {
                            string filename = saveFileDialog.FileName;
                            rtb_content = "ENCRYPTED" + rtb_content;
                            string encrypted_content = Encryptor.Encrypt(rtb_content, password);
                            File.WriteAllText(filename, "");
                            StreamWriter strw = new StreamWriter(filename);
                            strw.Write(encrypted_content);
                            strw.Close();
                            strw.Dispose();

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                    });

                    task.ContinueWith(t =>
                    {
                        parent.Enabled = true;
                        parent.changeSavedState();
                        this.Dispose();
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    task.ContinueWith(t =>
                    {
                        task.Exception.Handle(ex =>
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        });

                        parent.Enabled = true;
                        this.Dispose();

                    }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

                    task.Start();
                }
            }
        }

        private void EncryptForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            parent.Enabled = true;
        }
    }
}
