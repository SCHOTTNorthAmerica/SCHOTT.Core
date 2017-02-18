using SCHOTT.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCHOTT.Core.Threading
{
    /// <summary>
    /// A class to support closing the SCHOTT Threaded Workers provided in this library and others.
    /// </summary>
    public class ClosingWorker
    {
        private readonly List<IThreadInterface> _threadedItemsToClose = new List<IThreadInterface>();

        /// <summary>
        /// The internal MessageBroker for this FirmwareUploaderSerial
        /// </summary>
        private readonly MessageBroker _messageBroker = new MessageBroker();

        /// <summary>
        /// Register to updates of the Firmware uploader.
        /// </summary>
        /// <param name="context">The context in which to run the update action.</param>
        /// <param name="action">The lambda expression to execute on updates.</param>
        public void RegisterStatusUpdate(MessageBroker.MessageContext context, Action<string> action)
        {
            _messageBroker.Register("StatusUpdate", context, action);
        }

        private void RunStatusUpdate(string statusUpdate)
        {
            _messageBroker.RunActions("StatusUpdate", statusUpdate);
        }

        /// <summary>
        /// Add a thread for the ClosingWorker to close.
        /// </summary>
        /// <param name="thread"></param>
        public void AddThread(IThreadInterface thread)
        {
            _threadedItemsToClose.Add(thread);
        }
        
        /// <summary>
        /// Called when closing the application. This will ensure all SCHOTT threads 
        /// that were added to the worker are closed before returning. 
        /// </summary>
        /// <returns>True when threads are shut down, false when still closing threads.</returns>
        public ClosingInfo ShutdownThreads()
        {
            var closingInfo = new ClosingInfo();

            // check all the threaded classes
            closingInfo.ChildInfo.Clear();
            foreach (var o in _threadedItemsToClose)
                closingInfo.ChildInfo.Add(o.ShutdownReady());

            // update ClosingInfo
            if (closingInfo.ChildInfo.Any(x => x.ShutdownReady == false))
            {
                // we have children still waiting
                closingInfo.ShutdownReady = false;
                foreach (var info in closingInfo.ChildInfo.Where(x => x.ShutdownReady == false))
                {
                    if (closingInfo.Message.Length > 0)
                        closingInfo.Message += $"{Environment.NewLine}{Environment.NewLine}";

                    closingInfo.Message += $"{info.Message}";
                }
            }
            else
            {
                closingInfo.ShutdownReady = true;
            }

            RunStatusUpdate(closingInfo.Message);
            return closingInfo;
        }

    }
    
}
