using System;
using System.ComponentModel;
using System.IO;

namespace SCHOTT.Core.Extensions
{
    /// <summary>
    /// An advanced StringWriter with flush functions and event to notify the user when it is written too.
    /// </summary>
    public sealed class AdvancedStringWriter : StringWriter
    {
        /// <summary>
        /// An event that is raised whenever the AdvancedStringWriter is flushed.
        /// </summary>
        /// <param name="sender">The StringWriter that the event is raised from.</param>
        /// <param name="args">The args passed by the StringWriter event.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public delegate void FlushedEventHandler(object sender, EventArgs args);

        /// <summary>
        /// An event that is raised whenever the AdvancedStringWriter is flushed.
        /// </summary>
        public event FlushedEventHandler Flushed;

        private bool AutoFlush { get; set; }
        private bool AutoClear { get; set; }

        /// <summary>
        /// Create a flushable StringWriter
        /// </summary>
        public AdvancedStringWriter()
        { }

        /// <summary>
        /// Create a flushable StringWriter
        /// </summary>
        /// <param name="autoFlush">Determines if the AdvancedStringWriter will flush automatically anytime it is written too.</param>
        /// <param name="autoClear">Determines if the AdvancedStringWriter will clear itself after every flush event.</param>
        public AdvancedStringWriter(bool autoFlush, bool autoClear)
        {
            AutoFlush = autoFlush;
            AutoClear = autoClear;
        }

        private void OnFlush()
        {
            var eh = Flushed;
            eh?.Invoke(this, EventArgs.Empty);
            if (AutoClear) GetStringBuilder().Length = 0;
        }

        /// <summary>
        /// Flush the StringWriter
        /// </summary>
        public override void Flush()
        {
            base.Flush();
            OnFlush();
        }

        /// <summary>
        /// Write a character to the StringWriter
        /// </summary>
        public override void Write(char value)
        {
            base.Write(value);
            if (AutoFlush) Flush();
        }

        /// <summary>
        /// Write a string to the StringWriter
        /// </summary>
        public override void Write(string value)
        {
            base.Write(value);
            if (AutoFlush) Flush();
        }

        /// <summary>
        /// Write a character buffer to the StringWriter
        /// </summary>
        public override void Write(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            if (AutoFlush) Flush();
        }
    }

}
