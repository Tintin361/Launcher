using Microsoft.Data.SqlClient;
using Passwords;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Culauncher
{
    /// <summary>
    /// Logique d'interaction pour AccountWindow.xaml
    /// </summary>
    public partial class AccountWindow : Window
    {
        public void Disconnect()
        {
            bool restart = CreateCustomDialog("Redémarrage", "L'application va redémarrer, êtes-vous sûr de vouloir vous déconnecter ?", true, "/icons/attention_icon.png");

            if (restart)
            {
                NewMainWindow window = Application.Current.Windows.OfType<NewMainWindow>().First();
                window.DisconnectProfil();

                ProcessStartInfo processStartInfo = new ProcessStartInfo("Culauncher.exe");
                processStartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                Process.Start(processStartInfo);

                Application.Current.Shutdown();
            }
        }

        private string _accountEmail;

        public AccountWindow()
        {
            InitializeComponent();
            _accountEmail = Properties.Settings.Default.AccountEmail;
            Download_Avatar();
        }

        private void QuitAccount_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeAccount_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(DisconnectButton, 0.8);
            Disconnect();
        }

        private void Window_ContentRendered(object sender, System.EventArgs e)
        {
            UsernameText.Text = _accountEmail;
        }

        private string GetPass()
        {
            PassManager passManager = new PassManager();
            return passManager.GetDecryptedPassword();
        }

        private async void Download_Avatar()
        {
            // Get the Avatar URL from the database
            string url = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection($"Data Source=database.culture-sympathique.fr; Initial Catalog=master; User Id=Launcher_Create; Password={GetPass()}; Encrypt=False;"))
                {
                    conn.Open();
                    string request = "SELECT ProfilIcon FROM Users WHERE UserId=@id AND Email=@email AND PasswordHash=@password;";
                    using (SqlCommand command = new SqlCommand(request, conn))
                    {
                        command.Parameters.AddWithValue("@id", Properties.Settings.Default.AccountId);
                        command.Parameters.AddWithValue("@email", Properties.Settings.Default.AccountEmail);
                        command.Parameters.AddWithValue("@password", Properties.Settings.Default.AccountPasswordHash);

                        using (SqlDataReader read = command.ExecuteReader())
                        {
                            if (!read.HasRows)
                            {
                                CreateCustomDialog("Erreur de données", "Votre compte n'a pas été trouvé sur nos serveur ? Êtes-vous bien inscrit ?", false, "/icons/profil_icon.png");
                                return;
                            }
                            while (read.Read())
                            {
                                url = (string)read["ProfilIcon"];
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                CreateCustomDialog("Erreur serveur", $"Impossible d'accéder aux serveurs, vérifiez que vous êtes bien connecté(e) à Internet. Il se peut également que le serveur soit en maintenance.\n\n{ex}", false, "/icons/attention_icon.png");
                return;
            }

            // Next, download the image in the app's files
            string imgPath = Path.Combine(Directory.GetCurrentDirectory(), "data/avatar.jpg");
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage message = await client.GetAsync(url);

                    if (message.IsSuccessStatusCode)
                    {
                        using (Stream stream = await message.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = new FileStream(imgPath, FileMode.Create))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        CreateCustomDialog("Erreur", "Impossible de télécharger votre avatar.", false, "/icons/profil_icon.png");
                    }
                }
                catch (Exception ex)
                {
                    CreateCustomDialog("Erreur", $"Impossible de télécharger votre avatar.\n\n{ex}", false, "/icons/profil_icon.png");
                }
            }

            // And now, display the downloaded image in an Image Object
            BitmapImage bitmapImage = new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute));
            ImageBrush brush = AccountImage.Fill as ImageBrush;
            brush.ImageSource = bitmapImage;

            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
        }

        // Create a Custom Dialog function, see CustomDialogs.xaml
        private bool CreateCustomDialog(string title, string messageContent, bool mode, string imgPath)
        {
            var customDialogs = new CustomDialogs(title, messageContent, mode, imgPath);
            customDialogs.ShowDialog();

            bool state = customDialogs.buttonClick;
            return state;
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
