using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textual
{
    public partial class MainForm : Form
    {
        // Mutual exclusion for the richTextBox in given tab
        private Mutex rtbMutex;

        //Mutex for saving rich text box content
        private Mutex saveMutex;

        //Class to manage of words for spell check
        private WordList wordList;

        //Font sizes
        private int[] fontSizes = { 11, 12, 14, 18, 21, 25 };

        //Fonts
        private List<FontFamily> fonts = System.Drawing.FontFamily.Families.ToList();

        //Timer for auto save
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        //Tick counter for timer
        private bool timerActive = false;

        //State of real time spell check
        private bool spellCheckEnabled = false;

        public MainForm()
        {
            InitializeComponent();
            rtbMutex = new Mutex();
            wordList = new WordList();
            saveMutex = new Mutex();
            setComboBoxItems();

            //Initialize time
            timer.Interval = 120000;
            timer.Tick += timerMethod;
        }

        //Auto saving mechanism with timer (Every 2 minutes an auto save will be triggered)
        private void timerMethod(object sender, EventArgs e)
        {
            if (timerActive)
            {
                timer.Stop();
                saveAll();
                timer.Start();
            }
        }

        //Method to set items to combo box
        private void setComboBoxItems()
        {
            //Add font sizes to font size combo box
            foreach (int i in fontSizes)
            {
                toolStripComboBox_sizes.Items.Add(i);
            }
            
            //Add all fonts in FontFamily class to font combo box
            foreach (FontFamily i in fonts)
            {
                toolStripComboBox_fonts.Items.Add(i.Name);
            }

            //Enable combo boxes for fonts and font sizes
            toolStripComboBox_fonts.Enabled = false;
            toolStripComboBox_sizes.Enabled = false;
        }

        //Method to create a new tab
        private void makeNewTab()
        {
            try
            {
                //Create new TabPage and RichTextBox objects    
                TabPage tabPage = new TabPage();
                RichTextBox richTextBox = new RichTextBox();
                //Assign event handlers to RichTextBox
                richTextBox.TextChanged += new EventHandler(richTextBox_TextChanged);
                richTextBox.KeyPress += new KeyPressEventHandler(richTextBox_KeyPress);

                richTextBox.ForeColor = Color.Black;
                richTextBox.Dock = DockStyle.Fill;

                //Add the rich text box to the created tab
                tabPage.Controls.Add(richTextBox);

                //Add the tab to the window
                tabControl.Controls.Add(tabPage);
                resetCounts();

                tabControl.SelectedTab = tabPage;

                //Title of the newly opened tab
                tabPage.Text = "untitled";
                tabPage.Tag = "untitled";
                filename_toolStripLabel.Text = "untitled";

                //Enable combo boxes
                toolStripComboBox_fonts.Enabled = true;
                toolStripComboBox_sizes.Enabled = true;

                richTextBox.ContextMenuStrip = contextMenuStrip;

                //Activate timer when first tab is open
                if (!timerActive)
                {
                    timer.Start();
                    timerActive = true;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        //Method to close the current tab
        private void closeCurrentTab()
        {
            if (tabControl.SelectedTab != null) 
            {
                //If tab doesn't have unsaved changes
                if (!tabControl.SelectedTab.Text.StartsWith("*"))
                {
                    tabControl.TabPages.Remove(tabControl.SelectedTab);
                    resetCounts();
                }
                else
                {
                    //Display a message to the user alerting that there are unsaved changes
                    DialogResult result = MessageBox.Show("You haven't saved your changes!", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    switch (result) {

                        //User clicks "Yes"
                        case DialogResult.Yes:
                            save();
                            break;
                        //User clicks "No"
                        case DialogResult.No:
                            tabControl.TabPages.Remove(tabControl.SelectedTab);
                            resetCounts();
                            break;
                        //User clicks "Cancel"
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

        /* Method to save the contents of a tab */
        private void save()
        {
            //Get the seelcted tab
            TabPage selectedTab = tabControl.SelectedTab;

            if (selectedTab != null)
            {
                //If opened document is an ecrypted document make the user re-encrypt and save
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
                                string filepath = filename;
                                string rtb_content = getRichTextBox().Rtf;

                                //Create a new task to save the rich text box content
                                Task task = new Task(() =>
                                {
                                    //Aquire the mutex
                                    bool saveLock = saveMutex.WaitOne();

                                    try
                                    {
                                        //Save file
                                        File.WriteAllText(filepath, "");
                                        StreamWriter streamWriter = File.AppendText(filepath);
                                        streamWriter.Write(rtb_content);
                                        streamWriter.Close();
                                        streamWriter.Dispose();

                                    }
                                    catch (Exception ex)
                                    {
                                        throw ex;
                                    }
                                    finally
                                    {
                                        //Release mutex
                                        if (saveLock) saveMutex.ReleaseMutex();
                                    }
                                });

                                task.ContinueWith(t =>
                                {

                                    //Remove saved status (*)
                                    if (selectedTab.Text.Contains("*"))
                                    {
                                        selectedTab.Text = selectedTab.Text.Remove(0, 1);
                                    }

                                }, TaskScheduler.FromCurrentSynchronizationContext());

                                //If there were any exceptions that occured during the task execution
                                task.ContinueWith(t =>
                                {
                                    task.Exception.Handle(ex =>
                                    {
                                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return false;
                                    });

                                }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

                                task.Start();
                            }
                        }
                    }
                    else
                    {
                        saveAs();
                    }
                }
            }
        }

        /*SAVE AS*/
        private void saveAs()
        {
            if (tabControl.SelectedTab != null)
            {
                TabPage tabPage = tabControl.SelectedTab;

                //Only allow user to save document as .rtf
                saveFileDialog.Filter = "Rich Text Format|*.rtf";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get filename and content to be saved
                    string filename = saveFileDialog.FileName;
                    string rtb_content = getRichTextBox().Rtf;

                    //Create a new task to carry out saving
                    Task task = new Task(() =>
                    {
                        bool saveLock = saveMutex.WaitOne();

                        try
                        {
                            File.WriteAllText(filename, "");
                            StreamWriter streamWriter = File.AppendText(filename);
                            streamWriter.Write(rtb_content);
                            streamWriter.Close();
                            streamWriter.Dispose();

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            if (saveLock) saveMutex.ReleaseMutex();
                        }
                    });

                    //When task is done executing
                    task.ContinueWith(t =>
                    {

                        //Remove saved status (*)
                        if (tabPage.Text.Contains("*"))
                        {
                            tabPage.Text = tabPage.Text.Remove(0, 1);
                        }

                        //Simplified filename
                        string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                        //Set tab title
                        tabPage.Text = fname;

                        //Set filepath as tag
                        tabPage.Tag = filename;

                        //Set the filepath as the tool strip label
                        filename_toolStripLabel.Text = filename;

                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    //If there were any exceptions that occured during the task execution
                    task.ContinueWith(t =>
                    {
                        task.Exception.Handle(ex =>
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        });

                    }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

                    task.Start();
                }
            }
        }

        /*SAVE ALL*/
        private void saveAll()
        {
            //If tabs are open
            if(tabControl.TabCount > 0)
            {

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

                    if (!tab_text.EndsWith(".texx"))
                    {
                        filepath = (string)tab.Tag;

                        //If tab is not an untitled tab and a filepath exists
                        if (filepath != "untitled")
                        {
                            //Start a new task to save the content of the tab's rich text box
                            Task task = new Task(() =>
                            {
                                bool saveLock = saveMutex.WaitOne();
                                try
                                {
                                    File.WriteAllText(filepath, "");
                                    StreamWriter streamWriter = File.AppendText(filepath);
                                    streamWriter.Write(rtb_content);
                                    streamWriter.Close();
                                    streamWriter.Dispose();
                                }
                                catch (Exception ex)
                                {
                                    //Throw any errors
                                    throw ex;
                                }
                                finally
                                {
                                    if (saveLock) saveMutex.ReleaseMutex();
                                }
                            });

                            task.ContinueWith(t =>
                            {
                                //Remove saved status (*)
                                if (tab_text.Contains("*"))
                                {
                                    tab.Text = tab.Text.Remove(0, 1);
                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());

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
        }


        //When "Open" is clicked
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //Only allow users to open rich text format or encrypted documents of textual
                openFileDialog.Filter = "Textual Files|*.rtf;*texx";
                openFileDialog.Multiselect = false;
                openFileDialog.FileName = "";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filename = openFileDialog.FileName;

                    //If file is an encrypted file
                    if (filename.EndsWith(".texx"))
                    {
                        //Open the DecryptForm window
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

                        RichTextBox richTextBox = getRichTextBox();
                        TabPage tabPage = tabControl.SelectedTab;

                        //Create a new task that reads the document and returns the content as a string
                        Task<string> task = new Task<string>(() =>
                        {
                            try
                            {
                                StreamReader strReader = new StreamReader(filename);
                                string content = strReader.ReadToEnd();
                                strReader.Close();
                                return content;

                            }
                            catch(Exception ex)
                            {
                                throw ex;
                            }

                        });

                        task.ContinueWith(t =>
                        {
                            richTextBox.Rtf = t.Result;

                            //Set the tab title and toolstrip label
                            string fname = filename.Substring(filename.LastIndexOf("\\") + 1);
                            tabPage.Tag = filename;
                            tabPage.Text = fname;
                            filename_toolStripLabel.Text = filename;

                        }, TaskScheduler.FromCurrentSynchronizationContext());

                        //Catch any errors and display (This occurs only when an error is thrown from the task)
                        task.ContinueWith(t =>
                        {

                            task.Exception.Handle(ex =>
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            });

                        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

                        task.Start();

                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //Method called by DecryptForm class after user inputs a password
        public void decryptAndOpen(string password, string filename)
        {
            //Task used to read document and decrypt it
            Task<string> task = new Task<string>(() =>
            {
                try
                {
                    StreamReader streamReader = new StreamReader(filename);
                    string rtb_content = streamReader.ReadToEnd();
                    streamReader.Close();
                    string decrypted = Encryptor.Decrypt(rtb_content, password);
                    return decrypted;
                }catch(Exception ex)
                {
                    throw ex;
                }
            });

            //After task ends
            task.ContinueWith(t =>
            {
                string decrypted = t.Result;

                //If successfully decrypted
                if (decrypted.StartsWith("ENCRYPTED"))
                {
                    //If no unedited untitled tab exists or if no tabs are open
                    if (tabControl.SelectedTab == null || !filename_toolStripLabel.Text.Equals("untitled") || tabControl.SelectedTab.Text.Contains("*"))
                    {
                        makeNewTab();
                    }

                    decrypted = decrypted.Substring(9);

                    getRichTextBox().Rtf = decrypted;
                    TabPage tabPage = tabControl.SelectedTab;
                    tabPage.Tag = filename;
                    //Simplified filename
                    string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

                    tabPage.Text = fname;
                    filename_toolStripLabel.Text = filename;
                }
                else
                //If not decrypted successfully 
                {
                    throw new CryptographicException();
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());

            //Catch any errors and display (This occurs only when an error is thrown from the task)
            task.ContinueWith(t =>
            {
                
                task.Exception.Handle(ex =>
                {
                    //CryptographicException is thrown if the password is incorrect
                    if (ex is CryptographicException)
                    {
                        MessageBox.Show("Invalid Password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return false;
                });

            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

            task.Start();

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



        //When "Print Preview" is clicked
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                printPreviewDialog.Document = printDocument;
                printPreviewDialog.ShowDialog();
            }
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
                        toolStripStatusLabel2.Text = t.Result.charachterCount + " Characters";
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
                    filename_toolStripLabel.Text = (string)tabPage.Tag;
                }
            }
            else
            {
                filename_toolStripLabel.Text = "No files open";

                //Disbale combo boxes
                toolStripComboBox_fonts.Enabled = false;
                toolStripComboBox_sizes.Enabled = false;

                //Deactivate Timer
                timer.Stop();
                timerActive = false;
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
            string text = getRichTextBox().Text;

            //Task to get all words of the rich text box in selected tab
            Task<string[]> task = new Task<string[]>(() =>
            {
                bool aLock = rtbMutex.WaitOne();

                try
                {
                    //Create array of strings by splitting rtb content and return result
                    char[] delimiters = new char[] { ' ', '\r', '\n' };
                    string[] words = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    return words;
                }
                finally
                {
                    if (aLock) rtbMutex.ReleaseMutex();
                }

            });

            task.ContinueWith(t =>
            {
                //Using the result obtainted from initial function perform spell check
                string[] words = t.Result;

                foreach (string a_word in words)
                {
                    string word = a_word.ToLower();

                    bool aLock = rtbMutex.WaitOne();
                    //word = (word.Contains('.')) ? word.Remove('.') : word;

                    //Create a match string using the selected word to be checked
                    var matchString = Regex.Escape(word);

                    try
                    {
                        //Check if wordlist object contains the selected word if it doesn't spelling is incorrect and should be highlighted in red
                        if (!wordList.words.Contains(word))
                        {
                            foreach (Match match in Regex.Matches(getRichTextBox().Text, matchString))
                            {
                                getRichTextBox().Select(match.Index, word.Length);
                                getRichTextBox().SelectionColor = Color.Red;
                                getRichTextBox().Select(getRichTextBox().TextLength, 0);
                                getRichTextBox().SelectionColor = getRichTextBox().ForeColor;
                            };
                        }
                        //If spelling is correct word color is set to black
                        else
                        {
                            foreach (Match match in Regex.Matches(getRichTextBox().Text, matchString))
                            {
                                getRichTextBox().Select(match.Index, word.Length);
                                getRichTextBox().SelectionColor = Color.Black;
                                getRichTextBox().Select(getRichTextBox().TextLength, 0);
                                getRichTextBox().SelectionColor = getRichTextBox().ForeColor;
                            };
                        }
                    }
                    finally
                    {
                        //Finally release the mutex
                        if (aLock) rtbMutex.ReleaseMutex();
                    }
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());

            //Carry out task only if spell check is enabled
            if (spellCheckEnabled)
            {
                task.Start();
            }
            
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

        //Change title and toolstrip label when encryted and saved
        public void changeTitleLabel(string filename)
        {
            //Get tabpage
            TabPage tabPage = tabControl.SelectedTab;

            //Simplified filename
            string fname = filename.Substring(filename.LastIndexOf("\\") + 1);

            //Set tab title
            tabPage.Text = fname;

            //Set the filepath as the tool strip label
            filename_toolStripLabel.Text = filename;
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
            if (tabControl.SelectedTab != null) getRichTextBox().SelectAll();
        }

        //When "About" button is clicked
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.Show();
        }

        //When "Bold" button is clicked
        private void toolStripButton_bold_Click(object sender, EventArgs e)
        {
            try
            {
                if (tabControl.SelectedTab != null)
                {
                    RichTextBox rtbCaseContent = getRichTextBox();

                    if (rtbCaseContent.SelectedText.Length > 0)
                    {
                        FontStyle style = FontStyle.Bold;
                        Font selectedFont;

                        //If multiple fonts are selected
                        if (rtbCaseContent.SelectionFont == null)
                        {
                            selectedFont = new Font(new Font(FontFamily.GenericSansSerif, 12), style);
                        }
                        else
                        {
                            selectedFont = rtbCaseContent.SelectionFont;

                            //If already bold convert to regular
                            if (rtbCaseContent.SelectionFont.Bold == true)
                            {
                                style = FontStyle.Regular;
                            }
                            //If previously italic, keep italic property
                            if (rtbCaseContent.SelectionFont.Italic == true)
                            {
                                style |= FontStyle.Italic;
                            }
                            //If previously underlined, keep underline property
                            if (rtbCaseContent.SelectionFont.Underline == true)
                            {
                                style |= FontStyle.Underline;
                            }
                        }

                        rtbCaseContent.SelectionFont = new Font(selectedFont, style);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //When "Italic" button is clicked
        private void toolStripButton_italic_Click(object sender, EventArgs e)
        {
            try
            {
                if (tabControl.SelectedTab != null)
                {
                    RichTextBox rtbCaseContent = getRichTextBox();

                    if (rtbCaseContent.SelectedText.Length > 0)
                    {
                        FontStyle style = FontStyle.Italic;
                        Font selectedFont;

                        //If multiple fonts are selected
                        if (rtbCaseContent.SelectionFont == null)
                        {
                            selectedFont = new Font(new Font(FontFamily.GenericSansSerif, 12), style);
                        }
                        else
                        {
                            selectedFont = rtbCaseContent.SelectionFont;

                            //If already italic convert to regular
                            if (rtbCaseContent.SelectionFont.Italic == true)
                            {
                                style = FontStyle.Regular;
                            }
                            //If previously underlined, keep underline property
                            if (rtbCaseContent.SelectionFont.Underline == true)
                            {
                                style |= FontStyle.Underline;
                            }
                            //If previously bold, keep bold property
                            if (rtbCaseContent.SelectionFont.Bold == true)
                            {
                                style |= FontStyle.Bold;
                            }
                        }

                        rtbCaseContent.SelectionFont = new Font(selectedFont, style);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //When "Underline" button is clicked
        private void toolStripButton_underline_Click(object sender, EventArgs e)
        {
            try
            {
                if (tabControl.SelectedTab != null)
                {
                    RichTextBox rtbCaseContent = getRichTextBox();

                    if (rtbCaseContent.SelectedText.Length > 0)
                    {
                        FontStyle style = FontStyle.Underline;
                        Font selectedFont; 

                        //If multiple fonts are selected
                        if(rtbCaseContent.SelectionFont == null)
                        {
                            selectedFont = new Font(new Font(FontFamily.GenericSansSerif, 12), style);
                        }
                        else
                        {
                            selectedFont = rtbCaseContent.SelectionFont;

                            //If selected text has been underlined, remove property
                            if (rtbCaseContent.SelectionFont.Underline == true)
                            {
                                style = FontStyle.Regular;
                            }
                            //If already italic keep property
                            if (rtbCaseContent.SelectionFont.Italic == true)
                            {
                                style |= FontStyle.Italic;
                            }
                            //If already bold keep property
                            if (rtbCaseContent.SelectionFont.Bold == true)
                            {
                                style |= FontStyle.Bold;
                            }
                        }

                        rtbCaseContent.SelectionFont = new Font(selectedFont, style);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //When font size is changed by the combo box
        private void toolStripComboBox_sizes_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (tabControl.SelectedTab != null)
                {
                    int index = toolStripComboBox_sizes.SelectedIndex;

                    //If multiple fonts are selected
                    FontFamily font = (getRichTextBox().SelectionFont == null) ? FontFamily.GenericSansSerif : getRichTextBox().SelectionFont.FontFamily;

                    if (index != -1) getRichTextBox().SelectionFont = new Font(font, fontSizes[index]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //When the font is changed by the combo box
        private void toolStripComboBox_fonts_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {

                if (tabControl.SelectedTab != null)
                {
                    int index = toolStripComboBox_fonts.SelectedIndex;

                    //If multiple sizes are selected
                    float size = (getRichTextBox().SelectionFont == null) ? 12 : getRichTextBox().SelectionFont.Size;

                    if (index != -1) getRichTextBox().SelectionFont = new Font(fonts[index], size);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //Method to add an image
        private void addImage()
        {
            if (tabControl.SelectedTab != null)
            {
                try
                {
                    //Only able to get image type files
                    openFileDialog.Filter = "Images |*.bmp;*.jpg;*.png;*.gif;*.ico";
                    openFileDialog.Multiselect = false;
                    openFileDialog.FileName = "";
                    DialogResult result = openFileDialog.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        //Get the selected image
                        Image img = Image.FromFile(openFileDialog.FileName);
                        //Add the image to the clipboard
                        Clipboard.SetImage(img);
                        //Paste the image on the rich text box
                        getRichTextBox().Paste();
                        getRichTextBox().Focus();
                    }
                    else
                    {
                        getRichTextBox().Focus();
                    }
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
            }
        }

        //When "Add Image" button is clicked
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            addImage();
        }

        //When "Enable/Disable Spell check" button is clicked
        private void enableSpellCheckToolStripMenuItemm_Click(object sender, EventArgs e)
        {
            //If spell check is already enabled
            if (spellCheckEnabled)
            {
                spellCheckEnabled = false;
                enableSpellCheckToolStripMenuItem.Text = "Enable &Spell Check";
            }
            else
            {

                DialogResult result = MessageBox.Show("Please note that real time spell check will be buggy for large files. (Rich text boxes with a lot of content.)", "Enable Real Time Spell Check", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                switch (result)
                {
                    case DialogResult.OK:
                        spellCheckEnabled = true;
                        enableSpellCheckToolStripMenuItem.Text = "Disable &Spell Check";
                        break;
                    case DialogResult.Cancel:
                        break;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (tabControl.TabCount > 0)
            {
                //Get all the tabs open in the system
                TabControl.TabPageCollection tabPageCollection = tabControl.TabPages;

                //Loop through all tab pages
                foreach (TabPage tab in tabPageCollection)
                {
                    tabControl.SelectedTab = tab;
                    closeCurrentTab();
                }
            }
        }
    }
}

    
