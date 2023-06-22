using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Passwords;
using System.IO;

namespace Culauncher
{
    /// <summary>
    /// Logique d'interaction pour AccountRegister.xaml
    /// </summary>
    public partial class AccountRegister : Window
    {
        private bool isConnect;

        private int userId;
        private string userEmail;
        private string userPassword;
        private bool isDeveloper;


        public AccountRegister()
        {
            InitializeComponent();
            isConnect = false;

            userId = Properties.Settings.Default.AccountId;
            userEmail = Properties.Settings.Default.AccountEmail;
            userPassword = Properties.Settings.Default.AccountPasswordHash;
            isDeveloper = Properties.Settings.Default.IsDevelopper;
        }

        private void QuitAccount_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeAccount_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MoveElement(UIElement element, double destY, TimeSpan duration)
        {
            DoubleAnimation anime = new DoubleAnimation();
            double fromY = double.IsNaN(Canvas.GetTop(element)) ? 0 : Canvas.GetTop(element);
            anime.From = fromY;
            anime.To = destY;
            anime.Duration = duration;

            Storyboard.SetTarget(anime, element);
            Storyboard.SetTargetProperty(anime, new PropertyPath(Canvas.TopProperty));

            Storyboard board = new Storyboard();
            board.Children.Add(anime);
            board.Begin();
        }

        private void RegisterButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RegisterBorder.Background.Opacity = 0.4;
            LoginBorder.Background.Opacity = 0.6;
            MoveElement(AccountImage, 20, TimeSpan.FromSeconds(0.2));
            MoveElement(EmailTextbox, 140, TimeSpan.FromSeconds(0.2));
            MoveElement(PasswordTextbox, 200, TimeSpan.FromSeconds(0.2));
            PasswordConfirm.Visibility = Visibility.Visible;
            ConfirmButton.Content = "S'inscrire";
            ConfirmButton.Foreground = new SolidColorBrush(Colors.Black);
            ConfirmButton.FontWeight = FontWeights.Bold;
            isConnect = false;
        }

        private void LoginButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginBorder.Background.Opacity = 0.4;
            RegisterBorder.Background.Opacity = 0.6;
            PasswordConfirm.Visibility = Visibility.Collapsed;
            MoveElement(AccountImage, 30, TimeSpan.FromSeconds(0.2));
            MoveElement(EmailTextbox, 160, TimeSpan.FromSeconds(0.2));
            MoveElement(PasswordTextbox, 240, TimeSpan.FromSeconds(0.2));
            ConfirmButton.Content = "Se Connecter";
            ConfirmButton.Foreground = new SolidColorBrush(Colors.Black);
            ConfirmButton.FontWeight = FontWeights.Bold;
            isConnect = true;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(ConfirmButton, 0.8);
            if (isConnect)
            {
                if (ConnectFun())
                {
                    AccountWindow window = new AccountWindow();
                    window.Show();

                    Close();
                }
            }
            else
            {
                CreateCustomDialog("Erreur", "La création de compte n'est pas encore disponible.", false, "/icons/info_icon.png");
                //RegisterFun();
            }
        }

        private bool VerifyData()
        {
            string email_Pattern = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";

            string user_email = EmailTextbox.Text;
            string user_password = PasswordTextbox.Password;
            string user_passconf = PasswordConfirm.Password;

            if (!Regex.IsMatch(user_email, email_Pattern))
            {
                CreateCustomDialog("Erreur pour l'inscription", "L'adresse E-Mail que vous avez entrée n'est pas valide. Merci de fourni une adresse correcte.", false, "/icons/profil_icon.png");
                return false;
            }

            if (!(user_password.Length >= 8 && user_password.Any(char.IsUpper) && user_password.Any(char.IsLower) && user_password.Any(char.IsDigit)))
            {
                CreateCustomDialog("Erreur pour l'inscription", "Le Mot de Passe doit contenir au moins: huit caractères, un chiffre, une lettre minuscule et une lettre majuscule.", false, "/icons/profil_icon.png");
                return false;
            }

            if (user_password != user_passconf)
            {
                CreateCustomDialog("Erreur pour l'inscription", "Les deux Mot de Passe ne sont pas les mêmes, vérifiez que vous n'avez pas fait de faute de frappe.", false, "/icons/profil_icon.png");
                return false;
            }
            return true;
        }

        public string HashPassword(string password)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashedBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashedBytes.Length; i++)
                {
                    sb.Append(hashedBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private string GetPass()
        {
            PassManager passManager = new PassManager();
            return passManager.GetDecryptedPassword();
        }

        private void RegisterFun()
        {
            if (VerifyData())
            {
                string user_email = EmailTextbox.Text;
                string user_password = HashPassword(PasswordTextbox.Password);
                string connectionString = $"Data Source=database.culture-sympathique.fr; Initial Catalog=Launcher_Database; User Id=ReadOnlyLogin; Password={GetPass()}; Encrypt=False;";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string request = "INSERT INTO Users (Email, PasswordHash) VALUES (@email, @password)";

                    using (SqlCommand command = new SqlCommand(request, connection))
                    {
                        command.Parameters.AddWithValue("@email", user_email);
                        command.Parameters.AddWithValue("@Password", user_password);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private bool ConnectFun()
        {
            string user_email = EmailTextbox.Text;
            string user_password = HashPassword(PasswordTextbox.Password);
            string connectionString = $"Data Source=database.culture-sympathique.fr; Initial Catalog=Launcher_Database; User Id=ReadOnlyLogin; Password={GetPass()}; Encrypt=False;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string request = "SELECT * FROM Users WHERE Email=@email AND PasswordHash=@password;";
                    using (SqlCommand command = new SqlCommand(request, conn))
                    {
                        command.Parameters.AddWithValue("@email", user_email);
                        command.Parameters.AddWithValue("@password", user_password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    userId = (int)reader["UserId"];
                                    userEmail = (string)reader["Email"];
                                    userPassword = (string)reader["PasswordHash"];
                                    isDeveloper = (bool)reader["IsDev"];
                                }
                            }
                            else
                            {
                                CreateCustomDialog("Erreur de connexion", "Votre compte n'a pas été trouvé, vérifiez que vous ne vous êtes pas trompé(e) avec vos identifiants de connexion.", false, "/icons/attention_icon.png");
                                return false;
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                CreateCustomDialog("Erreur serveur", $"Impossible d'accéder aux serveurs, vérifiez que vous êtes bien connecté(e) à Internet. Il se peut également que le serveur soit en maintenance.\n{ex}", false, "/icons/attention_icon.png");
                return false;
            }

            Properties.Settings.Default.AccountId = userId;
            Properties.Settings.Default.AccountEmail = userEmail;
            Properties.Settings.Default.AccountPasswordHash = userPassword;
            Properties.Settings.Default.IsDevelopper = isDeveloper;
            Properties.Settings.Default.Save();
            return true;
        }

        private void DataButton_Click(object sender, RoutedEventArgs e)
        {
            BounceAnimation(DataButton, 0.8);
        }

        // Create a Custom Dialog function
        private bool CreateCustomDialog(string title, string messageContent, bool mode, string iconPath)
        {
            var customDialogs = new CustomDialogs(title, messageContent, mode, iconPath);
            customDialogs.ShowDialog();

            bool state = customDialogs.buttonClick;
            return state;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (isConnect)
                {
                    if (ConnectFun())
                    {
                        AccountWindow window = new AccountWindow();
                        window.Show();

                        Close();
                    }
                }
                else
                {
                    RegisterFun();
                }
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
