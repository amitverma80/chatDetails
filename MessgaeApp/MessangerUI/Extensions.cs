using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessangerUI
{
    internal static class Extensions
    {
        public static void AppendLine(this TextBox textBox, string line)
        {
            line= DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + " - " + line;
            textBox.Text = textBox.Text + line.Replace("\n", Environment.NewLine) + Environment.NewLine;
            if (textBox.Visible)
            {
                textBox.SelectionStart = textBox.TextLength;
                textBox.ScrollToCaret();
            }
        }

        public static void Clear(this TextBox textBox)
        {
            textBox.Text = string.Empty;
        }
    }
}
