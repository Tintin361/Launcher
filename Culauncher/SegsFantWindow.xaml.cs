using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Culauncher
{
    /// <summary>
    /// Logique d'interaction pour SegsFantWindow.xaml
    /// </summary>
    public partial class SegsFantWindow : Window
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
                        PlaySegsButton.Content = "Jouer au Jeu";
                        break;
                    case LauncherStatus.failed:
                        PlaySegsButton.Content = "Échec de la MàJ";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlaySegsButton.Content = "Téléchargement du Jeu";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlaySegsButton.Content = "Téléchargement de la MàJ";
                        break;
                    default:
                        break;
                }
            }
        }


        public SegsFantWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version_SF.txt");
            gameZip = Path.Combine(rootPath, "SF.zip");
            gameExe = Path.Combine(rootPath, "SF", "Game.exe");
        }


        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                SFVersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("http://82.66.170.147/segs/Version_SF.txt"));


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
                    _onlineVersion = new Version(webClient.DownloadString("http://82.66.170.147/segs/Version_SF.txt"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("http://82.66.170.147/segs/SF.zip"), gameZip, _onlineVersion);

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

                SFVersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur de téléchargement: {ex} (C'est con ça !)");
            }
        }

        private void PlaySegsButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                /// Run with Admin
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "SF");
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed || Status == LauncherStatus.ready)
            {
                CheckForUpdates();
            }
        }


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                SFVersionText.Text = localVersion.ToString();
            }
        }


        private void MoreSegsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MoreBorderZone.IsVisible)
            {
                MoreBorderZone.Visibility = Visibility.Collapsed;
                MoreSegsButton.Content = "▼";
            }
            else
            {
                MoreBorderZone.Visibility = Visibility.Visible;
                MoreSegsButton.Content = "▲";
            }
        }

        private void UninstallSegsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Path.Combine(rootPath, "SF")))
            {
                MessageBoxButton buttons = MessageBoxButton.YesNo;
                DialogResult dialog = MessageBox.Show("Voulez-vous vraiment supprimer Segs Fantasy ? Toutes les données seront supprimées !", "Supprimer Segs Fantasy", (MessageBoxButtons)buttons);
                if (dialog == System.Windows.Forms.DialogResult.Yes)
                {
                    Directory.Delete(Path.Combine(rootPath, "SF"), true);
                    File.Delete(versionFile);
                    SFVersionText.Text = "0.0.0";
                }
                else
                {
                }
            }
            else
            {
                MessageBox.Show("Le jeu Segs Fantasy n'est pas installé sur votre ordinateur.", "Erreur");
            }
        }

        private void ShowSegsFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Path.Combine(rootPath, "SF")))
            {
                Process.Start("explorer.exe", Path.Combine(rootPath, "SF"));
            }
            else
            {
                MessageBox.Show("Le jeu Segs Fantasy n'est pas installé sur votre ordinateur.", "Erreur");
            }
        }

        private void StackPanel_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void StackPanel_PreviewMouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            HeadPromWindow headPromWindow = new HeadPromWindow();
            headPromWindow.Show();
            Close();
        }

        private void StackPanel_PreviewMouseDown_2(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("Culauncher Updater.exe");
            processStartInfo.WorkingDirectory = rootPath;
            Process.Start(processStartInfo);

            Close();
        }

        private void ExitMenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuButton.Visibility = Visibility.Visible;
            MenuBorderZone.Visibility = Visibility.Collapsed;
            ContentGrid.Opacity = 1;
            ContentGrid.IsEnabled = true;
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuButton.Visibility = Visibility.Collapsed;
            MenuBorderZone.Visibility = Visibility.Visible;
            ContentGrid.Opacity = 0.5;
            ContentGrid.IsEnabled = false;
        }
    }
}
