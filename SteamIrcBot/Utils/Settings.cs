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

        public static bool Validate()
        {
            if ( string.IsNullOrEmpty( Current.SteamUsername ) || string.IsNullOrEmpty( Current.SteamPassword ) )
            {
                Log.WriteError( "Settings", "Missing Steam credentials in settings file" );
                return false;
            }

            if ( string.IsNullOrEmpty( Current.WebAPIKey ) )
            {
                Log.WriteWarn( "Settings", "Missing Steam WebAPI key, customurl lookup will be unavailable" );
            }

            if ( string.IsNullOrEmpty( Current.IRCServer ) )
            {
                Log.WriteError( "Settings", "Missing IRC server in settings file" );
                return false;
            }

            if ( string.IsNullOrEmpty( Current.IRCNick ) )
            {
                Log.WriteError( "Settings", "Missing IRC nick in settings file" );
                return false;
            }

            if ( !Current.IsWebEnabled )
            {
                Log.WriteWarn( "Settings", "Missing WebPath/WebURL, web share will be unavailable" );
            }

            if ( Current.GCApp == 0 )
            {
                Log.WriteWarn( "Settings", "No GCApp configured, GC session info will be unavailable" );
            }

            return true;
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

        public uint GCApp;

        [XmlArrayItem( "AppID" )]
        public List<uint> ImportantApps;

        public string WebPath;
        public string WebURL;

        public string SteamDBChangelistURL;
        public string SteamDBHistoryURL;


        [XmlIgnore]
        public bool IsWebEnabled { get { return !string.IsNullOrEmpty( WebPath ) && !string.IsNullOrEmpty( WebURL ); } }


        public SettingsXml()
        {
            ImportantApps = new List<uint>();

            IRCPort = 6667;
        }
    }
}
