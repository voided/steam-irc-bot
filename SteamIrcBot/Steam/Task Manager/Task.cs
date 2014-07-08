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
        public abstract void Handle( CallbackMsg callback );
    }

    class JobTask : BaseTask
    {
        public Task<CallbackMsg> Task { get { return completionSource.Task; } }


        TaskCompletionSource<CallbackMsg> completionSource = new TaskCompletionSource<CallbackMsg>();


        public override void Handle( CallbackMsg callback )
        {
            completionSource.SetResult( callback );
        }
    }
}
