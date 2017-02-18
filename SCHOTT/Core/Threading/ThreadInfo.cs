using SCHOTT.Core.StateMachine;
using System.Threading;

namespace SCHOTT.Core.Threading
{
    /// <summary>
    /// Stores information used by other classes on the thread.
    /// </summary>
    public class ThreadInfo
    {
        /// <summary>
        /// Name of the parent thread.
        /// </summary>
        public string ParentThreadName { get; private set; }

        /// <summary>
        /// Name of this thread.
        /// </summary>
        public string ThreadName { get; }

        /// <summary>
        /// Stores the StateMachine used in this thread.
        /// </summary>
        public StepDefinition WorkerStateMachine { get; }

        private Thread Thread { get; }

        /// <summary>
        /// WaitHandle used in the StateMachine
        /// </summary>
        public EventWaitHandle WaitHandle { get; set; }

        private int _applicationClosingCount;

        /// <summary>
        /// Flag to close the thread as soon as possible.
        /// </summary>
        public bool CloseThread { get; private set; }

        /// <summary>
        /// Create the ThreadInfo object.
        /// </summary>
        /// <param name="worker">Delegate to the worker thread.</param>
        /// <param name="threadName">Name of this thread. Used in status printouts.</param>
        /// <param name="parentThreadName">Name of parent thread.</param>
        /// <param name="workerStateMachine">The StateMachine used in this thread.</param>
        public ThreadInfo(Thread worker, string threadName, string parentThreadName, StepDefinition workerStateMachine)
        {
            CloseThread = false;

            ParentThreadName = parentThreadName;
            ThreadName = threadName;
            WorkerStateMachine = workerStateMachine;

            if (WorkerStateMachine != null)
                WorkerStateMachine.RootThread = this;

            WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            Thread = worker;
            Thread.Start(this);
        }

        /// <summary>
        /// Function to begin shutting down the thread and query the status.
        /// </summary>
        /// <returns>True = StateMachine is finished and thread is closed, False otherwise.</returns>
        public ClosingInfo ShutdownReady()
        {
            var closingInfo = new ClosingInfo();

            if (Thread.ThreadState != ThreadState.Stopped)
            {
                CloseThread = true;
                WaitHandle.Set();
                closingInfo.Message = MessageStatus($"Closing Thread ({ThreadName}): Waiting", _applicationClosingCount++);
                closingInfo.ShutdownReady = false;
                return closingInfo;
            }

            closingInfo.Message = $"Closing Thread ({ThreadName}): Done!";
            closingInfo.ShutdownReady = true;
            return closingInfo;
        }

        /// <summary>
        /// Function to add elipses indicating that the thread closing is still in process.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dots"></param>
        /// <returns></returns>
        public static string MessageStatus(string message, int dots)
        {
            var msg = message + "•";
            for (var i = 0; i < dots % 5; i++) { msg += "•"; }
            return msg;
        }
    }
}
