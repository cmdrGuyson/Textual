﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textual
{
    public partial class Form1 : Form
    {
        // Mutual exclusion for the richTextBox in given tab
        private Mutex rtbMutex;

        //List of opened file paths
        private static List<string> openedFilesList;

        //Class to manage of words for spell check
        private WordList wordList;

        public Form1()
        {
            InitializeComponent();
            rtbMutex = new Mutex();
            openedFilesList = new List<string> { };
            wordList = new WordList();
        }

        //Method to create a new tab
        private void makeNewTab()
        {
            TabPage tabPage = new TabPage();
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.TextChanged += new EventHandler(richTextBox_TextChanged);
            richTextBox.KeyPress += new KeyPressEventHandler(richTextBox_KeyPress);
            richTextBox.ForeColor = Color.Black;
            richTextBox.Dock = DockStyle.Fill;
            tabPage.Controls.Add(richTextBox);
            tabControl.Controls.Add(tabPage);
            resetCounts();

            tabControl.SelectedTab = tabPage;

            //Title of the newly opened tab
            tabPage.Text = "untitled";

            filename_toolStripLabel.Text = "untitled";
        }

        //Method to close the current tab
        private void closeCurrentTab()
        {
            if (tabControl.SelectedTab != null) 
            {
                if (!tabControl.SelectedTab.Text.StartsWith("*"))
                {
                    if (filename_toolStripLabel.Text.Contains("\\"))
                    {
                        openedFilesList.Remove(filename_toolStripLabel.Text);
                    }
                    tabControl.TabPages.Remove(tabControl.SelectedTab);
                    resetCounts();
                }
                else
                {
                    DialogResult result = MessageBox.Show("You haven't saved your changes!", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    switch (result) {

                        case DialogResult.Yes:
                            save();
                            break;
                        case DialogResult.No:
                            if (filename_toolStripLabel.Text.Contains("\\"))
                            {
                                openedFilesList.Remove(filename_toolStripLabel.Text);
                            }
                            tabControl.TabPages.Remove(tabControl.SelectedTab);
                            resetCounts();
                            break;
                        default:
                            break;
                    }
                }
            }
            
            
        }

        //Method to get the RichTextBox of the current tab
        private RichTextBox getRichTextBox()
        {
            TabPage tabPage = tabControl.SelectedTab;
            RichTextBox text = tabPage.Controls[0] as RichTextBox;
            return text;
        }

        //When "New Tab" is clicked
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeNewTab();
        }

        //When "Close Tab" is clicked
        private void toolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            closeCurrentTab();
        }

        //When "Save" is clicked
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            save();
        }

        /*SAVE*/
        private void save()
        {
            TabPage selectedTab = tabControl.SelectedTab;

            if (selectedTab.Text.EndsWith(".texx"))
            {
                DialogResult result = MessageBox.Show("Please re-encrypt and save!", "Unsaved Changes", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                switch (result)
                {

                    case DialogResult.OK:
                        encryptAndSave();
                        break;
                    case DialogResult.Cancel:
                        break;
                }
            }
            else
            {
                //If save as has already been done
                if (filename_toolStripLabel.Text.Contains("\\"))
                {
                    //If file has unsaved changes
                    if (selectedTab.Text.Contains("*"))
                    {
                        string filename = filename_toolStripLabel.Text;
                        if (File.Exists(filename))
                        {
                            getRichTextBox().SaveFile(filename, RichTextBoxStreamType.RichText);

                            /*
                            File.WriteAllText(filename, "");
                            StreamWriter strwriter = File.AppendText(filename);
                            strwriter.Write(getRichTextBox().Text);
                            strwriter.Close();
                            strwriter.Dispose(); */
                            selectedTab.Text = selectedTab.Text.Remove(0, 1);
                        }
                    }
                }
                else
                {
                    saveAs();
                }
            }
        }

        /*SAVE AS*/
        private void saveAs()
        {
            if (tabControl.SelectedTab != null)
            {
                TabPage tabPage = tabControl.SelectedTab;

                saveFileDialog.Filter = "Rich Text Format|*.rtf";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filename = saveFileDialog.FileName;
                    getRichTextBox().SaveFile(filename, RichTextBoxStreamType.RichText);

                    /*File.WriteAllText(filename, "");
                    StreamWriter strw = new StreamWriter(filename);
                    strw.Write(getRichTextBox().Text);
                    strw.Close();
                    strw.Dispose();*/

                    //Simplified filename
                    string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                    tabPage.Text = fname;
                    filename_toolStripLabel.Text = filename;
                    openedFilesList.Add(filename);

                    //remove not saved status
                    if (tabControl.SelectedTab.Text.StartsWith("*"))
                    {
                        tabPage.Text = tabPage.Text.Remove(0, 1);
                    }
                }
            }
        }

        /*SAVE ALL*/
        private void saveAll()
        {
            //If tabs are open
            if(tabControl.TabCount > 0)
            {
                //Change the order of the opened files list
                openedFilesList.Reverse();

                //Get all the tabs open in the system
                TabControl.TabPageCollection tabPageCollection = tabControl.TabPages;

                //Loop through all tab pages
                foreach(TabPage tab in tabPageCollection)
                {
                    //Get title of tab and the content in each tabs rich text box
                    string tab_text = tab.Text;
                    RichTextBox tab_rtb =  tab.Controls[0] as RichTextBox;
                    string rtb_content = tab_rtb.Rtf;

                    string filepath = null;

                    if (tab_text.EndsWith(".texx"))
                    {
                        break;
                    }

                    //Find the filepath (if it exists) of the tab using the openedFilesList
                    foreach (string filename in openedFilesList)
                    {
                        string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                        if (tab_text.Contains("*"))
                        {
                            string new_fname = tab.Text.Remove(0, 1);

                            if (fname == new_fname)
                            {
                                filepath = filename;
                            }
                        }
                        else
                        {
                            if (fname == tab.Text)
                            {
                               filepath = filename;
                            }
                        }
                    }

                    //If tab is not an untitled tab and a filepath exists
                    if ((!tab_text.Equals("untitled") || !tab_text.Equals("*untitled")) && filepath!=null)
                    {
                        //Start a new task to save the content of the tab's rich text box
                        Task task = new Task(() =>
                        {
                            try
                            {
                                File.WriteAllText(filepath, "");
                                StreamWriter streamWriter = File.AppendText(filepath);
                                streamWriter.Write(rtb_content);
                                streamWriter.Close();
                                streamWriter.Dispose();

                                //Remove saved status (*)
                                if (tab_text.Contains("*"))
                                {
                                    tab.Text = tab.Text.Remove(0, 1);
                                }
                            }
                            catch (Exception ex)
                            {
                                //Throw any errors
                                throw ex;
                            }
                        });

                        //Start the task
                        task.Start();

                        //Catch any errors and display (This occurs only when an error is thrown from the task)
                        task.ContinueWith(t =>
                        {

                            task.Exception.Handle(ex =>
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            });

                        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
                    }

                }
            }
        }

        //When "Open" is clicked
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //openFileDialog.Filter = "Rich Text Format|*.rtf";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filename = openFileDialog.FileName;

                    //If file is an encrypted file
                    if (filename.EndsWith(".texx"))
                    {
                        DecryptForm decryptForm = new DecryptForm(this, filename);
                        decryptForm.Show();
                    }
                    else
                    {
                        //If no unedited untitled tab exists or if no tabs are open
                        if (tabControl.SelectedTab == null || !filename_toolStripLabel.Text.Equals("untitled") || tabControl.SelectedTab.Text.Contains("*"))
                        {
                            makeNewTab();
                        }

                        getRichTextBox().LoadFile(openFileDialog.FileName, RichTextBoxStreamType.RichText);
                        //getRichTextBox().Text = encryptor.Decrypt(getRichTextBox().Text);
                        TabPage tabPage = tabControl.SelectedTab;

                        //Simplified filename
                        string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                        tabPage.Text = fname;
                        openedFilesList.Add(filename);
                        filename_toolStripLabel.Text = filename;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void decryptAndOpen(string password, string filename)
        {
            try
            {
                StreamReader streamReader = new StreamReader(filename);
                string rtb_content = streamReader.ReadToEnd();
                streamReader.Close();

                string decrypted = Encryptor.Decrypt(rtb_content, password);

                if (decrypted.StartsWith("ENCRYPTED"))
                {
                    //If no unedited untitled tab exists or if no tabs are open
                    if (tabControl.SelectedTab == null || !filename_toolStripLabel.Text.Equals("untitled") || tabControl.SelectedTab.Text.Contains("*"))
                    {
                        makeNewTab();
                    }

                    decrypted = decrypted.Substring(9);

                    getRichTextBox().Rtf = decrypted;
                    //getRichTextBox().Text = encryptor.Decrypt(getRichTextBox().Text);
                    TabPage tabPage = tabControl.SelectedTab;

                    //Simplified filename
                    string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                    tabPage.Text = fname;
                    openedFilesList.Add(filename);
                    filename_toolStripLabel.Text = filename;
                }
                else
                {
                    MessageBox.Show("Invalid Password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

               
            }catch (CryptographicException ex)
            {
                MessageBox.Show("Invalid Password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //When "Print" button is clicked
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //printDialog.Document = printDocument;
            if (tabControl.SelectedTab != null)
            {
                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDocument.Print();
                }
            }
        }

        //Method to print
        private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            e.Graphics.DrawString(getRichTextBox().Text, getRichTextBox().Font, Brushes.Black, 12, 10);
        }

        //When "Undo" button is clicked
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null) getRichTextBox().Undo();
        }

        //When "Redo" button is clicked
        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null) getRichTextBox().Redo();
        }

        //When "Cut" button is clicked
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null) getRichTextBox().Cut();
        }

        //When "Copy" button is clicked
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null) getRichTextBox().Copy();
        }

        //When "Paste" button is clicked
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null) getRichTextBox().Paste();
        }

        //Method to change fonts
        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    getRichTextBox().SelectionFont = fontDialog.Font;
                }
            }

        }

        //When text is changed in the tabs rich text box update the word count, charachter count and line count
        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            getRichTextBox().ForeColor = Color.Black;
            //When changes are done set the saved state of the current tab as false
            if (!tabControl.SelectedTab.Text.StartsWith("*"))
            {
                tabControl.SelectedTab.Text = $"*{tabControl.SelectedTab.Text}";
            }

            //spellCheck();
            calculateCounts();
        }

        //Method to calculate word count in selected tab
        private void calculateCounts()
        {
            if (tabControl.SelectedTab != null)
            {
                //This is performed in a task
                Task<Counts> task = new Task<Counts>((text) =>
                {
                    string rtb_text = (string)text;

                    Counts counts = new Counts();

                    //Ignore new lines, spaces, etc and calculate the word count, charachter count and line count
                    char[] delimiters = new char[] { ' ', '\r', '\n' };
                    counts.wordCount = rtb_text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
                    counts.charachterCount = rtb_text.Length;
                    counts.lineCount = rtb_text.Split('\n').Length;

                    return counts;

                }, getRichTextBox().Text);

                task.Start();

                //When the task is complete display word count in the status label
                task.ContinueWith(t =>
                {
                    //Only update counts if a tab is open
                    if (tabControl.SelectedTab != null)
                    {
                        toolStripStatusLabel1.Text = t.Result.wordCount + " Words";
                        toolStripStatusLabel2.Text = t.Result.charachterCount + " Charachters";
                        toolStripStatusLabel3.Text = t.Result.lineCount + " Lines";
                    }

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }

        //When user switches to a new tab display new word count, charachter count and line count
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            resetCounts();
            calculateCounts();

            TabPage tabPage = tabControl.SelectedTab;

            //If at least 1 tab is open
            if(tabControl.TabCount > 0)
            {
                if (tabPage.Text.Equals("untitled") || tabPage.Text.Equals("*untitled"))
                {
                    filename_toolStripLabel.Text = tabPage.Text;
                }
                else
                {
                    foreach (string filename in openedFilesList)
                    {
                        if (tabPage != null)
                        {
                            string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                            if (tabPage.Text.Contains("*"))
                            {
                                string new_fname = tabPage.Text.Remove(0, 1);

                                if(fname == new_fname)
                                {
                                    filename_toolStripLabel.Text = filename;
                                }
                            }
                            else
                            {
                                if(fname == tabPage.Text)
                                {
                                    filename_toolStripLabel.Text = filename;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                filename_toolStripLabel.Text = "No files open";
            }
        }

        //Set all counts to empty
        private void resetCounts()
        {
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel2.Text = "";
            toolStripStatusLabel3.Text = "";
        }

        
        private void tabControl_ControlAdded(object sender, ControlEventArgs e)
        {
            //When a new tab is added calculate the word/charachter/line counts
            calculateCounts();
        }

        //When "Save-As" is clicked
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAs();
        }

        //When "Font Color" is clicked
        private void toolStripButtonTextColor_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    getRichTextBox().SelectionColor = colorDialog.Color;
                }
            }
        }

        private void spellCheck()
        {
            Task task = new Task(() =>
            {
                bool aLock = rtbMutex.WaitOne();

                try
                {
                    char[] delimiters = new char[] { ' ', '\r', '\n' };
                    string[] words = getRichTextBox().Text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string word in words)
                    {
                        if (!wordList.words.Contains(word) || !wordList.words.Contains(word.Remove('.')))
                        {
                            var matchString = Regex.Escape(word);
                            foreach (Match match in Regex.Matches(getRichTextBox().Text, matchString))
                            {
                                getRichTextBox().Select(match.Index, word.Length);
                                getRichTextBox().SelectionColor = Color.Red;
                                getRichTextBox().Select(getRichTextBox().TextLength, 0);
                                getRichTextBox().SelectionColor = getRichTextBox().ForeColor;
                            };
                        }
                    }
                }
                finally
                {
                    if (aLock) rtbMutex.ReleaseMutex();
                }

            });

            task.Start();
            
        }

        //Each time "Space" key is pressed while typing in a tab
        private void richTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == ' ')
            {
                spellCheck();
            }
        }

        private void toolStripMenuItem_saveAll_Click(object sender, EventArgs e)
        {
            saveAll();
        }

        private void toolStripMenuItem_encrypt_Click(object sender, EventArgs e)
        {
            encryptAndSave();
        }

        //Method to show EncryptForm window
        private void encryptAndSave()
        {
            if (tabControl.SelectedTab != null)
            {
                //Get the rich text box content
                string rtb_content = getRichTextBox().Rtf;
                //Create a new EncryptForm by using rich text box content and show it
                EncryptForm encryptForm = new EncryptForm(rtb_content, this);
                this.Enabled = false;
                encryptForm.Show();
            }
        }

        //Used by EncryptForm class to change the saved state of a tab
        public void changeSavedState()
        {
            TabPage tab = tabControl.SelectedTab;

            //Remove saved status (*)
            if (tab.Text.Contains("*"))
            {
                tab.Text = tab.Text.Remove(0, 1);
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getRichTextBox().SelectAll();
        }

        //When "Print Preview" is clicked
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                printPreviewDialog.Document = printDocument;
                printPreviewDialog.ShowDialog();
            }
        }
    }

}

    
