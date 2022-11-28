using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace Culauncher
{
    /// <summary>
    /// Logique d'interaction pour HeadPromWindow.xaml
    /// </summary>
    public partial class HeadPromWindow : Window
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
                        PlayHeadpatButton.Content = "Jouer au Jeu";
                        break;
                    case LauncherStatus.failed:
                        PlayHeadpatButton.Content = "Échec de la MàJ";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayHeadpatButton.Content = "Téléchargement de la MàJ";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayHeadpatButton.Content = "Téléchargement du Jeu";
                        break;
                    default:
                        break;
                }
            }
        }


        public HeadPromWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version_Headpat.txt");
            gameZip = Path.Combine(rootPath, "Headpat.zip");
            gameExe = Path.Combine(rootPath, "Headpat", "Game.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                HeadpatVersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("http://82.66.170.147/headpat/Version_Headpat.txt"));


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
                    _onlineVersion = new Version(webClient.DownloadString("http://82.66.170.147/headpat/Version_Headpat.txt"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("http://82.66.170.147/headpat/Headpat.zip"), gameZip, _onlineVersion);

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

                HeadpatVersionText.Text = onlineVersion;
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
                HeadpatVersionText.Text = localVersion.ToString();
            }
        }

        private void PlayHeadpatButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {

                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Headpat");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed || Status == LauncherStatus.ready)
            {
                CheckForUpdates();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            Close();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SegsFantWindow segsFantWindow = new SegsFantWindow();
            segsFantWindow.Show();

            Close();
        }

        private void MoreHeadpatButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"More button");
        }
    }
}
