using System;
using System.Collections.Generic;
using System.Linq;

namespace SCHOTT.Core.Threading
{
    /// <summary>
    /// Base class to setup threaded workers.
    /// </summary>
    public class ThreadingBase : IThreadInterface
    {
        /// <summary>
        /// List of worker threads
        /// </summary>
        protected List<ThreadInfo> WorkerThreads;

        /// <summary>
        /// Closing count for thread, used to set number of elipses in the message
        /// </summary>
        protected int ApplicationClosingCount;

        /// <summary>
        /// The neame of this thread
        /// </summary>
        protected string ThreadName;

        /// <summary>
        /// Create the threaded object
        /// </summary>
        /// <param name="threadName"></param>
        public ThreadingBase(string threadName)
        {
            ThreadName = threadName;
        }

        /// <summary>
        /// Get shutdown status
        /// </summary>
        /// <returns>ClosingInfo for this thread and all children</returns>
        public virtual ClosingInfo ShutdownReady()
        {
            var closingInfo = new ClosingInfo();

            // poke each thread then see if it is complete
            AddDerivedClosingInfoChildren(closingInfo);
            foreach (var info in WorkerThreads)
                closingInfo.ChildInfo.Add(info.ShutdownReady());

            // update ClosingInfo
            if (closingInfo.ChildInfo.Any(x => x.ShutdownReady == false))
            {
                // we have children still waiting
                closingInfo.Message = ThreadInfo.MessageStatus($"Closing Thread ({ThreadName}): Waiting", ApplicationClosingCount++);
                closingInfo.ShutdownReady = false;
                foreach (var info in closingInfo.ChildInfo.Where(x => x.ShutdownReady == false))
                {
                    closingInfo.Message += $"{Environment.NewLine}  {info.Message.Replace(Environment.NewLine, $"{Environment.NewLine}  ")}";
                }
            }
            else
            {
                closingInfo.Message = $"Closing Thread ({ThreadName}): Done!";
                closingInfo.ShutdownReady = true;
            }

            // return readiness to shutdown
            return closingInfo;
        }

        /// <summary>
        /// Function to be overridden by derived classes. Processing of data should be done here.
        /// </summary>
        /// <param name="closingInfo">The closing info object to add children too.</param>
        protected virtual void AddDerivedClosingInfoChildren(ClosingInfo closingInfo) { }
    }
}
