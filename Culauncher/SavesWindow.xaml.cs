using System;
using System.IO.Compression;
using System.IO;
using System.Windows;
using Path = System.IO.Path;
using System.Windows.Forms;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Text;
using Azure;
using System.Globalization;

namespace Culauncher
{
    public partial class SavesWindow : Window, INotifyPropertyChanged
    {
        private string rootPath;
        private string shortName;
        private string currentGame;
        private DateTime? localDateTime;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Exemple de propriété avec gestion de la notification de modification
        private string _onlineText;
        public string OnlineText
        {
            get { return _onlineText; }
            set
            {
                if (_onlineText != value)
                {
                    _onlineText = value;
                    OnPropertyChanged(nameof(OnlineText));
                }
            }
        }


        public SavesWindow(string rootPath, string shortName, string currentGame)
        {
            InitializeComponent();
            this.rootPath = rootPath;
            this.shortName = shortName;
            this.currentGame = currentGame;
            localDateTime = GetLastModifDate($"{Directory.GetCurrentDirectory()}/{shortName}/save/global.rmmzsave");

            WindowName.Text = $"Gestion des Sauvegardes pour {currentGame}";
            this.Title = $"Gestion des Sauvegardes pour {currentGame}";
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.EnableOnlineSaves)
            {
                EnableUpdates.IsChecked = true;
            }

            if (localDateTime.HasValue)
            {
                LocalText.Text = localDateTime.Value.ToString();
                LocalImage.Source = new BitmapImage(new Uri("icons/folder_icon.png", UriKind.RelativeOrAbsolute));
            }

            try
            {
                string onlineDateTime = await GetOnlineDate($"http://culture-sympathique.fr/launcher_data/{Properties.Settings.Default.AccountId}/{shortName.ToLower()}/saves.zip");
                if (!string.IsNullOrEmpty(onlineDateTime))
                {
                    Dispatcher.Invoke(() =>
                    {
                        string time = onlineDateTime;
                        DateTime frenchDate = DateTime.Parse(time);


                        OnlineTextBlock.Text = frenchDate.ToString("dd/MM/yyyy HH:mm:ss", new CultureInfo("fr-FR"));
                        OnlineImage.Source = new BitmapImage(new Uri("icons/folder_icon.png", UriKind.RelativeOrAbsolute));
                    });
                }
                else
                {
                    CreateCustomDialog("Erreur de connexion", "Impossible de récupérer les informations du serveur, êtes-vous connecté à internet ?", false, "icons/info_icon.png");
                }
            }
            catch
            {
                CreateCustomDialog("Erreur de connexion", "Impossible de récupérer les informations du serveur, êtes-vous connecté à internet ?", false, "icons/info_icon.png");
            }
        }

