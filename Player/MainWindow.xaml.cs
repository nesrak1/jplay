using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using JAudio;

namespace JAudioPlayer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Help_About_Click(object sender, RoutedEventArgs e)
        {
            About aboutWin = new About();
            aboutWin.Owner = this;
            aboutWin.ShowDialog();
        }

        private void File_Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\JAudio Player";
                string file = appData + @"\Z2Sound.dat";
                string path = string.Empty;

                if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);

                if (File.Exists(file))
                {
                    StreamReader reader = new StreamReader(new FileStream(file, FileMode.Open));
                    path = reader.ReadLine();
                    reader.Dispose();
                }
                else
                {
                    FolderBrowserDialog dlg = new FolderBrowserDialog();
                    dlg.Description = "Select the path that contains 'Z2Sound.baa' and the subdirectory 'Waves'.";
                    dlg.ShowNewFolderButton = false;

                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        StreamWriter writer = new StreamWriter(new FileStream(file, FileMode.Create));
                        writer.WriteLine(dlg.SelectedPath);
                        writer.Dispose();

                        path = dlg.SelectedPath;
                    }
                    else
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                }

                playback = new Playback(path);

                if (Environment.GetCommandLineArgs().Length > 1)
                {
                    this.Title = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]) + " - JAudio Player";

                    JAudio.Sequence.Bms seq = new JAudio.Sequence.Bms(System.IO.File.OpenRead(Environment.GetCommandLineArgs()[1]));
                    playback.Sequence = seq;
                    Playback_Start(null, null);
                }
            }

            catch (SharpDX.SharpDXException)
            {
                System.Windows.MessageBox.Show("Could not initialize the audio interface.", "Load sequence", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Load sequence", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Playback playback;

        private void Playback_Start(object sender, RoutedEventArgs e)
        {
            try
            {
                playback.Start();
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Playback", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Playback_Stop(object sender, RoutedEventArgs e)
        {
            playback.Stop();
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.Filter = "BMS sequences|*.bms";
            dlg.FilterIndex = 0;
            dlg.Multiselect = false;
            dlg.Title = "Open sequence";

            if (dlg.ShowDialog(this) == true)
            {
                this.Title = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + " - JAudio Player";

                if (playback.IsPlaying) playback.Stop();
                playback.Sequence = new JAudio.Sequence.Bms(System.IO.File.OpenRead(dlg.FileName));
                playback.Start();
            }
        }
    }
}
