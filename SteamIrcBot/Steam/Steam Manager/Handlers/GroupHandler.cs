using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class GroupHandler : SteamHandler
    {
        public GroupHandler( CallbackManager manager )
            : base( manager )
        {
            new Callback<SteamFriends.ClanStateCallback>( OnClanState, manager );
        }


        void OnClanState( SteamFriends.ClanStateCallback callback )
        {
            string clanName = callback.ClanName;

            if ( string.IsNullOrWhiteSpace( clanName ) )
                clanName = Steam.Instance.Friends.GetClanName( callback.ClanID );

            if ( string.IsNullOrWhiteSpace( clanName ) || clanName == "[unknown]" ) // god this sucks. why on earth did i make steamkit follow steamclient to the letter
                clanName = "Group";

            foreach ( var announcement in callback.Announcements )
            {
                string announceUrl = string.Format( "http://steamcommunity.com/gid/{0}/announcements/detail/{1}", callback.ClanID.ConvertToUInt64(), announcement.ID.Value );
                IRC.Instance.SendToTag( "steam-news", "{0} announcement: {1} - {2}", clanName, announcement.Headline, announceUrl );
            }

            foreach ( var clanEvent in callback.Events )
            {
                if ( !clanEvent.JustPosted )
                    continue; // we're only interested in recent clan events

                string eventUrl = string.Format( "http://steamcommunity.com/gid/{0}/events/{1}", callback.ClanID.ConvertToUInt64(), clanEvent.ID.Value );
                IRC.Instance.SendToTag( "steam-news", "{0} event: {1} - {2}", clanName, clanEvent.Headline, eventUrl );
            }
        }
    }
}
