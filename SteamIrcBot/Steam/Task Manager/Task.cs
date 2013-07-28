using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamIrcBot
{
    abstract class BaseTask
    {
        public abstract void Handle( object callback );
    }

    class JobTask<T> : BaseTask
        where T : CallbackMsg
    {
        public Task<T> Task { get { return completionSource.Task; } }


        TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();


        public override void Handle( object callback )
        {
            SteamClient.JobCallback<T> jobCallback = callback as SteamClient.JobCallback<T>;

            completionSource.SetResult( jobCallback.Callback );
        }
    }
}
