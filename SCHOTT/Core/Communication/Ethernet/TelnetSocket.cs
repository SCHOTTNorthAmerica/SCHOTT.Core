using SCHOTT.Core.Extensions;
using SCHOTT.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SCHOTT.Core.Communication.Ethernet
{
    /// <summary>
    /// A TelnetSocket class that uses the AsyncSocket base to create a Telnet interface to a hardware device. 
    /// This class includes functions to easyily make sending commands and recieving responces in a syncronus way possible.
    /// </summary>
    public class TelnetSocket : AsyncSocket, ITextProtocol
    {
        private readonly List<byte> _socketInFifo = new List<byte>();

        /// <summary>
        /// Allows the user to register for output messages from the class. 
        /// This is used when the user wants to echo traffic from the commands.
        /// See SendCommandWithEcho.
        /// </summary>
        /// <param name="context">The context in which to execture the action.</param>
        /// <param name="action">The action to execute on a message output event.</param>
        public void RegisterMessageOutput(MessageBroker.MessageContext context, Action<string> action)
        {
            MessageBroker.Register("MessageOutput", context, action);
        }

        private void RunMessageOutput(string args)
        {
            MessageBroker.RunActions("MessageOutput", args);
        }

        /// <summary>
        /// The parameters of the port.
        /// </summary>
        protected readonly ComParameters ComParameters = new ComParameters();
        private readonly object _comLock = new object();

        /// <summary>
        /// Default contstructor using default ComParameters.
        /// </summary>
        public TelnetSocket()
        {
            
        }

        /// <summary>
        /// Create a TelnetSocket using specific ComParameters.
        /// </summary>
        /// <param name="comParameters"></param>
        public TelnetSocket(ComParameters comParameters)
        {
            ComParameters.CopyFrom(comParameters);
        }

        #region External Functions

        /// <summary>
        /// Port capability for high speed.
        /// </summary>
        public bool IsHighSpeed()
        {
            return true;
        }

        /// <summary>
        /// Tests the connection to the unit using the default command and expected responce pair. 
        /// The function will send the ComParameters.Command to the unit, and will check for the ComParameters.ExpectedResponce.
        /// </summary>
        /// <returns>True if ExpectedResponce is received, False otherwise.</returns>
        public bool SendConnectionTest()
        {
            return SendCommandSingle(ComParameters.Command).Contains(ComParameters.ExpectedResponce);
        }

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
        public bool SendCommandSingleTest(string command, string expectedResponce, out string workingLine,
            bool removePrefix = false, string prefixToRemove = "", bool echoComTraffic = false)
        {
            // send command and get responce
            var line = SendCommand(command, 1, echoComTraffic).FirstOrDefault() ?? "";
            workingLine = "";

            if (removePrefix)
            {
                if (prefixToRemove == "")
                    prefixToRemove = expectedResponce;

                var index = line.IndexOf(prefixToRemove, StringComparison.Ordinal);

                workingLine = index >= 0 ? line.Substring(index + prefixToRemove.Length) : line;
            }

            // see if we got the expected responce back
            return line.Contains(expectedResponce);
        }

        /// <summary>
        /// Send a command to the unit and return the responce. This function will only read 1 line for a response. 
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="removePrefix">When true, return string will only contain the string after expectedResponce.</param>
        /// <param name="prefixToRemove">The prefix to remove from the value, leave as default value to use command string.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>The unit responce to the command.</returns>
        public string SendCommandSingle(string command, bool removePrefix = false, string prefixToRemove = "", bool echoComTraffic = false)
        {
            // send command and get responce
            var line = SendCommand(command, 1, echoComTraffic).FirstOrDefault() ?? "";

            if (!removePrefix)
                return line;

            if (prefixToRemove == "")
                prefixToRemove = command;

            var index = line.IndexOf(prefixToRemove, StringComparison.Ordinal);

            return index >= 0 ? line.Substring(index + prefixToRemove.Length) : line;
        }

        /// <summary>
        /// Send a command to the unit.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="linesToRead">Number of lines to read for the responce. 
        /// Do not include this optional parameter to use the default value assigned at creation of the port.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>Returns a list of strings based on the default number of lines to read.</returns>
        public List<string> SendCommand(string command, int linesToRead = -1, bool echoComTraffic = false)
        {
            return SendCommand(Encoding.ASCII.GetBytes(command).ToList(), linesToRead, echoComTraffic);
        }

        /// <summary>
        /// Send a command to the unit.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="linesToRead">Number of lines to read for the responce. 
        /// Do not include this optional parameter to use the default value assigned at creation of the port.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>Returns a list of strings based on the default number of lines to read.</returns>
        public List<string> SendCommand(List<byte> command, int linesToRead = -1, bool echoComTraffic = false)
        {
            if (linesToRead < 0)
                linesToRead = ComParameters.LinesToRead;

            var linesToReturn = new List<string> { "" };

            if (!Monitor.TryEnter(_comLock, ComParameters.MaxDelayMilliseconds))
                return linesToReturn;

            try
            {
                if (IsConnected)
                {
                    _socketInFifo.Clear();

                    // send the data
                    var commandClone = command.CloneList();
                    commandClone.Add((byte)ComParameters.TerminationChar);

                    if (echoComTraffic)
                    {
                        RunMessageOutput($"USER> {Encoding.ASCII.GetString(commandClone.ToArray())}{Environment.NewLine}");
                    }

                    if (!ComParameters.DelaySecondCharacter || commandClone.Count == 1)
                    {
                        SendData(commandClone.ToArray());
                    }
                    else
                    {
                        var array = commandClone.ToArray();
                        SendData(array, 0, 1);
                        TimeFunctions.Wait(50);
                        SendData(array, 1, commandClone.Count - 1);
                    }

                    if (linesToRead > 0)
                    {
                        linesToReturn = ReadLine(linesToRead, ComParameters.TimeoutMilliseconds);
                        if (echoComTraffic)
                        {
                            var returnStrings = string.Join(Environment.NewLine, linesToReturn.ToArray());
                            RunMessageOutput($"UNIT> {returnStrings}{Environment.NewLine}");
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                Monitor.Exit(_comLock);
            }

            return linesToReturn;
        }
        
        #endregion

        #region Internal Functions

        private void SendMessage(string message)
        {
            SendData(Encoding.ASCII.GetBytes(message));
        }

        private string ReadExisting()
        {
            var returnString = Encoding.UTF8.GetString(_socketInFifo.ToArray());
            _socketInFifo.RemoveRange(0, returnString.Length);
            return returnString;
        }

        /// <summary>
        /// This function will put any received characters from the AsyncSocket into the ReceiveFIFO
        /// </summary>
        /// <param name="data"></param>
        protected override void ProcessReceivedData(List<byte> data)
        {
            _socketInFifo.AddRange(data);
        }

        private List<string> ReadLine(int linesToRead, int timeoutMilliseconds)
        {
            var workingString = "";
            var finishBy = DateTime.Now.AddMilliseconds(timeoutMilliseconds);

            while (DateTime.Now < finishBy && IsConnected)
            {
                if (_socketInFifo.Count > 0)
                {
                    workingString += ReadExisting();

                    // special case for dealing with linux vs windows new line commands
                    workingString = workingString.Replace("\n", "");

                    // we have at least started the last requested line
                    var workingStrings = workingString.Split(new[] { ComParameters.TerminationChar }).ToList();

                    if (ComParameters.EndPrompt.Length > 0 && workingStrings.Any(s => s.Contains(ComParameters.EndPrompt)))
                    {
                        workingStrings.RemoveAll(s => s.Contains(ComParameters.EndPrompt));
                        return workingStrings;
                    }
                    
                    if (workingStrings.Count >= linesToRead)
                    {
                        // now we need to check to make sure the last line has the closing character
                        var lastLine = workingString.Substring(workingString.IndexOf(workingStrings[linesToRead - 1],
                                StringComparison.Ordinal));

                        if (lastLine.Contains(ComParameters.TerminationChar))
                        {
                            return workingStrings;
                        }
                    }
                }

                TimeFunctions.Wait(10);
            }

            // we failed so return empty
            return new List<string> { "" };
        }

        #endregion

    }

}
