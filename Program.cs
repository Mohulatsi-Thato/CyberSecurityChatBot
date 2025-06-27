// Program.cs
using System;
using System.Windows.Forms;

namespace CyberChatBotGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());   // ← launches the form above
        }
    }
}
