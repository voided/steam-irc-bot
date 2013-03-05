using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class BitVector64
    {
        private UInt64 data;

        public BitVector64()
        {
        }
        public BitVector64( UInt64 value )
        {
            data = value;
        }

        public UInt64 Data
        {
            get { return data; }
            set { data = value; }
        }

        public UInt64 this[ uint bitoffset, UInt64 valuemask ]
        {
            get
            {
                return ( data >> ( ushort )bitoffset ) & valuemask;
            }
            set
            {
                data = ( data & ~( valuemask << ( ushort )bitoffset ) ) | ( ( value & valuemask ) << ( ushort )bitoffset );
            }
        }
    }

    public class GID
    {
        BitVector64 gid;


        public GID()
            : this( ulong.MaxValue )
        {
        }
        public GID( ulong gid )
        {
            this.gid = new BitVector64( gid );
        }


        public static implicit operator ulong( GID gid )
        {
            return gid.gid.Data;
        }

        public static implicit operator GID( ulong gid )
        {
            return new GID( gid );
        }


        public uint SequentialCount
        {
            get { return ( uint )gid[ 0, 0xFFFFF ]; }
            set { gid[ 0, 0xFFFFF ] = ( ulong )value; }
        }

        public DateTime StartTime
        {
            get
            {
                uint startTime = ( uint )gid[ 20, 0x3FFFFFFF ];
                return new DateTime( 2005, 1, 1 ).AddSeconds( startTime );
            }
            set
            {
                uint startTime = ( uint )value.Subtract( new DateTime( 2005, 1, 1 ) ).TotalSeconds;
                gid[ 20, 0x3FFFFFFF ] = ( ulong )startTime;
            }
        }

        public uint ProcessID
        {
            get { return ( uint )gid[ 50, 0xF ]; }
            set { gid[ 50, 0xF ] = ( ulong )value; }
        }

        public uint BoxID
        {
            get { return ( uint )gid[ 54, 0x3FF ]; }
            set { gid[ 54, 0x3FF ] = ( ulong )value; }
        }

    }
}
