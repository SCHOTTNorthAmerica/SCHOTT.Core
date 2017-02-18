using SCHOTT.Core.Extensions;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SCHOTT.Core.Settings
{
    /// <summary>
    /// A class to persist custom settings in the application directory.
    /// </summary>
    public static class SettingsFunctions
    {
        /// <summary>
        /// Initializes the settings folder.
        /// </summary>
        public static void InitializeSettings()
        {
            // make sure settings folder exists
            if (!Directory.Exists(Path.GetDirectoryName(Application.ExecutablePath) + "\\Settings"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\Settings");
            }
        }

        /// <summary>
        /// Write the settings object out to a given filename.
        /// </summary>
        /// <typeparam name="T">The object type to serialize.</typeparam>
        /// <param name="serializeableObject">The object to serialize.</param>
        /// <param name="fileName">The file name to write too.</param>
        public static void WriteSettings<T>(this T serializeableObject, string fileName = "ApplicationSettings")
        {
            InitializeSettings();

            var settingsType = serializeableObject.GetType();
            var serializer = XmlSerializer.FromTypes(new[] { settingsType })[0];

            using (var stream = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + $"\\Settings\\{fileName}.xml", FileMode.Create))
            {
                serializer.Serialize(stream, serializeableObject);
            }
        }

        /// <summary>
        /// Read the settings object out of the given filename.
        /// </summary>
        /// <typeparam name="T">The object type to serialize.</typeparam>
        /// <param name="serializeableObject">The object to serialize.</param>
        /// <param name="fileName">The file name to read from.</param>
        public static void ReadSettings<T>(this T serializeableObject, string fileName = "ApplicationSettings")
        {
            InitializeSettings();

            var settingsType = serializeableObject.GetType();
            var serializer = XmlSerializer.FromTypes(new[] { settingsType })[0];

            var fullFilePath = Path.GetDirectoryName(Application.ExecutablePath) + $"\\Settings\\{fileName}.xml";

            if (!File.Exists(fullFilePath))
                return;

            using (var stream = new FileStream(fullFilePath, FileMode.Open))
            {
                var settings = (T)serializer.Deserialize(stream);
                serializeableObject.CopyFrom(settings);
            }
        }
        
    }
}
