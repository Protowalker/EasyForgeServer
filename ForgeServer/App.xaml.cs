using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            string serverDirectory = ForgeServer.Properties.Settings.Default.ServerDirectory;
            if (serverDirectory == null || serverDirectory == "")
            {
                SetupDirectory();
            }

            Program = new Program();
            Program.SetupServer();
            Current.Properties.Add("program", Program);
            Exit += new ExitEventHandler(Program.OnProgramExit);
        }


        private void SetupDirectory()
        {
            // Configure the message box to be displayed
            string messageBoxText = "Hello! You have no directory chosen for your server. Press OK to choose a directory where you would like your server to be (if a server is already there, that's alright. Nothing will be overwritten)";
            string caption = "No Server Directory";
            MessageBoxButton button = MessageBoxButton.OKCancel;

            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button);
            if (result == MessageBoxResult.OK)
            {
                string directory = Program.ChooseServerDirectory();
                while(directory == null)
                {
                    messageBoxText = "Choosing a directory is required. Please try again.";
                    result = MessageBox.Show(messageBoxText, caption, button);
                    if (result == MessageBoxResult.Cancel)
                    {
                        Application.Current.Shutdown(0);
                        break;
                    }
                    directory = Program.ChooseServerDirectory();
                }
                ForgeServer.Properties.Settings.Default.ServerDirectory = directory;
            }
            else
            {
                Application.Current.Shutdown(0);
            }

        }


        protected void OnAppExit(object sender, ExitEventArgs e)
        {
            ForgeServer.Properties.Settings.Default.Save();
        }
    }
}
