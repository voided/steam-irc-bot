using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SteamKit2;
using SteamKit2.GC;
using ProtoBuf;

namespace SteamIrcBot
{
    class GCHandler
    {
        public GCHandler( GCManager manager )
        {
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

            new Callback<SteamGameCoordinator.MessageCallback>( OnGCMessage, manager );

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