        private async Task<string> GetOnlineDate(string url)
        {
            string onlineDateTime = string.Empty;
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage responseMessage = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                if (responseMessage.IsSuccessStatusCode)
                {
                    if (responseMessage.Content.Headers.TryGetValues("Last-Modified", out var lastModifiedValues))
                    {
                        onlineDateTime = lastModifiedValues.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                CreateCustomDialog("Erreur de connexion", "Impossible de récupérer les informations du serveur, êtes-vous connecté à internet ?", false, "icons/info_icon.png");
            }
            return onlineDateTime;
        }

        private void QuitApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeApp_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(ExportButton, 0.8);
            if (Directory.Exists(Path.Combine(rootPath, shortName)))
            {
                if (!Directory.Exists(Path.Combine(rootPath, shortName, "save")))
                {
                    CreateCustomDialog("Erreur", $"Le jeu {currentGame} ne possède aucune sauvegarde.", false, "/icons/info_icon.png");
                }
                else if (Directory.GetFiles(Path.Combine(rootPath, shortName, "save")).Length != 0)
                {
                    CreateCustomDialog("Erreur", $"Le jeu {currentGame} ne possède aucune sauvegarde.", false, "/icons/info_icon.png");
                }
                else
                {
                    bool result = CreateCustomDialog("Exporter les sauvegardes", "Cette action vous permet de récupérer vos sauvegardes dans un fichier Zip, elle n'effacera pas les sauvegardes existantes.",
                        true, "/icons/info_icon.png");
                    if (result)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Title = "Exporter les Sauvegardes";
                        saveFileDialog.Filter = "Fichier ZIP|*.zip";
                        saveFileDialog.DefaultExt = ".zip";
                        saveFileDialog.FileName = $"Sauvegardes_{shortName}";
                        DialogResult dialogResult = saveFileDialog.ShowDialog();

                        if (dialogResult == System.Windows.Forms.DialogResult.OK)
                        {
                            string savePath = Path.GetDirectoryName(saveFileDialog.FileName);
                            string _path = Path.Combine(rootPath, shortName, "save");

                            if (File.Exists(Path.Combine(savePath, saveFileDialog.FileName)) == true)
                            {
                                File.Delete(Path.Combine(savePath, saveFileDialog.FileName));
                            }
                            ZipFile.CreateFromDirectory(_path, Path.Combine(savePath, saveFileDialog.FileName), CompressionLevel.Fastest, true);
                        }
                    }
                }
            }
            else
            {
                CreateCustomDialog("Erreur", $"Le jeu {currentGame} n'est pas installé sur votre ordinateur.", false, "/icons/info_icon.png");
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(ImportButton, 0.8);
            if (Directory.Exists(Path.Combine(rootPath, shortName)))
            {
                bool weContinue = false;
                if (Directory.Exists(Path.Combine(rootPath, shortName, "save")) == true)
                {
                    weContinue = OnSupprimeTout();
                }
                else
                {
                    weContinue = true;
                }

                if (weContinue)
                {
                    CreateCustomDialog("Informations", "Si vous avez des sauvegardes en .rmmzsave, vous pouvez les mettre dans un dossier save puis créer un fichier zip de ce dossier et l'importer avec le Launcher.\n(Les miniscules sont importantes.)",
                        false, "/icons/info_icon.png");

                    Microsoft.Win32.OpenFileDialog openFile = new Microsoft.Win32.OpenFileDialog();
                    openFile.Filter = "Fichier ZIP|*.zip";
                    openFile.DefaultExt = ".zip";
                    openFile.Multiselect = false;

                    if (openFile.ShowDialog() == true)
                    {
                        try
                        {
                            File.Copy(openFile.FileName, Path.Combine(rootPath, "data.zip"), true);

                            var archiveZip = Path.Combine(rootPath, "data.zip");
                            ZipFile.ExtractToDirectory(archiveZip, Path.Combine(rootPath, shortName));
                            File.Delete(archiveZip);
                        }
                        catch (Exception ex)
                        {
                            CreateCustomDialog("Erreur", $"Impossible de remplacer les sauvegardes {ex}", false, "/icons/info_icon.png");
                        }
                    }
                }
            }
            else
            {
                CreateCustomDialog("Erreur", $"Le jeu {currentGame} n'est pas installé sur votre ordinateur.", false, "icons/info_icon.png");
            }
        }

        private bool OnSupprimeTout()
        {
            bool result = CreateCustomDialog("Attention !",
                $"Il existe déjà des sauvegardes existantes pour {currentGame}, cette action va les effacer.\nVoulez-vous vraiment continuer (Les fichiers seront supprimés en cliquant sur OUI) ?", true,
                "/icons/attention_icon.png");

            if (result)
            {
                Directory.Delete(Path.Combine(rootPath, shortName, "save"), true);
                return true;
            }
            return false;
        }

        // Create a Custom Dialog function
        private bool CreateCustomDialog(string title, string messageContent, bool mode, string imgPath)
        {
            var customDialogs = new CustomDialogs(title, messageContent, mode, imgPath);
            customDialogs.ShowDialog();

            bool state = customDialogs.buttonClick;
            return state;
        }

        private DateTime? GetLastModifDate(string path)
        {
            DateTime? lastModified = null;
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                lastModified = fileInfo.LastAccessTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return lastModified;
        }

        private void TextBlock_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EnableUpdates.IsChecked = !EnableUpdates.IsChecked;
            EnableSaves();
        }

        private void EnableUpdates_Click(object sender, RoutedEventArgs e)
        {
            EnableSaves();
        }

        private void EnableSaves()
        {
            if (Properties.Settings.Default.AccountEmail != String.Empty && Properties.Settings.Default.AccountPasswordHash != String.Empty && Properties.Settings.Default.AccountId != 0)
            {
                bool? isChecked = EnableUpdates.IsChecked;
                if (isChecked.HasValue)
                {
                    Properties.Settings.Default.EnableOnlineSaves = isChecked.Value;
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                CreateCustomDialog("Erreur de Compte", "Vous n'êtes pas connecté à un compte.", false, "icons/info_icon.png");
                EnableUpdates.IsChecked = false;
            }
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
}
