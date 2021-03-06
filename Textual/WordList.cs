﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textual
{
    class WordList
    {

        public string[] words { get; set; }

        public WordList()
        {

            try
            {
                //Import list of words from .txt file (located at ../bin/Debug/allwords.txt) 
                words = File.ReadAllText("allwords.txt").Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                //convert all words to lower case. Parallel for loop is used to decrease runtime by achieving data parallelism
                Parallel.For(0, words.Length, i =>
                 {
                     words[i] = words[i].ToLower();
                 });

            }
            catch(Exception e)
            {
                MessageBox.Show($"Something went wrong when importing disctionary.\n{e.Message}", "Error during dictionary import", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

    }
}
