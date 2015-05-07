using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using SteamKit2;

namespace SteamIrcBot
{
    abstract class Job
    {
        internal TimeSpan Period { get; set; }

        DateTime nextTick;


        internal void Run( bool force = false )
        {
            if ( DateTime.Now < nextTick && !force )
                return;

            nextTick = DateTime.Now + Period;

            OnRun();
        }

        protected abstract void OnRun();
    }

    class JobManager
    {
        Timer jobTimer;

        List<Job> registeredJobs;


        public JobManager( CallbackManager manager )
        {
            registeredJobs = new List<Job>();

            jobTimer = new Timer( OnTick );

            var jobTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where( t => t.IsSubclassOf( typeof( Job ) ) );

            foreach ( var type in jobTypes )
            {
                var job = Activator.CreateInstance( type, manager ) as Job;

                Log.WriteDebug( "JobManager", "Scheduling job {0} for every {1}", type.Name, job.Period );

                registeredJobs.Add( job );
            }
        }


        public void Start()
        {
            Log.WriteInfo( "JobManager", "Starting..." );

            jobTimer.Change( TimeSpan.FromSeconds( 1 ), TimeSpan.FromMilliseconds( -1 ) );
        }

        public void Stop()
        {
            jobTimer.Dispose();

            Log.WriteInfo( "JobManager", "Stopped" );
        }

        public void ForceRun<T>()
            where T : Job
        {
            var job = registeredJobs
                .FirstOrDefault( j => j.GetType() == typeof( T ) );

            if ( job == null )
                throw new InvalidOperationException( string.Format( "Attempting to force run a nonregistered job {0}", typeof( T ) ) );

            job.Run( true );
        }


        void OnTick( object state )
        {
            registeredJobs.ForEach( j => j.Run() );

            // trigger another job timer update in one second
            jobTimer.Change( TimeSpan.FromSeconds( 1 ), TimeSpan.FromMilliseconds( -1 ) );
        }
    }
}
