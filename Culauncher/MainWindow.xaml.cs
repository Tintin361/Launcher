using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace Culauncher
{

    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayJimmyButton.Content = "Jouer au Jeu";
                        break;
                    case LauncherStatus.failed:
                        PlayJimmyButton.Content = "Échec de la MàJ";
                        break;  
                    case LauncherStatus.downloadingGame:
                        PlayJimmyButton.Content = "Téléchargement de la MàJ";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayJimmyButton.Content = "Téléchargement du Jeu";
                        break;
                    default:
                        break;
                }
            }
        }



        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version_Jimmy.txt");
            gameZip = Path.Combine(rootPath, "Jimmy.zip");
            gameExe = Path.Combine(rootPath, "Jimmy", "Game.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                JimmyVersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("http://82.66.170.147/jimmy/Version_Jimmy.txt"));


                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Erreur de téléchargement de la Mise à Jour: {ex} (C'est con ça !)");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("http://82.66.170.147/jimmy/Version_Jimmy.txt"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("http://82.66.170.147/jimmy/Jimmy.zip"), gameZip, _onlineVersion);

            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur dans l'installation des fichiers du Jeu: {ex} (C'est con ça !)");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                JimmyVersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur de téléchargement: {ex} (C'est con ça !)");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                JimmyVersionText.Text = localVersion.ToString();
            }
        }

        private void PlayJimmyButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Jimmy");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed || Status == LauncherStatus.ready)
            {
                CheckForUpdates();
            }
        }

        private void MoreJimmyButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"More button");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            HeadPromWindow headPromWindow = new HeadPromWindow();
            headPromWindow.Show();

            Close();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SegsFantWindow segsFantWindow = new SegsFantWindow();
            segsFantWindow.Show();

            Close();
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if (_versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }


    }
}