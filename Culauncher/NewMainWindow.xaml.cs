using Microsoft.Data.SqlClient;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Net.Http;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Path = System.IO.Path;
using Passwords;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Threading;

namespace Culauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        uninstallGame
    }

    public partial class NewMainWindow : Window, INotifyPropertyChanged
    {
        private int progressValue;
        public int ProgressValue
        {
            get { return progressValue; }
            set
            {
                if (progressValue != value)
                {
                    progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        private string progressText;
        public string ProgressText
        {
            get { return progressText; }
            set
            {
                if (progressText != value)
                {
                    progressText = value;
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private const string iconPath = "icons/Annabelle_icon.ico";

        // Get Current Game
        private string currentGame;

        // For Launcher
        private string launcherVersionFile;

        // For Games
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string shortName;
        private string gameExe;
        private string onlineVersionFileUrl;
        private string onlineZipUrl;
        private string patchnoteUrl;
        private string currentMode;

        // User Options
        private string startupGame;
        private bool keepLauncherOpen;
        private bool minimizeInTaskbar;
        private bool isPinned;

        // Current OS Vars
        private string currentOS;


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
                        PlayButton.Content = "Jouer au Jeu";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Échec de la MàJ";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Téléchargement du Jeu";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Téléchargement de la MàJ";
                        break;
                    case LauncherStatus.uninstallGame:
                        PlayButton.Content = "Déinstallation du Jeu";
                        break;
                    default:
                        break;
                }
            }
        }

        public NewMainWindow()
        {
            InitializeComponent();

            currentGame = Properties.Settings.Default.StartupGame;

            CheckForLauncherUpdates();
            rootPath = Directory.GetCurrentDirectory();
            launcherVersionFile = Path.Combine(rootPath, "Version_Launcher.txt");
            isPinned = false;

            currentMode = Properties.Settings.Default.AppTheme;

            UserSettings();
            SetCurrentGamesVariables();
            if (Properties.Settings.Default.IsDevelopper)
            {
                Dev_versions.Visibility = Visibility.Visible;
            }

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon(iconPath);
            ni.Visible = true;
                #pragma warning disable CS8622 // La nullabilité des types référence dans le type du paramètre ne correspond pas au délégué cible (probablement en raison des attributs de nullabilité).
            ni.Click += delegate (object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            ni.DoubleClick += delegate (object sender, EventArgs args)
            {
                Close();
            };
                #pragma warning restore CS8622 // La nullabilité des types référence dans le type du paramètre ne correspond pas au délégué cible (probablement en raison des attributs de nullabilité).

        }

        private void UserSettings()
        {
            startupGame = Properties.Settings.Default.StartupGame;
            keepLauncherOpen = Properties.Settings.Default.KeepLauncherOpen;
            minimizeInTaskbar = Properties.Settings.Default.MinimizeInTaskbar;
        }

        private void SetCurrentGamesVariables()
        {
            if (currentGame == "Headpat Problem")
            {
                versionFile = Path.Combine(rootPath, "Version_Headpat.txt");
                gameZip = Path.Combine(rootPath, "Headpat.zip");
                gameExe = Path.Combine(rootPath, "Headpat", "Game.exe");
                shortName = "Headpat";
                onlineVersionFileUrl = "http://culture-sympathique.fr/launcher/games/headpat-main/Version_Headpat.txt";
                onlineZipUrl = "http://culture-sympathique.fr/launcher/games/headpat-main/Headpat.zip";
                patchnoteUrl = "http://culture-sympathique.fr/launcher/games/headpat-main/Patchnote.txt";
            }
            else if (currentGame == "Segs Fantasy")
            {
                versionFile = Path.Combine(rootPath, "Version_SF.txt");
                gameZip = Path.Combine(rootPath, "SF.zip");
                gameExe = Path.Combine(rootPath, "SF", "Game.exe");
                shortName = "SF";
                onlineVersionFileUrl = "http://culture-sympathique.fr/launcher/games/sefa-main/Version_SF.txt";
                onlineZipUrl = "http://culture-sympathique.fr/launcher/games/sefa-main/SF.zip";
                patchnoteUrl = "http://culture-sympathique.fr/launcher/games/sefa-main/Patchnote.txt";
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            RefreshDataProfil();
            GetUserSettings();
            SetCurrentGamesVariables();
            CheckForLauncherUpdates();
            RefreshCurrentVersion();
            ChangeThemeButton();
            ChangeTheme();
        }

        private void GetUserSettings()
        {
            if (startupGame == "headpat")
            {
                HeadProbChangeLayout();
                currentGame = "Headpat Problem";
            }
            else if (startupGame == "sefa")
            {
                SefaChangeLayout();
                currentGame = "Segs Fantasy";
                SwapMarginAnimation(SFButton, HeadpatButton);
            }
        }

        private void RefreshCurrentVersion()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();
            }
            else
            {
                VersionText.Text = "0.0.0";
            }
        }


        /// Launcher Updates
        private void CheckForLauncherUpdates()
        {
            if (File.Exists(launcherVersionFile))
            {
                Version localVersion = new Version(File.ReadAllText(launcherVersionFile));

                try
                {
                    HttpClient client = new HttpClient();
                    using (HttpResponseMessage responseMessage = client.GetAsync("http://culture-sympathique.fr/launcher/app/Version_Launcher.txt").Result)
                    {
                        using (HttpContent content = responseMessage.Content)
                        {
                            Version onlineVersion = new Version(content.ReadAsStringAsync().Result);
                            if (onlineVersion.IsDifferentThan(localVersion))
                            {
                                CreateCustomDialog("Mise à Jour", "Une Mise à Jour du Launcher est disponible !!!", false, "/icons/update_icon.png");
                            }
                            else
                            {
                                Status = LauncherStatus.ready;

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    CreateCustomDialog("Erreur", $"Erreur de téléchargement de la MàJ du Launcher. {ex}", false, "/icons/info_icon.png");
                }
            }
        }


        private void GetPatchnote()
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                using (HttpResponseMessage responseMessage = httpClient.GetAsync(patchnoteUrl).Result)
                {
                    using (HttpContent content = responseMessage.Content)
                    {
                        CreateCustomDialog($"Patchnote de {currentGame}", content.ReadAsStringAsync().Result, false, "/icons/info_icon.png");
                    }
                }
            }
            catch (Exception ex)
            {
                CreateCustomDialog("Erreur", $"Impossible de récupérer le patchnote en ligne. {ex}", false, "/icons/info_icon.png");
            }
        }


        /// Games Updates
        private void CheckForUpdates()
        {
            if (File.Exists(Path.Combine(rootPath, gameZip)))
            {
                File.Delete(gameZip);
            }
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    HttpClient client = new HttpClient();
                    using (HttpResponseMessage responseMessage = client.GetAsync(onlineVersionFileUrl).Result)
                    {
                        using (HttpContent content = responseMessage.Content)
                        {
                            Version onlineVersion = new Version(content.ReadAsStringAsync().Result);
                            if (onlineVersion.IsDifferentThan(localVersion))
                            {
                                HideButtons(true);
                                IProgress<int> progress = new Progress<int>(value =>
                                {
                                    DownloadProgressBar.Value = value;
                                });
                                InstallGameFiles(true, onlineVersion, progress);
                            }
                            else
                            {
                                Status = LauncherStatus.ready;
                                HideButtons(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                    bool launchGame = CreateCustomDialog("Mode Hors Ligne ?", "Impossible de ce connecter pour vérifier les MàJ, voulez-vous lancer le jeu quand-même ?", true, "/icons/update_icon.png");
                    if (launchGame == true)
                    {
                        Status = LauncherStatus.ready;
                        LaunchGames();
                    }
                    else
                    {
                        Status = LauncherStatus.failed;
                        CreateCustomDialog("Erreur", $"Erreur de téléchargement de la Mise à Jour: {ex}", false, "/icons/attention_icon.png");

                        HideButtons(false);
                    }
                }
            }
            else
            {
                HideButtons(true);
                IProgress<int> progress = new Progress<int>(value =>
                {
                    DownloadProgressBar.Value = value;
                });
                InstallGameFiles(false, Version.zero, progress);
            }
        }

        // Install Game if User has not
        private async void InstallGameFiles(bool _isUpdate, Version _onlineVersion, IProgress<int> progress)
        {
            try
            {
                HttpClient client = new HttpClient();

                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    using (HttpResponseMessage responseMessage = await client.GetAsync(onlineVersionFileUrl))
                    {
                        using (HttpContent content = responseMessage.Content)
                        {
                            _onlineVersion = new Version(content.ReadAsStringAsync().Result);
                        }
                    }

                    ProgressBorder.Visibility = Visibility.Visible;

                    HttpResponseMessage zipResponse = await client.GetAsync(onlineZipUrl);
                    using (var stream = await client.GetStreamAsync(onlineZipUrl))
                    {
                        using (var fileStream = new FileStream($"{shortName}.zip", FileMode.CreateNew))
                        {
                            var buffer = new byte[65536];
                            int bytesRead;
                            long totalBytesRead = 0;
                            long? totalBytes = zipResponse.Content.Headers.ContentLength;

                            DateTime startTime = DateTime.Now;
                            long previousBytesRead = 0;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesRead += bytesRead;

                                var progressPercentage = (int)(totalBytesRead * 100 / totalBytes);

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ProgressValue = progressPercentage;
                                }, DispatcherPriority.Background);

                                progress.Report(progressPercentage);

                                // Calculer la vitesse de téléchargement
                                DateTime currentTime = DateTime.Now;
                                TimeSpan elapsedTime = currentTime - startTime;
                                long bytesSinceLastUpdate = totalBytesRead - previousBytesRead;
                                double downloadSpeed = bytesSinceLastUpdate / elapsedTime.TotalSeconds;

                                // Afficher les informations de téléchargement
                                string speedText = string.Format("{0} ko/s", (downloadSpeed / 1024).ToString("0.00"));
                                string downloadedText = string.Format("{0}", (totalBytesRead / (1024 * 1024)));
                                string totalSizeText = string.Format("{0} Mo", (totalBytes / (1024 * 1024)));
                                string timeRemainingText = string.Empty;

                                if (downloadSpeed > 0)
                                {
                                    double secondsRemaining = (double)((totalBytes - totalBytesRead) / downloadSpeed);
                                    TimeSpan timeRemaining = TimeSpan.FromSeconds(secondsRemaining);
                                    timeRemainingText = string.Format("{0:%h}h: {0:%m}m: {0:%s}s", timeRemaining);
                                }

                                string downloadInfo = string.Format("Téléchargé: {0}/{1} - Vitesse: {2} - Temps restant: {3}", downloadedText, totalSizeText, speedText, timeRemainingText);
                                textCurrent.Text = downloadInfo;
                            }
                        }
                    }
                    
                    DownloadGameCompleted(_onlineVersion);
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                HideButtons(false);
                ProgressBorder.Visibility = Visibility.Collapsed;
                CreateCustomDialog("Erreur", $"Erreur dans l'installation des fichiers du Jeu: {ex}", false, "/icons/info_icon.png");
            }
        }

        private void DownloadGameCompleted(Version _onlineVersion)
        {
            try
            {
                string onlineVersion = _onlineVersion.ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
                HideButtons(false);
                ProgressBorder.Visibility = Visibility.Collapsed;

                CreateCustomDialog("Installation terminée", $"Le Jeu a bien été installé sur votre ordinateur", false, "/icons/info_icon.png");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                HideButtons(false);
                ProgressBorder.Visibility = Visibility.Collapsed;
                CreateCustomDialog("Erreur", $"Erreur de téléchargement: {ex} (C'est con ça !)", false, "/icons/info_icon.png");
            }
        }


        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(PlayButton, 0.8);
            PlayButton.IsEnabled = false;
            CheckForUpdates();
            LaunchGames();
        }

        public void LaunchGames()
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                if (currentGame == "Headpat Problem")
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                    startInfo.WorkingDirectory = Path.Combine(rootPath, "Headpat");
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    Process.Start(startInfo);
                }
                else if (currentGame == "Segs Fantasy")
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                    startInfo.WorkingDirectory = Path.Combine(rootPath, "SF");
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    Process.Start(startInfo);
                }

                if (keepLauncherOpen == false)
                {
                    Close();
                }
            }
        }

        private void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(UninstallButton, 0.8);
            if (Directory.Exists(Path.Combine(rootPath, shortName)))
            {
                bool uninstall = CreateCustomDialog($"Supprimer {currentGame} ?",
                    $"Voulez-vous vraiment supprimer {currentGame} ? Toutes les données seront supprimées ! (Si vous souhaitez garder vos sauvegardes, elles sont dans le dossier save de votre Jeu.)", true,
                    "/icons/attention_icon.png");
                if (uninstall == true)
                {
                    Directory.Delete(Path.Combine(rootPath, shortName), true);
                    File.Delete(versionFile);
                    VersionText.Text = "0.0.0";

                    CreateCustomDialog("Jeu désinstallé", $"Le Jeu {currentGame} a bien été désinstallé de votre ordinateur.", false, "/icons/info_icon.png");
                }
            }
            else
            {
                CreateCustomDialog("Erreur", $"Le jeu {currentGame} n'est pas installé sur votre ordinateur.", false, "/icons/info_icon.png");
            }
        }

        private void ShowFolder_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(ShowFolder, 0.8);
            if (Directory.Exists(Path.Combine(rootPath, shortName)))
            {
                Process.Start("explorer.exe", Path.Combine(rootPath, shortName));
            }
            else
            {
                CreateCustomDialog("Erreur", $"Le jeu {currentGame} n'est pas installé sur votre ordinateur.", false, "/icons/info_icon.png");
            }
        }

        private void HeadpatButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentGame != "Headpat Problem")
            {
                BounceAnimation(HeadpatButton, 0.8);
                currentGame = "Headpat Problem";
                HeadProbChangeLayout();
                SetCurrentGamesVariables();
                RefreshCurrentVersion();
                SwapMarginAnimation(SFButton, HeadpatButton);
                this.Title = "Headpat Problem - CULauncher";
                this.Icon = BitmapFrame.Create(new Uri($"{Directory.GetCurrentDirectory()}/icons/Annabelle_icon.ico", UriKind.RelativeOrAbsolute));
            }
        }

        private void SFButton_Click(object sender, RoutedEventArgs e)
        {
            Random random = new();
            int number = random.Next(1, 50);
            if (number == 25)
            {
                SoundPlayer player = new SoundPlayer("Sounds/pouf.wav");
                player.Load();
                player.Play();
            }

            if (currentGame != "Segs Fantasy")
            {
                BounceAnimation(SFButton, 0.8);
                currentGame = "Segs Fantasy";
                SefaChangeLayout();
                SetCurrentGamesVariables();
                RefreshCurrentVersion();
                SwapMarginAnimation(SFButton, HeadpatButton);
                this.Title = "Segs Fantasy - CULauncher";
                this.Icon = BitmapFrame.Create(new Uri($"{Directory.GetCurrentDirectory()}/icons/Damien_icon.ico", UriKind.RelativeOrAbsolute));
            }
        }

        /// Menu Functions
        private void UpdateButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BounceAnimation(UpdateButton, 0.8);
            ProcessStartInfo processStartInfo = new ProcessStartInfo("Culauncher Updater.exe");
            processStartInfo.WorkingDirectory = rootPath;
            Process.Start(processStartInfo);

            Close();
        }

        private void HideButtons(bool setDisable)
        {
            if (setDisable)
            {
                MenuButton.IsEnabled = false;
                HeadpatButton.IsEnabled = false;
                SFButton.IsEnabled = false;
                PlayButton.IsEnabled = false;
                UninstallButton.IsEnabled = false;
                ShowFolder.IsEnabled = false;
                SaveWindow.IsEnabled = false;
                Dev_versions.IsEnabled = false;
                QuitApp.IsEnabled = false;
                GridSplitter.Opacity = 0.3;
            }
            else
            {
                MenuButton.IsEnabled = true;
                HeadpatButton.IsEnabled = true;
                SFButton.IsEnabled = true;
                PlayButton.IsEnabled = true;
                UninstallButton.IsEnabled = true;
                ShowFolder.IsEnabled = true;
                SaveWindow.IsEnabled = true;
                Dev_versions.IsEnabled = true;
                QuitApp.IsEnabled = true;
                GridSplitter.Opacity = 1;
            }
        }

        private void ShowElementWithFade(UIElement uIElement)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.2)
            };
            uIElement.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
        }

        private void HideElementWithFade(UIElement uIElement)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.2)
            };
            uIElement.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
        }

        private async void ChangeImageWithFade(string uriImage, Image img, bool visibility = false)
        {
            HideElementWithFade(img);
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            img.Source = new BitmapImage(new Uri(uriImage, UriKind.RelativeOrAbsolute));
            if (visibility)
            {
                img.Visibility = Visibility.Visible;
            }
            ShowElementWithFade(img);
            
        }

        private void HideMoreMenu()
        {
            ButtonAutomationPeer peer = new ButtonAutomationPeer(ExitMenuButton);
            IInvokeProvider invokeProvider = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
            invokeProvider.Invoke();
        }

        // Create a Custom Dialog function
        private bool CreateCustomDialog(string title, string messageContent, bool mode, string imgPath)
        {
            var customDialogs = new CustomDialogs(title, messageContent, mode, imgPath);
            customDialogs.ShowDialog();

            bool state = customDialogs.buttonClick;
            return state;
        }

        private async void HeadProbChangeLayout()
        {
            LogoImage.Visibility = Visibility.Collapsed;
            // More Menu Colors
            SolidColorBrush pinkColor = new SolidColorBrush(Color.FromRgb(255, 103, 219));

            PlayButton.Background = pinkColor;
            MoreButton.Background = pinkColor;
            UninstallButton.Background = pinkColor;
            ShowFolder.Background = pinkColor;
            ExitMoreMenu.Background = pinkColor;
            SaveWindow.Background = pinkColor;
            PatchnoteButton.Background = pinkColor;
            DownloadProgressBar.Foreground = pinkColor;
            

            // Main Grid
            LogoImage.Width = 400;
            LogoImage.Margin = new Thickness(0, 0, 0, 50);
            LogoImage.VerticalAlignment = VerticalAlignment.Center;
            ChangeTheme();
        }

        private void SefaChangeLayout()
        {
            LogoImage.Visibility = Visibility.Collapsed;

            // More Menu Colors
            SolidColorBrush purpleColor = new SolidColorBrush(Color.FromRgb(136, 45, 226));

            PlayButton.Background = purpleColor;
            MoreButton.Background = purpleColor;
            UninstallButton.Background = purpleColor;
            ShowFolder.Background = purpleColor;
            ExitMoreMenu.Background = purpleColor;
            SaveWindow.Background = purpleColor;
            PatchnoteButton.Background = purpleColor;
            DownloadProgressBar.Foreground = purpleColor;

            // Main Grid
            ChangeTheme();
            LogoImage.Width = 642;
            LogoImage.Margin = new Thickness(0, 0, 0, 10);
            LogoImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            LogoImage.VerticalAlignment = VerticalAlignment.Top;
            
        }

        private bool GetUserSystemTheme()
        {
            bool isDarkThemeEnabled = false;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    object value = key.GetValue("AppsUseLightTheme");

                    if (value != null && value is int themeValue)
                    {
                        isDarkThemeEnabled = themeValue == 0;
                    }
                }
            }
            return isDarkThemeEnabled;
        }

        private void ChangeTheme()
        {
            if (currentGame == "Segs Fantasy")
            {
                if (currentMode == "light")
                {
                    ChangeImageWithFade("images/Segs/White_BG.png", BackgroundImage);
                    ChangeImageWithFade("images/Segs/Segs_Launcher_Logo.png", LogoImage, true);
                }
                else if (currentMode == "dark")
                {
                    ChangeImageWithFade("images/Segs/Black_BG.png", BackgroundImage);
                    ChangeImageWithFade("images/Segs/Segs_Launcher_Logo.png", LogoImage, true);
                }
                else
                {
                    if (!GetUserSystemTheme())
                    {
                        ChangeImageWithFade("images/Segs/White_BG.png", BackgroundImage);
                    }
                    else
                    {
                        ChangeImageWithFade("images/Segs/Black_BG.png", BackgroundImage);
                    }
                    ChangeImageWithFade("images/Segs/Segs_Launcher_Logo.png", LogoImage, true);
                }
            }
            else if (currentGame == "Headpat Problem")
            {
                if (currentMode == "light")
                {
                    ChangeImageWithFade("images/Headpat/HP_Launcher_BG_blur.png", BackgroundImage);
                    ChangeImageWithFade("images/Headpat/HP_Launcher_Logo.png", LogoImage, true);
                }
                else if (currentMode == "dark")
                {
                    ChangeImageWithFade("images/Segs/Black_BG.png", BackgroundImage);
                    ChangeImageWithFade("images/Headpat/HP_Launcher_Logo.png", LogoImage, true);
                }
                else
                {
                    if (!GetUserSystemTheme())
                    {
                        ChangeImageWithFade("images/Headpat/HP_Launcher_BG_blur.png", BackgroundImage);
                    }
                    else
                    {
                        ChangeImageWithFade("images/Segs/Black_BG.png", BackgroundImage);
                    }
                    ChangeImageWithFade("images/Headpat/HP_Launcher_Logo.png", LogoImage, true);
                }
            }
        }

        private void QuitApp_Click(object sender, RoutedEventArgs e)
        {
            if (minimizeInTaskbar == true)
            {
                if (WindowState == WindowState.Normal)
                {
                    this.Hide();
                }
                else if (WindowState == WindowState.Maximized)
                {
                    this.Hide();
                }
                base.OnStateChanged(e);
            }
            else
            {
                Close();
            }
        }

        private void MaximizeApp_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeImage.Source = new BitmapImage(new Uri("icons/unmaximize_icon.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                MaximizeImage.Source = new BitmapImage(new Uri("icons/maxim_icon.png", UriKind.RelativeOrAbsolute));
            }
        }

        private void MinimizeApp_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuButton.Visibility = Visibility.Collapsed;
            GridSplitter.Opacity = 0.3;
            MenuBorderZone.Visibility = Visibility.Visible;
            ContentGrid.IsEnabled = false;
        }

        private void ExitMenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuButton.Visibility = Visibility.Visible;
            GridSplitter.Opacity = 1.0;
            ContentGrid.IsEnabled = true;
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            MoreBorderZone.Visibility = Visibility.Visible;
        }

        private void SettingsButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BounceAnimation(SettingsButton, 0.8);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();

            UserSettings();
        }

        private void Dev_versions_Click(object sender, RoutedEventArgs e)
        {
            InsiderMainWindow insiderMainWindow = new InsiderMainWindow();
            insiderMainWindow.Show();

            Close();
        }

        private void PatchnoteButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(PatchnoteButton, 0.8);
            GetPatchnote();
        }

        private void SaveWindow_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(SaveWindow, 0.8);
            SavesWindow savesWindow = new SavesWindow(rootPath, shortName, currentGame);
            savesWindow.ShowDialog();
        }

        private void PinApp_Click(object sender, RoutedEventArgs e)
        {
            if (isPinned)
            {
                this.Topmost = false;
                RotateAnimation(PinImage, -45.0, 0.0);
                Style style = Resources["DisableBorder"] as Style;
                this.Style = style;
                isPinned = false;
            }
            else
            {
                this.Topmost = true;
                RotateAnimation(PinImage, 0.0, -45.0);
                Style style = Resources["EnableBorder"] as Style;
                this.Style = style;
                isPinned = true;
            }
        }

        private void RotateAnimation(Image img, double from, double to, double time = 0.1)
        {
            DoubleAnimation anim = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(time)
            };
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            RotateTransform rotateTransform = new RotateTransform();
            img.RenderTransform = rotateTransform;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        public void RefreshDataProfil()
        {
            if (Properties.Settings.Default.AccountEmail != string.Empty)
            {
                if (VerifyOnlineProfil() == false)
                {
                    DisconnectProfil();
                }
                else
                {
                    if (Properties.Settings.Default.IsDevelopper)
                    {
                        Dev_versions.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Dev_versions.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private string GetPass()
        {
            PassManager passManager = new PassManager();
            return passManager.GetDecryptedPassword();
        }

        public bool VerifyOnlineProfil()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection($"Data Source=database.culture-sympathique.fr; Initial Catalog=Launcher_Database; User Id=ReadOnlyLogin; Password={GetPass()}; Encrypt=False;"))
                {
                    conn.Open();
                    string request = "SELECT * FROM Launcher_Database.dbo.Users WHERE UserId=@id AND Email=@email AND PasswordHash=@password;";
                    using (SqlCommand command = new SqlCommand(request, conn))
                    {
                        command.Parameters.AddWithValue("@id", Properties.Settings.Default.AccountId);
                        command.Parameters.AddWithValue("@email", Properties.Settings.Default.AccountEmail);
                        command.Parameters.AddWithValue("@password", Properties.Settings.Default.AccountPasswordHash);

                        using (SqlDataReader read = command.ExecuteReader())
                        {
                            if (!read.HasRows)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (SqlException ex)
            {
                CreateCustomDialog("Erreur serveur", $"Impossible d'accéder aux serveurs, vérifiez que vous êtes bien connecté(e) à Internet. Il se peut également que le serveur soit en maintenance.\n{ex}",
                    false, "/icons/info_icon.png");
                return false;
            }
        }

        public void DisconnectProfil()
        {
            Properties.Settings.Default.AccountId = 0;
            Properties.Settings.Default.AccountEmail = string.Empty;
            Properties.Settings.Default.AccountPasswordHash = string.Empty;
            Properties.Settings.Default.IsDevelopper = false;
            Properties.Settings.Default.Save();
        }

        public string getPass()
        {
            PassManager passManager = new PassManager();
            return passManager.GetDecryptedPassword();
        }

        private void AccountButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BounceAnimation(AccountButton, 0.8);
            if (Properties.Settings.Default.AccountEmail != "" && Properties.Settings.Default.AccountPasswordHash != "")
            {
                AccountWindow window = new AccountWindow();
                window.Show();
                return;
            }
            AccountRegister accountRegister = new AccountRegister();
            accountRegister.Show();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RotateAnimation(RefreshImage, 0.0, 360.0, 1.0);
            if (Properties.Settings.Default.AccountEmail == string.Empty)
            {
                CreateCustomDialog("Erreur", "Vous n'êtes pas connecté.", false, "/icons/info_icon.png");
                return;
            }
            RefreshDataProfil();
        }

        private void SwapMarginAnimation(Button button1, Button button2)
        {
            Thickness margin1 = button1.Margin;
            Thickness margin2 = button2.Margin;

            ThicknessAnimation anim1 = new ThicknessAnimation()
            {
                From = margin1,
                To = margin2,
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = false
            };

            ThicknessAnimation anim2 = new ThicknessAnimation()
            {
                From = margin2,
                To = margin1,
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = false
            };

            button1.BeginAnimation(Button.MarginProperty, anim1);
            button2.BeginAnimation(Button.MarginProperty, anim2);
        }

        private void NewTextMargin(TextBlock tblock, Thickness margin)
        {
            ThicknessAnimation anim = new ThicknessAnimation()
            {
                From = tblock.Margin,
                To = margin,
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = false
            };
            tblock.BeginAnimation(TextBlock.MarginProperty, anim);
        }

        private void ThemeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BounceAnimation(ThemeButton, 0.8);

            if (currentMode == "light")
            {
                IconChangeAnimation(ThemeImg, "icons/dark_mode_icon.png", 0.3);
                ThemeText.Text = "Thème: Sombre";
                NewTextMargin(ThemeText, new Thickness(0, 0, 38, 0));
                currentMode = "dark";
                Properties.Settings.Default.AppTheme = "dark";
                Properties.Settings.Default.Save();
            }
            else if (currentMode == "dark")
            {
                IconChangeAnimation(ThemeImg, "icons/saves_icon.png", 0.3);
                ThemeText.Text = "Thème: Auto";
                NewTextMargin(ThemeText, new Thickness(0, 0, 53, 0));
                currentMode = "auto";
                Properties.Settings.Default.AppTheme = "auto";
                Properties.Settings.Default.Save();

            }
            else if (currentMode == "auto")
            {
                IconChangeAnimation(ThemeImg, "icons/light_mode_icon.png", 0.3);
                ThemeText.Text = "Thème: Clair";
                NewTextMargin(ThemeText, new Thickness(0, 0, 55, 0));
                currentMode = "light";
                Properties.Settings.Default.AppTheme = "light";
                Properties.Settings.Default.Save();
            }

            ChangeTheme();
        }

        private void ChangeThemeButton()
        {
            if (currentMode == "dark")
            {
                IconChangeAnimation(ThemeImg, "icons/dark_mode_icon.png", 0.3);
                ThemeText.Text = "Thème: Sombre";
                NewTextMargin(ThemeText, new Thickness(0, 0, 38, 0));
                currentMode = "dark";
                Properties.Settings.Default.AppTheme = "dark";
                Properties.Settings.Default.Save();
            }
            else if (currentMode == "auto")
            {
                IconChangeAnimation(ThemeImg, "icons/saves_icon.png", 0.3);
                ThemeText.Text = "Thème: Auto";
                NewTextMargin(ThemeText, new Thickness(0, 0, 53, 0));
                currentMode = "auto";
                Properties.Settings.Default.AppTheme = "auto";
                Properties.Settings.Default.Save();

            }
            else if (currentMode == "light")
            {
                IconChangeAnimation(ThemeImg, "icons/light_mode_icon.png", 0.3);
                ThemeText.Text = "Thème: Clair";
                NewTextMargin(ThemeText, new Thickness(0, 0, 55, 0));
                currentMode = "light";
                Properties.Settings.Default.AppTheme = "light";
                Properties.Settings.Default.Save();
            }
        }

        private void IconChangeAnimation(Image img, string newIcon, double time)
        {
            DoubleAnimation anim = new DoubleAnimation()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(time)
            };
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            RotateTransform rotateTransform = new RotateTransform();
            img.Source = new BitmapImage(new Uri(newIcon, UriKind.RelativeOrAbsolute));
            img.RenderTransform = rotateTransform;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private void BounceAnimation(UIElement element, double endPos)
        {
            DoubleAnimation scaleXAnimation = new DoubleAnimation()
            {
                From = 1,
                To = endPos,
                Duration = TimeSpan.FromSeconds(0.1),
                AutoReverse = true
            };

            DoubleAnimation scaleYAnimation = new DoubleAnimation()
            {
                From = 1,
                To = endPos,
                Duration = TimeSpan.FromSeconds(0.1),
                AutoReverse = true
            };

            DoubleAnimation translateYAnimation = new DoubleAnimation()
            {
                From = 0,
                To = -10,
                Duration = TimeSpan.FromSeconds(0.1),
                AutoReverse = true
            };

            ScaleTransform scaleTransform = new ScaleTransform();

            TranslateTransform translateTransform = new TranslateTransform();

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = transformGroup;

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
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
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
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
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
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