using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textual
{
    public partial class EncryptForm : Form
    {
        private string rtb_content;
        private MainForm parent;

        public EncryptForm(string rtb_content, MainForm parent)
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
            //If enter is pressed
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                encryptClicked();
            }
        }

        private void encryptClicked()
        {
            string password = textBox1.Text;
            string confirmPassword = textBox2.Text;

            //If fields are empty
            if (password.Equals("") || confirmPassword.Equals(""))
            {
                MessageBox.Show("Dont't leave it blank!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //If password is not equal to confirm password
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
                    //Task to encrypt and save file
                    Task<string> task = new Task<string>(() =>
                    {
                        try
                        {
                            string filename = saveFileDialog.FileName;
                            //Add "ENCRYPTED" to the start of content to be encrypted
                            rtb_content = "ENCRYPTED" + rtb_content;
                            //Encrypt content and save
                            string encrypted_content = Encryptor.Encrypt(rtb_content, password);
                            File.WriteAllText(filename, "");
                            StreamWriter strw = new StreamWriter(filename);
                            strw.Write(encrypted_content);
                            strw.Close();
                            strw.Dispose();

                            return filename;

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                    });

                    //When task is over call changeSavedState method on parent form to remove * from tab title
                    task.ContinueWith(t =>
                    {
                        parent.Enabled = true;
                        parent.changeSavedState();
                        parent.changeTitleLabel(t.Result);
                        this.Dispose();

                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    //Handle any exceptions that occured in the task
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
