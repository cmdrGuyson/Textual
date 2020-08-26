using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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

        //Encryptor class
        Encryptor encryptor = new Encryptor();

        public Form1()
        {
            InitializeComponent();
            rtbMutex = new Mutex();
            openedFilesList = new List<string> { };
            wordList = new WordList();
        }

        //Method to create a new tab
        public void makeNewTab()
        {
            TabPage tabPage = new TabPage();
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.TextChanged += new EventHandler(richTextBox_TextChanged);
            richTextBox.ForeColor = Color.Red;
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
        public void closeCurrentTab()
        {
            if (tabControl.SelectedTab != null) 
            {
                if (!tabControl.SelectedTab.Text.StartsWith("*"))
                {
                    tabControl.TabPages.Remove(tabControl.SelectedTab);

                    resetCounts();
                }
                else
                {
                    DialogResult result = MessageBox.Show("You haven't saved your changes!", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    switch (result) {

                        case DialogResult.Yes:
                            saveAs();
                            break;
                        case DialogResult.No:
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
        public RichTextBox getRichTextBox()
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

            //If save as has already been done
            if (filename_toolStripLabel.Text.Contains("\\"))
            {
                //If file has unsaved changes
                if (selectedTab.Text.Contains("*"))
                {
                    string filename = filename_toolStripLabel.Text;
                    if (File.Exists(filename))
                    {
                        File.WriteAllText(filename, "");
                        StreamWriter strwriter = File.AppendText(filename);
                        strwriter.Write(getRichTextBox().Text);
                        strwriter.Close();
                        strwriter.Dispose();
                        selectedTab.Text = selectedTab.Text.Remove(0, 1);
                    }
                }
            }
            else
            {
                saveAs();
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
                    getRichTextBox().SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.PlainText);
                    
                    string filename = saveFileDialog.FileName;

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

        //When "Open" is clicked
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //openFileDialog.Filter = "Rich Text Format|*.rtf";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if(tabControl.SelectedTab == null || !filename_toolStripLabel.Text.Equals("untitled") || tabControl.SelectedTab.Text.Contains("*")){
                    makeNewTab();
                }
                
                getRichTextBox().LoadFile(openFileDialog.FileName, RichTextBoxStreamType.PlainText);
                TabPage tabPage = tabControl.SelectedTab;

                string filename = openFileDialog.FileName;
                //Simplified filename
                string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                tabPage.Text = fname;
                openedFilesList.Add(filename);
                filename_toolStripLabel.Text = filename;
            }
        }

        //When "Print" button is clicked
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printDialog.Document = printDocument;
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
                toolStripStatusLabel1.Text = t.Result.wordCount + " Words";
                toolStripStatusLabel2.Text = t.Result.charachterCount + " Charachters";
                toolStripStatusLabel3.Text = t.Result.lineCount + " Lines";

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }

        //When user switches to a new tab display new word count, charachter count and line count
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            
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
                                string new_fname = tabPage.Text.Remove(tabPage.Text.Length - 1);

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
                    foreach (string word in wordList.words)
                    {
                        string find = word;
                        if (getRichTextBox().Text.Contains(find))
                        {
                            var matchString = Regex.Escape(find);
                            foreach (Match match in Regex.Matches(getRichTextBox().Text, matchString))
                            {
                                getRichTextBox().Select(match.Index, find.Length);
                                getRichTextBox().SelectionColor = Color.Black;
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
}
}

    
