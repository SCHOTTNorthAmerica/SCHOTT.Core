using SCHOTT.Core.Threading;
using SCHOTT.Core.Utilities;
using System;

namespace SCHOTT.Core.Extensions
{
    /// <summary>
    /// A class to support closing the SCHOTT Threaded Workers provided in this library and others.
    /// </summary>
    public static class ClosingWorkerExtensions
    {
        /// <summary>
        /// A function that blocks until all threads in the supplied closing worker are complete and closed.
        /// This function will display status in the console.
        /// </summary>
        /// <param name="closingWorker">The ClosingWorker to perform the actions on.</param>
        public static void WaitForThreadsToCloseNoOutput(this ClosingWorker closingWorker)
        {
            // run the closing worker until all child threads are closed
            while (closingWorker.ShutdownThreads().ShutdownReady == false)
            {
                TimeFunctions.Wait(50);
            }
        }

        /// <summary>
        /// A function that blocks until all threads in the supplied closing worker are complete and closed.
        /// This function will display status in the console.
        /// </summary>
        /// <param name="closingWorker">The ClosingWorker to perform the actions on.</param>
        public static void WaitForThreadsToCloseConsoleOutput(this ClosingWorker closingWorker)
        {
            // run the closing worker until all child threads are closed
            ClosingInfo status;
            var cycles = 0;
            var previousLines = 0;
            while ((status = closingWorker.ShutdownThreads()).ShutdownReady == false)
            {
                if (cycles++ % 6 == 0)
                {
                    ConsoleFunctions.ClearLine(previousLines);
                    Console.WriteLine(status.Message);
                    previousLines = status.Message.Split('\r').Length;
                }
                TimeFunctions.Wait(50);
            }

            // clean up console
            ConsoleFunctions.ClearLine(previousLines);
            Console.WriteLine("Shutdown Threads Complete!");
        }
    }
}
