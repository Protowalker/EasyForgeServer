using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ForgeServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Program Program { get; set; }

        void OnAppStartup(object sender, StartupEventArgs e)
        {
            Program = new Program();
            Current.Properties.Add("program", Program);
            Exit += new ExitEventHandler(Program.OnProgramExit);
        }

    }
}
