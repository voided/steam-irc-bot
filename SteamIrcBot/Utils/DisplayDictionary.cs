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


        public new void Add( string key, string value )
        {
            if ( string.IsNullOrEmpty( value ) )
                return;

            value = value.Clean();

            base[ key ] = value.Truncate( maxValueLen );
        }
        public void Add( string key, object value )
        {
            if ( value == null )
                return;

            this.Add( key, value.ToString() );
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
