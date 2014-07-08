using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamIrcBot
{
    class TaskManager
    {
        ConcurrentDictionary<JobID, BaseTask> taskMap = new ConcurrentDictionary<JobID, BaseTask>();


        public TaskManager( CallbackManager manager )
        {
            new Callback<CallbackMsg>( OnJobCallback, manager );
        }


        public async Task<CallbackMsg> WaitForJob<T>( JobID jobId, TimeSpan? timeout = null )
        {
            if ( timeout == null )
                timeout = TimeSpan.FromSeconds( 10 );

            var jobTask = new JobTask();
            taskMap.TryAdd( jobId, jobTask );

            Task completedTask = await Task.WhenAny( jobTask.Task, Task.Delay( timeout.Value ) );

            // the jobtask completed or timed out, we no longer want to track it
            BaseTask ignored;
            taskMap.TryRemove( jobId, out ignored );

            if ( completedTask == jobTask.Task )
                return await jobTask.Task; // our job task completed before the timeout, return the result

            // job task timed out
            return null;
        }


        void OnJobCallback( CallbackMsg callback )
        {
            BaseTask task;

            if ( !taskMap.TryGetValue( callback.JobID, out task ) )
                return; // we're not waiting for this job

            task.Handle( callback );
        }
    }
}
