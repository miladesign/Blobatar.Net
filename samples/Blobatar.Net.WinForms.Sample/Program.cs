using System;
using System.Windows.Forms;

namespace Blobatar.Net.WinForms.Sample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
