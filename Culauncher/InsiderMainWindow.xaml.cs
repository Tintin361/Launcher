using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using Path = System.IO.Path;

namespace Culauncher
{
    /// <summary>
    /// Logique d'interaction pour InsiderMainWindow.xaml
    /// </summary>
    public partial class InsiderMainWindow : Window
    {
        // Get Current Game
        private string currentGame;
        private bool isUpdate;

        // For Games
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string shortName;
        private string gameExe;
        private string onlineVersionFileUrl;
        private string onlineZipUrl;


        public InsiderMainWindow()
        {
            InitializeComponent();

            rootPath = Path.Combine(Directory.GetCurrentDirectory(), "developpers");
            isUpdate = false;
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory("developpers");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            SetCurrentGamesVariables();
        }

        private void SetCurrentGamesVariables()
        {
            if (currentGame == "Jimmy et la kète du 11 Septembre")
            {
                versionFile = Path.Combine(rootPath, "Version_Jimmy.txt");
                gameZip = Path.Combine(Directory.GetCurrentDirectory(), "Jimmy.zip");
                gameExe = Path.Combine(rootPath, "Jimmy", "Game.exe");
                shortName = "Jimmy";
                onlineVersionFileUrl = "http://culture-sympathique.fr/launcher/games-dev/jimmy-dev/Version_Jimmy.txt";
                onlineZipUrl = "http://culture-sympathique.fr/launcher/games-dev/jimmy-dev/Jimmy.zip";
            }
            else if (currentGame == "Headpat Problem")
            {
                versionFile = Path.Combine(rootPath, "Version_Headpat.txt");
                gameZip = Path.Combine(Directory.GetCurrentDirectory(), "Headpat.zip");
                gameExe = Path.Combine(rootPath, "Headpat", "Game.exe");
                shortName = "Headpat";
                onlineVersionFileUrl = "http://culture-sympathique.fr/launcher/games-dev/headpat-dev/Version_Headpat.txt";
                onlineZipUrl = "http://culture-sympathique.fr/launcher/games-dev/headpat-dev/Headpat.zip";
            }
            else if (currentGame == "Segs Fantasy")
            {
                versionFile = Path.Combine(rootPath, "Version_SF.txt");
                gameZip = Path.Combine(Directory.GetCurrentDirectory(), "SF.zip");
                gameExe = Path.Combine(rootPath, "SF", "Game.exe");
                shortName = "SF";
                onlineVersionFileUrl = "http://culture-sympathique.fr/launcher/games-dev/sefa-dev/Version_SF.txt";
                onlineZipUrl = "http://culture-sympathique.fr/launcher/games-dev/sefa-dev/SF.zip";
            }
        }


        /// Games Updates
        private void CheckForUpdates()
        {
            if (!isUpdate)
            {
                if (File.Exists(Path.Combine(rootPath, gameZip)))
                {
                    File.Delete(gameZip);
                }
                if (File.Exists(versionFile))
                {
                    Version localVersion = new Version(File.ReadAllText(versionFile));

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
                                    isUpdate = true;
                                    InstallGameFiles(true, onlineVersion);
                                }
                                else
                                {
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CreateCustomDialog("Erreur", $"Erreur de téléchargement de la Mise à Jour: {ex}", false, "/icons/attention_icon.png");
                    }
                }
                else
                {
                    isUpdate = true;
                    InstallGameFiles(false, Version.zero);
                }
            }
        }


