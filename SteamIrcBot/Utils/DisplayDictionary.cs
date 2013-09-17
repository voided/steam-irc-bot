using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class DisplayDictionary : Dictionary<string, string>
    {
        int maxValueLen;


        public DisplayDictionary( int maxLen = 100 )
        {
            maxValueLen = maxLen;
        }


        public void Add( string key, string value, bool quotes = false )
        {
            if ( string.IsNullOrEmpty( value ) )
                return;

            value = value
                .Clean()
                .Truncate( maxValueLen );

            if ( quotes )
                value = string.Format( "\"{0}\"", value );

            base[ key ] = value;
        }
        public void Add( string key, object value, bool quotes = false )
        {
            if ( value == null )
                return;

            this.Add( key, value.ToString(), quotes );
        }

        public override string ToString()
        {
            var displayValues = this.Select( kvp =>
            {
                return string.Format( "{0}: {1}", kvp.Key, kvp.Value );
            } );

            return string.Join( ", ", displayValues );
        }
    }
}
