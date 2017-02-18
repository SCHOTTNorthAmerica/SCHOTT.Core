using System;

namespace SCHOTT.Core.Utilities
{
    /// <summary>
    /// Functions to help with console output
    /// </summary>
    public static class ConsoleFunctions
    {
        /// <summary>
        /// Clears a number of lines from the console
        /// </summary>
        /// <param name="lines">The number of lines to remove from the end of the console</param>
        public static void ClearLine(int lines = 1)
        {
            for (var i = 0; i < lines; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
        }

    }
}
