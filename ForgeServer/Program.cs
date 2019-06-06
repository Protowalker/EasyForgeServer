using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ForgeServer
{
    public class Program : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Process process;
        StreamWriter standardInput;

        public string ConsoleOutput
        {
            get => consoleOutput.ToString();
        }
        private StringBuilder consoleOutput = new StringBuilder();


        public bool ServerRunning { get => serverRunning; private set
            {
                serverRunning = value;
                NotifyPropertyChanged("ServerRunning");
            }
        }
        private bool serverRunning = false;

        public string JarfileName
        {
            get => jarfileName;
            private set
            {
                jarfileName = value;
                NotifyPropertyChanged("JarfileName");
            }
        }
        private string jarfileName;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetupServer()
        {
            process = new Process();

            process.StartInfo = new ProcessStartInfo("java", "-jar " + '"' + Properties.Settings.Default.ServerDirectory +"\\" + JarfileName + '"' + " nogui");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.WorkingDirectory = Properties.Settings.Default.ServerDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new DataReceivedEventHandler(OnConsoleOutput);
            process.Exited += new EventHandler(OnServerExited);
        }

        public string ChooseServerDirectory()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.EnsurePathExists = true;
            dialog.Title = "Choose Server Directory...";
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }

        public bool TryInstall()
        {
            foreach (string path in Directory.GetFiles(ForgeServer.Properties.Settings.Default.ServerDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                string filename = path.Split('\\').Last();
                if (Regex.IsMatch(filename, @"forge\S+installer\.jar$"))
                {
                    InstallServer(filename);
                    return true;
                }
            }
            //If no files found, return false
            return false;
        }

        private void InstallServer(string filename)
        {
            
            process = new Process();

            process.StartInfo = new ProcessStartInfo("java", "-jar " + '"' + Properties.Settings.Default.ServerDirectory + "\\" + filename + '"' + " --installServer");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.WorkingDirectory = Properties.Settings.Default.ServerDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new DataReceivedEventHandler(OnConsoleOutput);
            process.Exited += new EventHandler(OnServerExited);

            bool startSuccessful = process.Start();
            if (startSuccessful)
            {
                process.BeginOutputReadLine();
                standardInput = process.StandardInput;
                ServerRunning = true;
            }
            else
            {
                MessageBox.Show("Installer Failed to start! Make sure you've run the respective version of Minecraft at least once.", "Installer Failed To Start", MessageBoxButton.OK);
            }
        }

        public bool DoForgeCheck()
        {
            foreach(string path in Directory.GetFiles(ForgeServer.Properties.Settings.Default.ServerDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                string filename = path.Split('\\').Last();
                if (Regex.IsMatch(filename, @"forge\S+universal\.jar$"))
                {
                    
                    JarfileName = filename;
                    return true;
                }
            }
            //If no files found, return false
            return false;
        }
        public bool DoEULACheck()
        {
            bool eulaAgreed = false;

            foreach (string path in Directory.GetFiles(ForgeServer.Properties.Settings.Default.ServerDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                if (Regex.IsMatch(path, @"/(eula\\.txt)/g"))
                {
                    JarfileName = path.Split('\\').Last();
                    StreamReader eula = new StreamReader(path);
                    string line;
                    while((line = eula.ReadLine()) != null)
                    {
                        if(Regex.IsMatch(line, "/(eula=true)\\g"))
                        {
                            eulaAgreed = true;
                            break;
                        }
                    }
                    eula.Close();
                    break;
                }
            }
            return eulaAgreed;
        }

        public void StartServer()
        {
            if (DoForgeCheck())
            {
                SetupServer();
                bool startSuccessful = process.Start();
                if (startSuccessful)
                {
                    process.BeginOutputReadLine();
                    standardInput = process.StandardInput;
                    ServerRunning = true;
                }
                else
                {
                    MessageBox.Show("Server Failed to start! Make sure server directory is set to the proper location.", "Server Failed To Start", MessageBoxButton.OK);
                }
            }
        }

        public void StopServer()
        {
            try
            {
                standardInput.WriteLine("stop");
            } catch(Exception e)
            {
            }
        }

        public void WriteToConsole(string input)
        {
            consoleOutput.AppendLine(">" + input);
            NotifyPropertyChanged("ConsoleOutput");
            standardInput.WriteLine(input);
        }


        public void OnConsoleOutput(object sender, DataReceivedEventArgs e)
        {
            consoleOutput.AppendLine(e.Data);
            NotifyPropertyChanged("ConsoleOutput");
        }

        public void OnProgramExit(object sender, ExitEventArgs e)
        {
            if(ServerRunning)
                StopServer();
        }

        private void OnServerExited(object sender, EventArgs e)
        {
            if (ServerRunning == false) return;

            process.CancelOutputRead();
            consoleOutput.Clear();
            standardInput.Close();
            standardInput = null;
            ServerRunning = false;
        }
    }
}