        // Install Game if User has not
        private async void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                HttpClient client = new HttpClient();
                using (HttpResponseMessage responseMessage = client.GetAsync(onlineVersionFileUrl).Result)
                {
                    using (HttpContent content = responseMessage.Content)
                    {
                        if (!_isUpdate)
                        {
                            _onlineVersion = new Version(content.ReadAsStringAsync().Result);
                        }
                        using (var stream = await client.GetStreamAsync(onlineZipUrl))
                        {

                            using (var fileStream = new FileStream($"{shortName}.zip", FileMode.CreateNew))
                            {
                                await stream.CopyToAsync(fileStream);
                            }
                        }
                    }
                }
                DownloadGameCompleted(_onlineVersion);
            }
            catch (Exception ex)
            {
                isUpdate = false;
                CreateCustomDialog("Erreur", $"Erreur dans l'installation des fichiers du Jeu: {ex}", false, "/icons/attention_icon.png");
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
                isUpdate = false;

                CreateCustomDialog("Installation terminée", "Le Jeu a bien été installé sur votre ordinateur", false, "/icons/info_icon.png");
            }
            catch (Exception ex)
            {
                isUpdate = false;
                CreateCustomDialog("Erreur", $"Erreur de téléchargement: {ex}", false, "/icons/info_icon.png");
            }
        }


        private void PlayGame()
        {
            CheckForUpdates();
            if (File.Exists(gameExe))
            {
                if (currentGame == "Jimmy et la kète du 11 Septembre")
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                    startInfo.WorkingDirectory = Path.Combine(rootPath, "Jimmy");
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    Process.Start(startInfo);
                }

                else if (currentGame == "Headpat Problem")
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
            }
        }

        private void UninstallButton()
        {
            if (Directory.Exists(Path.Combine(rootPath, shortName)))
            {
                bool uninstall = CreateCustomDialog($"Supprimer {currentGame} ?",
                    $"Voulez-vous vraiment supprimer {currentGame} ? Toutes les données seront supprimées ! (Si vous souhaitez garder vos sauvegardes, elles sont dans le dossier save de votre Jeu.)", true,
                    "/icons/attention_icon.png");
                if (uninstall == true)
                {
                    Directory.Delete(Path.Combine(rootPath, shortName), true);
                    File.Delete(versionFile);

                    CreateCustomDialog("Jeu désinstallé", $"Le Jeu {currentGame} à bien été désinstallé de votre ordinateur.", false, "/icons/info_icon.png");
                }
            }
            else
            {
                CreateCustomDialog("Erreur", $"Le jeu {currentGame} n'est pas installé sur votre ordinateur.", false, "/icons/info_icon.png");
            }
        }

        private void ShowFolder()
        {
            if (Directory.Exists(Path.Combine(rootPath, shortName)))
            {
                Process.Start("explorer.exe", Path.Combine(rootPath, shortName));
            }
            else
            {
                CreateCustomDialog("Erreur", $"Le jeu {currentGame} n'est pas installé sur votre ordinateur.", false, "/icons/info_icon.png");
            }
        }



        // Create a Custom Dialog function
        private bool CreateCustomDialog(string title, string messageContent, bool mode, string imgPath)
        {
            var customDialogs = new CustomDialogs(title, messageContent, mode, imgPath);
            customDialogs.ShowDialog();

            bool state = customDialogs.buttonClick;
            return state;
        }


        private void PlayJimmyButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Jimmy et la kète du 11 Septembre";
            SetCurrentGamesVariables();
            PlayGame();
        }

        private void UnistallJimmyButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Jimmy et la kète du 11 Septembre";
            SetCurrentGamesVariables();
            UninstallButton();
        }

        private void OpenJimmyButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Jimmy et la kète du 11 Septembre";
            SetCurrentGamesVariables();
            ShowFolder();
        }

        private void PlaySegsButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Segs Fantasy";
            SetCurrentGamesVariables();
            PlayGame();
        }

        private void UnistallSegsButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Segs Fantasy";
            SetCurrentGamesVariables();
            UninstallButton();
        }

        private void OpenSegsButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Segs Fantasy";
            SetCurrentGamesVariables();
            ShowFolder();
        }

        private void PlayHeadButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Headpat Problem";
            SetCurrentGamesVariables();
            PlayGame();
        }

        private void UnistallHeadButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Headpat Problem";
            SetCurrentGamesVariables();
            UninstallButton();
        }

        private void OpenHeadButton_Click(object sender, RoutedEventArgs e)
        {
            currentGame = "Headpat Problem";
            SetCurrentGamesVariables();
            ShowFolder();
        }

        private void QuitApp_Click(object sender, RoutedEventArgs e)
        {
            NewMainWindow newMainWindow = new NewMainWindow();
            newMainWindow.Show();

            Close();
        }

        private void MinimizeApp_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
