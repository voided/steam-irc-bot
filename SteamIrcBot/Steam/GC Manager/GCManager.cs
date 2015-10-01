using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SteamKit2;
using SteamKit2.GC;
using ProtoBuf;
using System.IO;
using SteamKit2.GC.Internal;

namespace SteamIrcBot
{
    class GCHandler
    {
        protected GCManager Manager { get; set; }

        public GCHandler( GCManager manager )
        {
            this.Manager = manager;
        }
    }

    class InjectedGCMsg : IPacketGCMsg
    {
        uint eMsg;
        byte[] data;


        public InjectedGCMsg( uint eMsg, byte[] data )
        {
            this.eMsg = eMsg;
            this.data = data;
        }


        public bool IsProto { get { return true; } }

        public uint MsgType { get { return eMsg; } }

        public JobID TargetJobID { get { return JobID.Invalid; } }
        public JobID SourceJobID { get { return JobID.Invalid; } }

        public byte[] GetData()
        {
            // in order for this to work, we need to craft a whole fake gc message, including the header

            using ( var ms = new MemoryStream() )
            using ( var bw = new BinaryWriter( ms ) )
            {
                byte[] header;

                using ( var headerStream = new MemoryStream() )
                {
                    Serializer.Serialize( headerStream, new CMsgProtoBufHeader() );
                    header = headerStream.ToArray();
                }

                bw.Write( eMsg );
                bw.Write( (uint)header.Length );
                bw.Write( header );

                // now finally write the body
                bw.Write( data );

                return ms.ToArray();
            }
        }
    }

    class GCManager
    {
        List<GCCallback> callbacks;
        List<GCHandler> handlers;


        public GCManager( CallbackManager manager )
        {
            callbacks = new List<GCCallback>();
            handlers = new List<GCHandler>();

            manager.Subscribe<SteamGameCoordinator.MessageCallback>( OnGCMessage );

            var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where( t => t.IsSubclassOf( typeof( GCHandler ) ) );

            foreach ( var type in handlerTypes )
            {
                var handler = Activator.CreateInstance( type, this ) as GCHandler;

                Log.WriteDebug( "GCManager", "Registering handler {0}", type );

                handlers.Add( handler );
            }
        }


        internal void Register( GCCallback callback )
        {
            callbacks.Add( callback );
        }

        public void InjectExtraMessage( uint eMsg, byte[] message, uint gcAppId )
        {
            Log.WriteDebug( "GCManager", "Got {0} injected GC message {1}", gcAppId, GetEMsgName( eMsg ) );

            var matchingCallbacks = callbacks
                .Where( call => call.EMsg == eMsg )
                .ToList();

            if ( matchingCallbacks.Count == 0 )
            {
                Log.WriteWarn( "GCManager", "Got {0} injected GC message {1}, but was unhandled", gcAppId, GetEMsgName( eMsg ) );
                return;
            }

            foreach ( var call in matchingCallbacks )
            {
                // we craft fake gc messages since the gc is only giving us the body, without a header
                call.Run( new InjectedGCMsg( eMsg, message ), gcAppId );
            }
        }


        void OnGCMessage( SteamGameCoordinator.MessageCallback callback )
        {
            Log.WriteDebug( "GCManager", "Got {0} GC message {1}", callback.AppID, GetEMsgName( callback.EMsg ) );

            var matchingCallbacks = callbacks
                .Where( call => call.EMsg == callback.EMsg );

            foreach ( var call in matchingCallbacks )
            {
                call.Run( callback.Message, callback.AppID );
            }
        }

        string GetEMsgName( uint eMsg )
        {
            // first lets try the enum'd emsgs
            Type[] eMsgEnums =
            {
                typeof( SteamKit2.GC.Internal.EGCBaseMsg ),
                typeof( SteamKit2.GC.Internal.EGCBaseClientMsg ),
                typeof( SteamKit2.GC.Internal.ESOMsg ),
                typeof( SteamKit2.GC.Internal.EGCSystemMsg ),
                typeof( SteamKit2.GC.Internal.EGCItemMsg ),
                typeof( SteamKit2.GC.Internal.EGCToGCMsg ),
                typeof( SteamKit2.GC.Dota.Internal.EDOTAGCMsg ),
                typeof( SteamKit2.GC.TF2.Internal.EGCBaseMsg ),
                typeof( SteamKit2.GC.TF2.Internal.ETFGCMsg ),
            };

            foreach ( var enumType in eMsgEnums )
            {
                if ( Enum.IsDefined( enumType, ( int )eMsg ) )
                    return Enum.GetName( enumType, ( int )eMsg );
            }

            // try the tf2 emsgs
            foreach ( var field in typeof( SteamKit2.GC.TF2.EGCMsg ).GetFields( BindingFlags.Public | BindingFlags.Static ) )
            {
                uint value = ( uint )field.GetValue( null );

                if ( value == eMsg )
                    return field.Name;
            }

            // no dice, we can only use the uint
            return eMsg.ToString();
        }

    }

    abstract class GCCallback
    {
        public uint EMsg { get; protected set; }

        abstract internal void Run( IPacketGCMsg msg, uint gcAppId );
    }

    class GCCallback<TMsg> : GCCallback
        where TMsg : IExtensible, new()
    {

        GCManager manager;
        Action<ClientGCMsgProtobuf<TMsg>, uint> func;


        public GCCallback( uint eMsg, Action<ClientGCMsgProtobuf<TMsg>, uint> func, GCManager mgr )
        {
            this.EMsg = eMsg;

            this.func = func;
            this.manager = mgr;

            mgr.Register( this );
        }


        internal override void Run( IPacketGCMsg msg, uint gcAppId )
        {
            var obj = Activator.CreateInstance( typeof( ClientGCMsgProtobuf<TMsg> ), msg ) as ClientGCMsgProtobuf<TMsg>;
            func( obj, gcAppId );
        }
    }
}
