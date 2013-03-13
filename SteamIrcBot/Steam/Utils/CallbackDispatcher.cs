using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SteamIrcBot
{
    class CallbackDispatcher
    {
        static CallbackDispatcher _instance = new CallbackDispatcher();
        public static CallbackDispatcher Instance { get { return _instance; } }


        Task dispatcher;
        CancellationTokenSource cancelToken;


        CallbackDispatcher()
        {
            cancelToken = new CancellationTokenSource();
            dispatcher = new Task( DispatchCallbacks, cancelToken.Token, TaskCreationOptions.LongRunning );
        }


        void DispatchCallbacks()
        {
            while ( true )
            {
                if ( cancelToken.IsCancellationRequested )
                    break;

                Steam.Instance.CallbackManager.RunWaitCallbacks( TimeSpan.FromSeconds( 1 ) );

                IRC.Instance.CommandManager.ExpireRequests();
            }
        }



        public void Start()
        {
            dispatcher.Start();
        }

        public void Stop()
        {
            cancelToken.Cancel();
        }

    }
}
