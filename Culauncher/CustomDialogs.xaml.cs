using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Culauncher
{
    /// <summary>
    /// Logique d'interaction pour CustomDialogs.xaml
    /// </summary>
    public partial class CustomDialogs : Window
    {
        // Variables Content
        public bool buttonClick;

        private void QuitDialog_Click(object sender, RoutedEventArgs e)
        {
            buttonClick = false;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClick = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClick = false;
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClick = false;
            Close();
        }

        public CustomDialogs(string titleText, string messageText, bool dialogYesNo, string imagePath)
        {
            InitializeComponent();

            TitleTextBlock.Text = titleText;
            ContentText.Text = messageText;
            this.Title = titleText;
            iconImage.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));

            if (dialogYesNo == true)
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
            }
            else if (dialogYesNo == false)
            {
                OKButton.Visibility = Visibility.Visible;
            }
        }
    }
}
