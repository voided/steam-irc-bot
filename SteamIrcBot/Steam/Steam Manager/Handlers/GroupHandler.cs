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
            foreach ( var announcement in callback.Announcements )
            {
                string announceUrl = string.Format( "http://steamcommunity.com/gid/{0}/announcements/detail/{1}", callback.ClanID.ConvertToUInt64(), announcement.ID.Value );
                IRC.Instance.SendAll( "{0} announcement: {1} - {2}", callback.ClanName, announcement.Headline, announceUrl );
            }

            foreach ( var clanEvent in callback.Events )
            {
                string eventUrl = string.Format( "http://steamcommunity.com/gid/{0}/events/{1}", callback.ClanID.ConvertToUInt64(), clanEvent.ID.Value );
                IRC.Instance.SendAll( "{0} event: {1} - {2}", callback.ClanName, clanEvent.Headline, eventUrl );
            }
        }
    }
}
