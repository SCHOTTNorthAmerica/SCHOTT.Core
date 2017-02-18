using System.Collections.Generic;

namespace SCHOTT.Core.Communication
{
    /// <summary>
    /// Defines a common set of functions for all Telnet style connections.
    /// </summary>
    public interface ITextProtocol
    {
        /// <summary>
        /// Sends a command to the unit and check for an expected responce.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="expectedResponce">The responce to test for.</param>
        /// <param name="workingLine">The string returned by the unit.</param>
        /// <param name="removePrefix">When true, workingLine will only contain the string after expectedResponce.</param>
        /// <param name="prefixToRemove">The prefix to remove from the return string, defaults to expectedResponce string.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>True if ExpectedResponce is received, False otherwise.</returns>
        bool SendCommandSingleTest(string command, string expectedResponce, out string workingLine,
            bool removePrefix = false, string prefixToRemove = "", bool echoComTraffic = false);

        /// <summary>
        /// Send a command to the unit and return the responce. This function will only read 1 line for a response. 
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="removePrefix">When true, return string will only contain the string after expectedResponce.</param>
        /// <param name="prefixToRemove">The prefix to remove from the value, leave as default value to use command string.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>The unit responce to the command.</returns>
        string SendCommandSingle(string command, bool removePrefix = false, string prefixToRemove = "", bool echoComTraffic = false);

        /// <summary>
        /// Send a command to the unit.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="linesToRead">Number of lines to read for the responce. 
        /// Do not include this optional parameter to use the default value assigned at creation of the port.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>Returns a list of strings based on the default number of lines to read.</returns>
        List<string> SendCommand(string command, int linesToRead = -1, bool echoComTraffic = false);

        /// <summary>
        /// Send a command to the unit.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="linesToRead">Number of lines to read for the responce. 
        /// Do not include this optional parameter to use the default value assigned at creation of the port.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>Returns a list of strings based on the default number of lines to read.</returns>
        List<string> SendCommand(List<byte> command, int linesToRead = -1, bool echoComTraffic = false);

        /// <summary>
        /// Port capability for high speed.
        /// </summary>
        bool IsHighSpeed();
    }
}
