using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SteamIrcBot
{
    class ServiceDispatcher
    {
        static ServiceDispatcher _instance = new ServiceDispatcher();
        public static ServiceDispatcher Instance { get { return _instance; } }


        Task dispatcher;
        CancellationTokenSource cancelToken;


        ServiceDispatcher()
        {
            cancelToken = new CancellationTokenSource();
            dispatcher = new Task( ServiceTick, cancelToken.Token, TaskCreationOptions.LongRunning );
        }


        void ServiceTick()
        {
            while ( true )
            {
                if ( cancelToken.IsCancellationRequested )
                    break;

                Steam.Instance.Tick();
                IRC.Instance.Tick();
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


        public void Wait()
        {
            dispatcher.Wait();
        }

    }
}
