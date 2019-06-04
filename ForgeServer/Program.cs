using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ForgeServer
{
    public class Program : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Process process;
        string serverDirectory = "C:/Users/Protowalker/Documents/EasyForgeServer";
        StreamWriter standardInput;

        public string ConsoleOutput
        {
            get => consoleOutput.ToString();
        }

        public bool ServerRunning { get => serverRunning; private set
            {
                serverRunning = value;
                NotifyPropertyChanged("ServerRunning");
            }
        }
        private bool serverRunning = false;

        private StringBuilder consoleOutput = new StringBuilder();

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Program()
        {
            process = new Process();

            process.StartInfo = new ProcessStartInfo("java", "-jar " + serverDirectory + "/forge-1.12.2-14.23.5.2838-universal.jar nogui");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.WorkingDirectory = serverDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new DataReceivedEventHandler(OnConsoleOutput);
            process.Exited += new EventHandler(OnServerExited);
        }

    
        public void StartServer()
        {
            process.Start();
            process.BeginOutputReadLine();
            standardInput = process.StandardInput;
            ServerRunning = true;
        }

        public void StopServer()
        {
            standardInput.WriteLine("stop");
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
