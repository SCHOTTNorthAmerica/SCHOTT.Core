namespace SCHOTT.Core.Threading
{
    /// <summary>
    /// Interface for threads.
    /// </summary>
    public interface IThreadInterface
    {
        /// <summary>
        /// Shutdown ready check for thread.
        /// </summary>
        /// <returns></returns>
        ClosingInfo ShutdownReady();
    }
}
