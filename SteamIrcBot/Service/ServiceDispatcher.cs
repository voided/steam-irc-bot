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


        Task mainDispatcher;
        Task ircDispatcher;
        CancellationTokenSource cancelToken;


        ServiceDispatcher()
        {
            cancelToken = new CancellationTokenSource();
            mainDispatcher = new Task(MainTick, cancelToken.Token, TaskCreationOptions.LongRunning);
            ircDispatcher = new Task(IrcTick, cancelToken.Token, TaskCreationOptions.LongRunning);
        }


        void MainTick()
        {
            while ( true )
            {
                if ( cancelToken.IsCancellationRequested )
                    break;

                Steam.Instance.Tick();
                RSS.Instance.Tick();
            }
        }

        void IrcTick()
        {
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                IRC.Instance.Tick();
            }
        }



        public void Start()
        {
            mainDispatcher.Start();
            ircDispatcher.Start();
        }

        public void Stop()
        {
            cancelToken.Cancel();
        }


        public void Wait()
        {
            Task.WaitAll(mainDispatcher, ircDispatcher);
        }

    }
}
