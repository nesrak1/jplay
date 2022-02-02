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
using JAudioPlayer;
using Button = System.Windows.Controls.Button;
using WinApp = System.Windows.Application;
using Path = System.IO.Path;
using MessageBox = System.Windows.MessageBox;
using JAudio.Tools;

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
            WinApp.Current.Shutdown();
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

                        string wavesFolder = Path.Combine(path, "Waves");

                        if (!Directory.Exists(wavesFolder))
                            Directory.CreateDirectory(wavesFolder);

                        if (Directory.GetFiles(wavesFolder, "*.wav").Length == 0)
                        {
                            MessageBoxResult res = MessageBox.Show(
                                "No wave files were detected in Waves. Would you like to start baad/wsys extraction?",
                                "JPlay", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (res == MessageBoxResult.Yes)
                            {
                                Title = "JAudio Player v1.0 Alpha - Running baad";
                                if (!baad.Convert(File.OpenRead(Path.Combine(path, "Z2Sound.baa")), Path.Combine(path, "Z2Sound.baa")))
                                {
                                    MessageBox.Show("An error occurred in baad.\nApplication will now exit.");
                                    WinApp.Current.Shutdown();
                                }
                                Title = "JAudio Player v1.0 Alpha - Running wsyster";
                                string[] wsysFiles = new[] { "Z2Sound.baa.0.wsys", "Z2Sound.baa.1.wsys" };
                                foreach (string wsysFile in wsysFiles)
                                {
                                    if (!wsyster.Convert(File.OpenRead(Path.Combine(path, wsysFile)), out string error))
                                    {
                                        MessageBox.Show("An error occurred in wsyster.\nMessage: " + error + "\nApplication will now exit.");
                                        WinApp.Current.Shutdown();
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("If you do not extract waves, the player will most likely not work.", "JPlay");
                            }
                        }
                    }
                    else
                    {
                        WinApp.Current.Shutdown();
                    }
                }

                playback = new Playback(path);

                if (Environment.GetCommandLineArgs().Length > 1)
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]) + " - JAudio Player";

                    JAudio.Sequence.Bms seq = new JAudio.Sequence.Bms(File.OpenRead(Environment.GetCommandLineArgs()[1]));
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
        private int lastTempo;

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
                Title = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + " - JAudio Player";

                if (playback.IsPlaying) playback.Stop();
                playback.Sequence = new JAudio.Sequence.Bms(File.OpenRead(dlg.FileName));
                InstrumentList.Children.Clear();
                for (int i = 0; i < playback.tracks.Count; i++)
                {
                    Playback.Track t = playback.tracks[i];
                    Button b = new Button()
                    {
                        Content = $"Instrument {i}/0: [enabled]"
                    };
                    InstData d = new InstData()
                    {
                        lastInstrument = 0,
                        track = t,
                        trackId = i,
                        bn = b
                    };
                    instDatas.Add(d);
                    b.Tag = d;
                    b.Click += InstClick;
                    InstrumentList.Children.Add(b);
                }
                playback.TickUpdate += UIUpdate;
                playback.Start();
            }
        }

        private void UIUpdate()
        {
            WinApp.Current.Dispatcher.InvokeAsync(new Action(() =>
            {
                if (playback != null && playback.IsPlaying)
                {
                    PositionSlider.Value = playback.Time;
                }
            }));
            if (playback.Tempo != lastTempo)
            {
                lastTempo = playback.Tempo;
                WinApp.Current.Dispatcher.InvokeAsync(new Action(() =>
                {
                    SpeedSlider.Value = playback.Tempo;
                }));
            }
            foreach (InstData instData in instDatas)
            {
                if (instData == null)
                    continue;
                if (instData.track.Instrument != instData.lastInstrument)
                {
                    instData.lastInstrument = instData.track.Instrument;
                    WinApp.Current.Dispatcher.InvokeAsync(new Action(() =>
                    {
                        instData.bn.Content = $"Instrument {instData.trackId}/{instData.lastInstrument}: [{(instData.track.Enabled ? "enabled" : "disabled")}]";
                    }));
                }
            }
        }

        private void InstClick(object sender, RoutedEventArgs e)
        {
            Button bn = e.OriginalSource as Button;
            InstData instData = bn.Tag as InstData;
            int trackNum = instData.trackId;
            Playback.Track tr = instData.track;
            int inst = tr.Instrument;
            if (tr.Enabled)
            {
                bn.Content = $"Instrument {trackNum}/{inst}: [disabled]";
                tr.Enabled = false;
                tr.AllNotesOff();
            }
            else
            {
                bn.Content = $"Instrument {trackNum}/{inst}: [enabled]";
                tr.Enabled = true;
            }
        }

        List<InstData> instDatas = new List<InstData>();

        private class InstData
        {
            public Button bn;
            public Playback.Track track;
            public int trackId;
            public int lastInstrument;
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (playback != null && playback.IsPlaying)
            {
                playback.Tempo = (int)SpeedSlider.Value;
            }
        }
    }
}
