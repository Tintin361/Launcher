using Culauncher;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Passwords
{
    public class PassManager
    {
        public string GetDecryptedPassword()
        {
            string xmlPath = Directory.GetCurrentDirectory() + "/data/data.xml";

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlPath);
                XmlNode passNode = xmlDoc.SelectSingleNode("/configuration/credentials/password");

                if (passNode != null)
                {
                    string pass64 = passNode.InnerText;

                    byte[] bytes = Convert.FromBase64String(pass64);
                    string decriptPass = Encoding.UTF8.GetString(bytes);

                    return decriptPass;
                }
                else
                {
                    CreateCustomDialog("Fichier(s) non trouvé(s)", "Il manque certains fichier dans le dossier du Launcher, faites une réinstallation de l'application.", false, "/icons/attention_icon.png");
                    throw new Exception("Mot de passe non trouvé dans le fichier XML.");
                }
            }
            catch (Exception ex)
            {
                CreateCustomDialog("Fichier(s) non trouvé(s)", "Il manque certains fichier dans le dossier du Launcher, faites une réinstallation de l'application.", false, "/icons/attention_icon.png");
                Console.WriteLine(ex.Message);
                return String.Empty;
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
    }
}
