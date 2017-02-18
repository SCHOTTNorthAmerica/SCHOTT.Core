using System.Collections.Generic;

namespace SCHOTT.Core.Threading
{
    /// <summary>
    /// Class to store information about a thread that is waiting to close.
    /// </summary>
    public class ClosingInfo
    {
        /// <summary>
        /// The message to report closing status.
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Closing status of any child threads.
        /// </summary>
        public List<ClosingInfo> ChildInfo { get; set; }

        /// <summary>
        /// List of strings from children still waiting to close.
        /// </summary>
        public List<string> ChildInfoMessages { get; set; }

        /// <summary>
        /// ShutdownReady will only be true when this thread and all children threads are complete and closed.
        /// </summary>
        public bool ShutdownReady { get; set; }

        /// <summary>
        /// Create ClosingInfo object.
        /// </summary>
        public ClosingInfo()
        {
            ChildInfo = new List<ClosingInfo>();
            ChildInfoMessages = new List<string>();
        }

        /// <summary>
        /// Return current closing message.
        /// </summary>
        /// <returns>Current closing message.</returns>
        public override string ToString()
        {
            return Message;
        }
    }    
}
