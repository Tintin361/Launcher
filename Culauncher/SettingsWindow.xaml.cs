using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Path = System.IO.Path;

namespace Culauncher
{
    public partial class SettingsWindow : Window
    {
        // For General Page
        private string startupGame;
        private bool keepLauncherOpen;
        private bool minimizeInTaskbar;

        // For Infos Page
        private string rootPath;
        private string launcherVersionFile;

        public SettingsWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            launcherVersionFile = Path.Combine(rootPath, "Version_Culauncher.txt");
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            RefreshLauncherCurrentVersion();
            RestoreSettings();
        }

        private void QuitSettings_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeSettings_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void RefreshLauncherCurrentVersion()
        {
            if (File.Exists(Path.Combine(rootPath, "Version_Culauncher.txt")))
            {
                Version currentVersion = new Version(File.ReadAllText(Path.Combine(rootPath, "Version_Culauncher.txt")));
                String ver = currentVersion.ToString();
                VersionTextOptions.Text = $"Version {ver}";
            }
        }


        private void RestoreSettings()
        {
            startupGame = Properties.Settings.Default.StartupGame;
            keepLauncherOpen = Properties.Settings.Default.KeepLauncherOpen;
            minimizeInTaskbar = Properties.Settings.Default.MinimizeInTaskbar;

            if (startupGame == "headpat")
            {
                HeadStart.IsChecked = true;
            }
            else if (startupGame == "sefa")
            {
                SefaStart.IsChecked = true;
            }

            if (keepLauncherOpen == true)
            {
                KeepLauncher.IsChecked = true;
            }

            if (minimizeInTaskbar == true)
            {
                MinimizeLauncher.IsChecked = true;
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.StartupGame = startupGame;
            Properties.Settings.Default.KeepLauncherOpen = keepLauncherOpen;
            Properties.Settings.Default.MinimizeInTaskbar = minimizeInTaskbar;

            Properties.Settings.Default.Save();
        }

        private void GeneralOptions_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BounceAnimation(GeneralOptions, 0.8);
            VersionSettings.Visibility = Visibility.Collapsed;
            GeneralSettings.Visibility = Visibility.Visible;
        }

        private void InfosOptions_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BounceAnimation(InfosOptions, 0.8);
            VersionSettings.Visibility = Visibility.Visible;
            GeneralSettings.Visibility = Visibility.Collapsed;
        }

        private void HeadStart_Click(object sender, RoutedEventArgs e)
        {
            startupGame = "headpat";
            SaveSettings();
            if (SefaStart.IsChecked == true)
            {
                SefaStart.IsChecked = false;
            }
            SetDefaultChoice();
        }

        private void SefaStart_Click(object sender, RoutedEventArgs e)
        {
            startupGame = "sefa";
            SaveSettings();
            if (HeadStart.IsChecked == true)
            {
                HeadStart.IsChecked = false;
            }
            SetDefaultChoice();
        }

        private void KeepLauncher_Click(object sender, RoutedEventArgs e)
        {
            if (KeepLauncher.IsChecked == true)
            {
                keepLauncherOpen = true;
            }
            else
            {
                keepLauncherOpen = false;
            }
            SaveSettings();
        }

        private void SetDefaultChoice()
        {
            if (HeadStart.IsChecked == false && SefaStart.IsChecked == false)
            {
                HeadStart.IsChecked = true;
            }
        }

        private void MinimizeLauncher_Click(object sender, RoutedEventArgs e)
        {
            if (MinimizeLauncher.IsChecked == true)
            {
                minimizeInTaskbar = true;
            }
            else
            {
                minimizeInTaskbar = false;
            }
            SaveSettings();
        }

        private void TextBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            KeepLauncher.IsChecked = !KeepLauncher.IsChecked;
            KeepLauncher_Click(sender, e);
        }

        private void TextBlock_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            MinimizeLauncher.IsChecked = !KeepLauncher.IsChecked;
            MinimizeLauncher_Click(sender, e);
        }

        private void TextBlock_PreviewMouseLeftButtonDown_2(object sender, MouseButtonEventArgs e)
        {
            HeadStart.IsChecked = !HeadStart.IsChecked;
            HeadStart_Click(sender, e);
        }

        private void TextBlock_PreviewMouseLeftButtonDown_3(object sender, MouseButtonEventArgs e)
        {
            SefaStart.IsChecked = !SefaStart.IsChecked;
            SefaStart_Click(sender, e);
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
