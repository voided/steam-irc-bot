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
        Timer ircDispatcher;

        WaitHandle ircDisposeEvent = new ManualResetEvent(false);
        CancellationTokenSource cancelToken;


        ServiceDispatcher()
        {
            cancelToken = new CancellationTokenSource();

            mainDispatcher = new Task(MainTick, cancelToken.Token, TaskCreationOptions.LongRunning);
            ircDispatcher = new Timer(IrcTick);
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

        void IrcTick(object state)
        {
            IRC.Instance.Tick();
        }



        public void Start()
        {
            mainDispatcher.Start();

            ircDispatcher.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
        }

        public void Stop()
        {
            ircDispatcher.Dispose(ircDisposeEvent);
            cancelToken.Cancel();
        }


        public void Wait()
        {
            ircDisposeEvent.WaitOne();
            mainDispatcher.Wait();
        }

    }
}
