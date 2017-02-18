namespace SCHOTT.Core.Communication
{
    /// <summary>
    /// Common parameters used to set up any com port or telnet socket
    /// </summary>
    public class ComParameters
    {
        /// <summary>
        /// Baud rate to use on connections that need a baud rate.
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// The command to send for checking unit connection
        /// </summary>
        public string Command { get; set; } = "";

        /// <summary>
        /// The expected responce from a unit during the connection test
        /// </summary>
        public string ExpectedResponce { get; set; } = "";

        /// <summary>
        /// How many lines to read by default on command functions.
        /// </summary>
        public int LinesToRead { get; set; } = 1;

        /// <summary>
        /// The character used to signify the end of a line.
        /// </summary>
        public char TerminationChar { get; set; } = '\r';

        /// <summary>
        /// The prompt supplied by the unit to signify it is ready for additional input.
        /// </summary>
        public string EndPrompt { get; set; } = "";

        /// <summary>
        /// How long to wait in milliseconds for a command to receive the correct number of lines, or the EndPrompt.
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 500;

        /// <summary>
        /// How long to wait for other commands to repeat before sending a command.
        /// </summary>
        public int MaxDelayMilliseconds { get; set; } = 500;

        /// <summary>
        /// When sending a command, this will insert a 50 millisecond delay between the first char and the rest of the command.
        /// This is required by some hardware types.
        /// </summary>
        public bool DelaySecondCharacter { get; set; } = false;
    }

}
