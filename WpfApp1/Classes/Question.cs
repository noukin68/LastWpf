using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Classes
{
    public class Question
    {
        public string Text { get; set; }
        public List<string> Options { get; set; }
        public int CorrectIndex { get; set; }

        public Question(string text, List<string> options, int correctIndex)
        {
            Text = text;
            Options = options;
            CorrectIndex = correctIndex;
        }
    }

}
