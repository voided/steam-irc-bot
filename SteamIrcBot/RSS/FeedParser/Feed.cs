using System;
using System.Collections.Generic;
using System.Text;

namespace SteamIrcBot.RSS.FeedParser
{
    public class Feed
    {
        public FeedType Type { get; set; }

        public string Title { get; set; }

        public List<Item> Items { get; set; }
    }
}
