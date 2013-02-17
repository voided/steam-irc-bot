using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using SteamKit2;

namespace SteamIrcBot
{
    static class Settings
    {
        const string SETTINGS_FILE = "settings.xml";

        public static SettingsXml Current { get; private set; }


        static Settings()
        {
            Current = new SettingsXml();
        }


        public static void Load()
        {
            string settingsPath = Path.Combine( Application.StartupPath, SETTINGS_FILE );

            Current = SettingsXml.Load( settingsPath );
        }

        public static void Save()
        {
            string settingsPath = Path.Combine( Application.StartupPath, SETTINGS_FILE );

            Current.Save( settingsPath );
        }
    }

    // the backing store of settings
    public class SettingsXml : XmlSerializable<SettingsXml>
    {
        public string SteamUsername;
        public string SteamPassword;

        public string WebAPIKey;

        public string IRCServer;
        public int IRCPort;

        public string IRCNick;

        public string IRCAnnounceChannel;
        public string IRCMainChannel;

        [XmlArrayItem( "AppID" )]
        public List<uint> ImportantApps;


        public SettingsXml()
        {
            ImportantApps = new List<uint>();

            IRCPort = 6667;
        }
    }
}
