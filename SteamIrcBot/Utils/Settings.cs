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
        public class RssFeedXml
        {
            [XmlAttribute]
            public bool IsRss10;

            [XmlAttribute]
            public string URL;

            public RssFeedXml()
            {
                IsRss10 = false;
            }

            public override string ToString()
            {
                return URL;
            }
        }

        public class IrcChannel
        {
            [XmlAttribute]
            public string Channel;
            [XmlAttribute]
            public string Tags;


            public IEnumerable<string> GetTags()
            {
                if ( string.IsNullOrEmpty( Tags ) )
                    return Enumerable.Empty<string>();

                return Tags.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
            }
        }


        [ConfigHidden]
        public string SteamUsername;
        [ConfigHidden]
        public string SteamPassword;

        [ConfigHidden]
        public string WebAPIKey;

        public string IRCServer;
        public int IRCPort;

        public string IRCNick;

        public List<IrcChannel> Channels;

        [XmlArrayItem( "Admin" ), ConfigHidden]
        public List<string> IRCAdmins;

        public uint GCApp;

        [XmlArrayItem( "AppID" )]
        public List<uint> ImportantApps;

        [ConfigHidden]
        public string WebPath;
        public string WebURL;

        public string SteamDBChangelistURL;
        public string SteamDBAppHistoryURL;
        public string SteamDBPackageHistoryURL;

        [XmlArrayItem( "Feed" )]
        public List<RssFeedXml> RssFeeds;

        [XmlArrayItem( "Quote" )]
        public List<string> BrunoQuotes;


        [XmlIgnore]
        public bool IsWebEnabled { get { return !string.IsNullOrEmpty( WebPath ) && !string.IsNullOrEmpty( WebURL ); } }


        public SettingsXml()
        {
            ImportantApps = new List<uint>();

            IRCPort = 6667;

            IRCAdmins = new List<string>();

            SteamDBChangelistURL = "http://steamdb.info/changelist/{0}/";
            SteamDBAppHistoryURL = "http://steamdb.info/app/{0}/#section_history";
            SteamDBPackageHistoryURL = "http://steamdb.info/sub/{0}/#section_history";

            BrunoQuotes = new List<string>();

            Channels = new List<IrcChannel>();
        }


        internal bool IsAdmin( SenderDetails sender )
        {
            return IRCAdmins.Any( a => string.Equals( sender.Hostname, a, StringComparison.OrdinalIgnoreCase ) );
        }

        internal IEnumerable<IrcChannel> GetChannelsForTag( string tag )
        {
            // return any channels that contain the given tag

            return Channels.Where( c =>
            {
                return c.GetTags()
                    .Any( chanTag => string.Equals( tag, chanTag, StringComparison.OrdinalIgnoreCase ) );
            } );
        }
    }
}
