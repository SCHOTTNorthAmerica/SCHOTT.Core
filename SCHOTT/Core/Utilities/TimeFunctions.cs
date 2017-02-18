using System;
using System.Threading;
using System.Windows.Forms;

namespace SCHOTT.Core.Utilities
{
    /// <summary>
    /// An extension class for time functions.
    /// </summary>
    public static class TimeFunctions
    {
        /// <summary>
        /// Blocking function to wait some number of milliseconds.
        /// </summary>
        /// <param name="millisecondTimeout">Number of milliseconds to wait.</param>
        public static void Wait(int millisecondTimeout)
        {
            var wait = new EventWaitHandle(false, EventResetMode.AutoReset);
            wait.WaitOne(millisecondTimeout);
        }

        /// <summary>
        /// Blocking function to wait some number of milliseconds, calls Application.DoEvents while waiting.
        /// </summary>
        /// <param name="millisecondTimeout">Number of milliseconds to wait.</param>
        public static void WaitSpin(int millisecondTimeout)
        {
            var finish = DateTime.Now.AddMilliseconds(millisecondTimeout);
            while (DateTime.Now < finish)
            {
                Application.DoEvents();
            }
        }
    }
    
}
