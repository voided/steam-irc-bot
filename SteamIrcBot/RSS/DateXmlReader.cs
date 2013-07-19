using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;

namespace SteamIrcBot
{
    class BadDateTimeParser
    {
        public static string Parse( string input )
        {
            DateTime dateTime;

            if ( !DateTime.TryParse( input, out dateTime ) )
            {
                // badly formatted date, try stripping the timezone
                input = input.Substring( 0, input.LastIndexOf( ' ' ) );

                if ( !DateTime.TryParse( input, out dateTime ) )
                {
                    throw new FormatException( string.Format( "Unable to parse datetime: {0}", input ) );
                }
            }

            return dateTime.ToUniversalTime().ToString( "R", CultureInfo.InvariantCulture );
        }
    }

    class DateXmlReader : XmlTextReader
    {
        bool isReadingDate = false;


        public DateXmlReader( Stream stream )
            : base( stream )
        {
        }


        public override void ReadStartElement()
        {
            string[] dateElements =
            {
                "lastBuildDate",
                "pubDate"
            };

            // are we parsing a datetime rss element?
            if ( dateElements.Any( e => string.Equals( e, base.LocalName, StringComparison.OrdinalIgnoreCase ) ) )
            {
                isReadingDate = true;
            }

            base.ReadStartElement();
        }
        public override void ReadEndElement()
        {
            isReadingDate = false;

            base.ReadEndElement();
        }

        public override string ReadString()
        {
            string elementText = base.ReadString();

            if ( isReadingDate )
            {
                // attempt to sanely parse this datetime
                elementText = BadDateTimeParser.Parse( elementText );
            }

            return elementText;
        }


        public static XmlReader Create( Stream input )
        {
            return new DateXmlReader( input );
        }

    }
}
