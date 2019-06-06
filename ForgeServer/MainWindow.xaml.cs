using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WPFCustomMessageBox;

namespace ForgeServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Boolean AutoScroll = true;
        public SolidColorBrush infoBrush;
        public SolidColorBrush commandBrush;
        public SolidColorBrush warnBrush;
        public SolidColorBrush errorBrush;
        private bool restarting = false;


        public MainWindow()
        {
            InitializeComponent();
            infoBrush = Brushes.Black;
            commandBrush = Brushes.Blue;
            warnBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffff35"));
            errorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e02d0d"));

            ((Program)Application.Current.Properties["program"]).PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnProgramPropertyChanged);
            string[] dirAsPath = (Properties.Settings.Default.ServerDirectory).Split('\\');
            Title = dirAsPath[dirAsPath.Length - 1];
        }

        private void OnProgramPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var program = ((Program)sender);

            if (e.PropertyName == "ConsoleOutput")
            {
                string[] stringList = program.ConsoleOutput.Split(new[] { '\n' }, StringSplitOptions.None);
                UpdateConsole(stringList);
            } else if(e.PropertyName == "ServerRunning")
            {
                if (restarting & !program.ServerRunning)
                {
                    program.StartServer();
                    WriteToConsole("Server Starting\n", Brushes.Green);
                    restarting = false;
                }

                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    console_input.IsEnabled = program.ServerRunning;
                    start_button.IsEnabled = !program.ServerRunning;
                    stop_button.IsEnabled = program.ServerRunning;
                    restart_button.IsEnabled = program.ServerRunning;
                });
                
            }
        }

        private void WriteToConsole(string text, Brush brush)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                
                consoleOutputBlock.Inlines.Add(new Run(text) { Foreground = brush });
            });
        }
        private void UpdateConsole(string[] stringList) {
            var newString = stringList[stringList.Length - 2];

            Brush brush;

            if (newString.StartsWith(">"))
                brush = commandBrush;
            else if (newString.Contains("/WARN]"))
                brush = warnBrush;
            else if (newString.Contains("/ERROR]"))
                brush = errorBrush;
            else
                brush = infoBrush;

            WriteToConsole(newString, brush);
        }

        private void OnScrollViewerScrollChanged(Object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    AutoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    AutoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (AutoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }

        private void OnConsoleInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var textbox = (TextBox)sender;
            ((Program)Application.Current.Properties["program"]).WriteToConsole(textbox.Text);
            textbox.Clear();
            e.Handled = true;

        }

        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            Program program = ((Program)Application.Current.Properties["program"]);
            bool forgeInstalled = program.DoForgeCheck();
            bool eulaAgreed = program.DoEULACheck();

            if (!forgeInstalled)
            {
                if (!program.TryInstall())
                {
                    AskToInstallForge();
                } else
                {
                    WriteToConsole("Installing Server...", commandBrush);
                }
            } else if (!eulaAgreed)
            {
                eulaAgreed = AskToAgreeToEULA();
            }
            if(forgeInstalled && eulaAgreed)
            {
                WriteToConsole("Server Starting\n", Brushes.Green);
                ((Program)Application.Current.Properties["program"]).StartServer();
                restart_button.IsEnabled = false;
                stop_button.IsEnabled = false;
                start_button.IsEnabled = false;
            }

        }

        private void AskToInstallForge()
        {
            MessageBoxResult result = MessageBox.Show("Cannot find your forge jar. You can go to the download site by clicking OK below. Download the file marked Installer (NOT windows installer!) and place it in your server's directory.", "Forge Server Not found", MessageBoxButton.OKCancel);
            if(result == MessageBoxResult.OK)
            {
                System.Diagnostics.Process.Start("https://files.minecraftforge.net/");
            }
        }

        private bool AskToAgreeToEULA()
        {
            MessageBoxResult result = CustomMessageBox.ShowYesNoCancel("By clicking Agree you hereby agree to the Mojang EULA (End User License Agreement).", "EULA", "Read EULA", "Agree", "Disagree");

            while(result == MessageBoxResult.Yes)
            {
                Process.Start("https://account.mojang.com/documents/minecraft_eula");
                result = CustomMessageBox.ShowYesNoCancel("By clicking Agree you hereby agree to the Mojang EULA (End User License Agreement).", "EULA", "Read", "Agree", "Disagree");
            }

            if (result == MessageBoxResult.No)
            {
                StreamWriter writer = new StreamWriter(ForgeServer.Properties.Settings.Default.ServerDirectory + "\\eula.txt");
                writer.WriteLine("eula=true");
                writer.Close();
                return true;
            } else
            {
                return false;
            }
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            WriteToConsole("Server Shutting Down\n", Brushes.Red);
            ((Program)Application.Current.Properties["program"]).StopServer();
            restart_button.IsEnabled = false;
            stop_button.IsEnabled = false;
            start_button.IsEnabled = false;
        }

        private void OnRestartButtonClick(object sender, RoutedEventArgs e)
        {
            ((Program)Application.Current.Properties["program"]).StopServer();
            WriteToConsole("Server Shutting Down\n", Brushes.Red);
            restart_button.IsEnabled = false;
            stop_button.IsEnabled = false;
            start_button.IsEnabled = false;

            restarting = true;
        }

        private void OnChooseServerButtonClick(object sender, RoutedEventArgs e)
        {
            Program program = ((Program)Application.Current.Properties["program"]);
            string directory = program.ChooseServerDirectory();
            if(directory != null)
            {
                ForgeServer.Properties.Settings.Default.ServerDirectory = directory;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
                program.StopServer();

                program.SetupServer();

                string[] dirAsPath = directory.Split('\\');
                Title = dirAsPath[dirAsPath.Length - 1];
            }
        }
    }
}
