using SCHOTT.Core.Extensions;
using SCHOTT.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCHOTT.Core.Communication.Serial
{
    /// <summary>
    /// Base class used to create ComPort communications protocols.
    /// </summary>
    public class ComPortBase : ITextProtocol
    {

        /// <summary>
        /// MessageBroker for the ComPortBase class
        /// </summary>
        protected readonly MessageBroker MessageBroker = new MessageBroker();

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

        private readonly SerialPort _serialPort = new SerialPort();
        private readonly ComParameters _portParameters = new ComParameters();
        private readonly object _comLock = new object();
        private DateTime _lastReceiveTime = DateTime.MinValue;


        /// <summary>
        /// The connected port name, null if no port is connected.
        /// </summary>
        public string PortName => _serialPort?.PortName;

        /// <summary>
        /// Boolean on if the port is connected.
        /// </summary>
        public bool IsConnected => _serialPort?.IsOpen == true;

        /// <summary>
        /// Baud rate of the connected port. Will return 0 if no port is connected.
        /// </summary>
        public int? BaudRate => _serialPort?.BaudRate ?? 0;

        /// <summary>
        /// The date of the last byte received on this port
        /// </summary>
        public DateTime LastReceiveTime => _lastReceiveTime;

        /// <summary>
        /// Initialize a ComPort using the supplied parameters.
        /// </summary>
        /// <param name="portName">The port name to connect too.</param>
        /// <param name="portParameters">The parameters to use when setting up the ComPort</param>
        public ComPortBase(string portName, ComParameters portParameters)
        {
            _portParameters.CopyFrom(portParameters);
            _serialPort.PortName = portName;
            _serialPort.ReadTimeout = portParameters.TimeoutMilliseconds;
            _serialPort.WriteTimeout = portParameters.TimeoutMilliseconds;
            _serialPort.BaudRate = portParameters.BaudRate;
        }

        #region External Commands

        /// <summary>
        /// Port capability for high speed.
        /// </summary>
        public bool IsHighSpeed()
        {
            return _serialPort.BaudRate >= 115200;
        }

        /// <summary>
        /// Tests the connection to the unit using the default command and expected responce pair. 
        /// The function will send the ComParameters.Command to the unit, and will check for the ComParameters.ExpectedResponce.
        /// </summary>
        /// <returns>True if ExpectedResponce is received, False otherwise.</returns>
        public bool SendConnectionTest()
        {
            return SendCommandSingle(_portParameters.Command).Contains(_portParameters.ExpectedResponce);
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
        public string SendCommandSingle(string command, bool removePrefix = false, string prefixToRemove = "",
            bool echoComTraffic = false)
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
                linesToRead = _portParameters.LinesToRead;

            var linesToReturn = new List<string> {""};

            if (!Monitor.TryEnter(_comLock, _portParameters.MaxDelayMilliseconds))
                return linesToReturn;

            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    // send the data
                    var commandClone = command.CloneList();
                    commandClone.Add((byte) _portParameters.TerminationChar);

                    if (echoComTraffic)
                    {
                        RunMessageOutput(
                            $"USER> {Encoding.ASCII.GetString(commandClone.ToArray())}{Environment.NewLine}");
                    }

                    if (!_portParameters.DelaySecondCharacter || commandClone.Count == 1)
                    {
                        _serialPort.Write(commandClone.ToArray(), 0, commandClone.Count);
                    }
                    else
                    {
                        var array = commandClone.ToArray();
                        _serialPort.Write(array, 0, 1);
                        TimeFunctions.Wait(50);
                        _serialPort.Write(array, 1, commandClone.Count - 1);
                    }

                    if (linesToRead > 0)
                    {
                        linesToReturn = ReadLine(linesToRead, _portParameters.TimeoutMilliseconds);
                        if (echoComTraffic)
                        {
                            var returnStrings = string.Join(Environment.NewLine, linesToReturn.ToArray());
                            RunMessageOutput($"UNIT> {returnStrings}{Environment.NewLine}");
                        }
                    }
                }
                else
                {
                    RunMessageOutput($"SYSTEM> Open Serial Port First");
                }
            }
            catch (Exception exc)
            {
                // ignore
                var test = exc.ToString();
            }
            finally
            {
                Monitor.Exit(_comLock);
            }

            return linesToReturn;
        }

        /// <summary>
        /// Connect to the selected ComPort.
        /// </summary>
        /// <returns>True if the ComPort was opened. False otherwise.</returns>
        public bool Connect()
        {
            try
            {
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the ComPort.
        /// </summary>
        /// <returns>True if disconnect was successfull. False otherwise.</returns>
        public bool Disconnect()
        {
            try
            {
                _serialPort.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Internal Commands

        private List<string> ReadLine(int linesToRead, int timeoutMilliseconds)
        {
            var workingString = "";
            var finishBy = DateTime.Now.AddMilliseconds(timeoutMilliseconds);

            while (DateTime.Now < finishBy && _serialPort.IsOpen)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    _lastReceiveTime = DateTime.Now;

                    workingString += _serialPort.ReadExisting();

                    // special case for dealing with linux vs windows new line commands
                    workingString = workingString.Replace("\n", "");

                    // we have at least started the last requested line
                    var workingStrings = workingString.Split(new[] { _portParameters.TerminationChar }).ToList();

                    if (_portParameters.EndPrompt.Length > 0 && workingStrings.Any(s => s.Contains(_portParameters.EndPrompt)))
                    {
                        return workingStrings;
                    }

                    if (workingStrings.Count >= linesToRead)
                    {
                        // now we need to check to make sure the last line has the closing character
                        var lastLine = workingString.Substring(workingString.IndexOf(workingStrings[linesToRead - 1],
                                StringComparison.Ordinal));

                        if (lastLine.Contains(_portParameters.TerminationChar))
                        {
                            return workingStrings;
                        }
                    }
                }

                TimeFunctions.Wait(10);
            }

            // we failed so return empty
            return new List<string> {""};
        }

        #endregion

        #region Static Functions

        /// <summary>
        /// Checks to see if a ComPort responds to an given command.
        /// </summary>
        /// <param name="portName">The ComPort to check.</param>
        /// <param name="portParameters">The parameters to use when connecting to the port and checking for responce.</param>
        /// <returns>ComPort if the unit responds as expected, null otherwise.</returns>
        public static T CheckPort<T>(string portName, ComParameters portParameters) where T : ComPortBase
        {
            var serialPort = (T)Activator.CreateInstance(typeof(T), portName, portParameters);
            serialPort.Connect();

            if (serialPort.SendConnectionTest())
                return serialPort;

            serialPort.Disconnect();
            return null;
        }

        /// <summary>
        /// Function to connect to the first ComPort in the provided list that responds to the ComParameters correctly.
        /// </summary>
        /// <param name="portsToCheck">A list of ComPorts to check. Format should be "COM#".</param>
        /// <param name="portParameters">The ComParameters to use when checking each port for correct responce.</param>
        /// <returns>ComPort if the unit responds as expected, null otherwise.</returns>
        public static T AutoConnectComPort<T>(List<string> portsToCheck, ComParameters portParameters) where T : ComPortBase
        {
            if (!Monitor.TryEnter(AutoConnectingLock, portParameters.MaxDelayMilliseconds))
                return null;

            // storage variable for return port
            T connectedPort = null;

            try
            {
                // kick off all the checkport tasks
                var portCheckTasks = portsToCheck.Where(p => p != null)
                    .Select(port => Task.Factory.StartNew(() => CheckPort<T>(port, portParameters)))
                    .Cast<Task>().ToList();

                // wait allotted time for ports to connect
                Task.WaitAll(portCheckTasks.ToArray(), portParameters.TimeoutMilliseconds + portParameters.MaxDelayMilliseconds);

                // pull the first connected port out of the list
                connectedPort = portCheckTasks.Cast<Task<T>>().FirstOrDefault(s => s.Result != null)?.Result;

                // close any other ports in the list that were connected
                foreach (var task in portCheckTasks.Cast<Task<T>>().Where(s => s.Result != null && s.Result != connectedPort))
                {
                    task.Result.Disconnect();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
                // ignored
            }
            finally
            {
                Monitor.Exit(AutoConnectingLock);
            }

            // return port, will be null if no port connected
            return connectedPort;
        }
        private static readonly object AutoConnectingLock = new object();

        #endregion
    }

}
