using SCHOTT.Core.Extensions;
using SCHOTT.Core.StateMachine;
using SCHOTT.Core.Threading;
using SCHOTT.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SCHOTT.Core.Communication.Serial
{
    /// <summary>
    /// Class using the SCHOTT ThreadedBase class to continuously try to connect to a ComPort using a selection criteria. 
    /// This type of ComPort is designed to exist for the entire program. If you need to create and destroy ComPorts multiple times,
    /// you should use the standard SCHOTT.Core.Communication.Serial.ComPortBase.
    /// </summary>
    public class ThreadedComPortBase : ThreadedStateMachine, ITextProtocol
    {
        /// <summary>
        /// The MessageBroker for the ThreadedComPortBase
        /// </summary>
        protected readonly MessageBroker MessageBroker = new MessageBroker();

        /// <summary>
        /// The name of this thread.
        /// </summary>
        protected readonly string _threadName;

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
        /// Class with information on connection status. Used when registering for status updates in the Async Socket.
        /// </summary>
        public class ConnectionUpdateArgs
        {
            /// <summary>
            /// Bool for if the ComPort is open.
            /// </summary>
            public bool IsOpen { get; set; }

            /// <summary>
            /// Bool for if a unit is responding on the connected ComPort.
            /// </summary>
            public bool IsConnected { get; set; }

            /// <summary>
            /// The connected Port.
            /// </summary>
            public string Port { get; set; }

            /// <summary>
            /// Create a ConnectionUpdateArgs object.
            /// </summary>
            public ConnectionUpdateArgs()
            {
                IsOpen = false;
                IsConnected = false;
                Port = "";
            }

            /// <summary>
            /// Create a ConnectionUpdateArgs object with starting parameters.
            /// </summary>
            /// <param name="isOpen">Bool for if the ComPort is open.</param>
            /// <param name="isConnected">Bool for if a unit is responding on the connected ComPort.</param>
            /// <param name="port">The connected Port.</param>
            public ConnectionUpdateArgs(bool isOpen, bool isConnected, string port)
            {
                IsOpen = isOpen;
                IsConnected = isConnected;
                Port = port;
            }

            /// <summary>
            /// Function to test for equality between two ConnectionUpdateArgs objects.
            /// </summary>
            /// <param name="newArgs">ConnectionUpdateArgs object to compare against.</param>
            /// <returns>True = Objects are equal, False = Objects differ</returns>
            public bool IsEqual(ConnectionUpdateArgs newArgs)
            {
                return IsConnected == newArgs.IsConnected &&
                       IsOpen == newArgs.IsOpen &&
                       Port == newArgs.Port;
            }
        }

        /// <summary>
        /// Allows the user to register for Connection Updates on the socket.
        /// </summary>
        /// <param name="context">Allows the user to specify how the update should arrive for syncing with GUI applications.</param>
        /// <param name="action">The lambda expression to execute on updates.</param>
        public void RegisterConnectionUpdate(MessageBroker.MessageContext context, Action<ConnectionUpdateArgs> action)
        {
            MessageBroker.Register("ConnectionUpdate", context, action);
        }

        private ConnectionUpdateArgs _lastArgs = new ConnectionUpdateArgs();
        private void RunConnectionUpdate(ConnectionUpdateArgs args)
        {
            if (_lastArgs.IsEqual(args))
                return;

            _lastArgs = args;
            MessageBroker.RunActions("ConnectionUpdate", args);
        }

        #region Variables

        /// <summary>
        /// ConnectionMode for the ThreadedComPortBase
        /// </summary>
        public enum ConnectionMode
        {
            /// <summary>
            /// Connect to any unit that responds properly.
            /// </summary>
            AnyCom,

            /// <summary>
            /// Use the SelectionRule provided to test specific ComPorts.
            /// </summary>
            SelectionRule,

            /// <summary>
            /// Used when the ComPort will always be there, and only checks for valid unit responces.
            /// </summary>
            PersistentPort
        }

        /// <summary>
        /// The currently connected ComPort
        /// </summary>
        public virtual ComPortBase CurrentConnection { get; private set; }

        private ConnectionMode TargetMode { get; set; } = ConnectionMode.AnyCom;
        private Func<ComPortInfo, bool> TargetSelectionRule { get; set; }

        /// <summary>
        /// The connection parameters that will be used when connecting to a new ComPortBase
        /// </summary>
        protected ComParameters ConnectionParameters = new ComParameters();

        /// <summary>
        /// PortName of the currently connected ComPort
        /// </summary>
        public string PortName => CurrentConnection?.PortName;
        
        /// <summary>
        /// Returns if the ComPort is open
        /// </summary>
        public bool IsOpen => CurrentConnection?.IsConnected == true;

        /// <summary>
        /// Returns if the ComPort is responding the the Command/ExpectedResponce Pair.
        /// </summary>
        public bool IsConnected { get; private set; }
        
        #endregion

        /// <summary>
        /// Creates a new ThreadedComPortBase object.
        /// </summary>
        /// <param name="threadName">The threadname to report during closing and error events.</param>
        /// <param name="closingWorker">The closing worker this thread should subscribe too.</param>
        /// <param name="connectionParameters">The ComParameters to use when testing connections</param>
        /// <param name="mode">Which connection mode to use when selecting ComPorts to test.</param>
        /// <param name="selectionRule">The selectionRule to use when mode = SelectionRule.</param>
        public ThreadedComPortBase(string threadName, ClosingWorker closingWorker, ComParameters connectionParameters,
            ConnectionMode mode, Func<ComPortInfo, bool> selectionRule = null) : base(threadName)
        {
            _threadName = threadName;
            closingWorker?.AddThread(this);
            ConnectionParameters.CopyFrom(connectionParameters);

            // initialize the StateMachine
            WorkerState = new StateDefinition();
            MachineFunctions.InitializeStates(WorkerStateMachine = new StepDefinition(threadName, true, WorkerState)
            {
                #region StateMachine
                SubSteps = new List<StepDefinition>
                {
                    new StepDefinition("Searching") {Delegate = StateMachine_Searching, DelayTimeMs = 200},
                    new StepDefinition("Connection Check") {Delegate = StateMachine_ConnectionCheck, DelayTimeMs = 200},
                    new StepDefinition("Complete") {Delegate = StateMachine_Complete, DelayTimeMs = 200}
                }
                #endregion // StateMachine
            });

            // set the mode for this com port
            ChangeMode(mode, selectionRule);

            // initialize ThreadingBase
            WorkerThreads = new List<ThreadInfo>
            {
                new ThreadInfo(new Thread(Worker), "State Machine", threadName, WorkerStateMachine)
            };
        }

        #region Internal Functions
        
        /// <summary>
        /// AutoConnect function called by the ThreadedComPortBase class. A derived class can override this function
        /// to return a different derived type of ComPortBase for the connect function. This allows for extension of
        /// the ThreadedComPortBase.
        /// </summary>
        /// <param name="portsToCheck">Which ports to check for a connection.</param>
        /// <param name="portParameters">Which parameters to use when checking ports.</param>
        /// <returns></returns>
        protected virtual ComPortBase AutoConnectComPort(List<string> portsToCheck, ComParameters portParameters)
        {
            return ComPortBase.AutoConnectComPort<ComPortBase>(portsToCheck, portParameters);
        }

        /// <summary>
        /// Connect function called by the ThreadedComPortBase class. A derived class can override this function
        /// to return a different derived type of ComPortBase for the connect function. This allows for extension of
        /// the ThreadedComPortBase.
        /// </summary>
        /// <param name="port">Which ports to connect too.</param>
        /// <param name="portParameters">Which parameters to use when checking ports.</param>
        /// <returns></returns>
        protected virtual ComPortBase ConnectComPort(string port, ComParameters portParameters)
        {
            return new ComPortBase(port, portParameters);
        }

        #endregion

        #region External Functions

        /// <summary>
        /// Allows inherted classes to register events on ComPort connections 
        /// </summary>
        protected virtual void ConnectionEventRegister()
        {
            CurrentConnection?.RegisterMessageOutput(MessageBroker.MessageContext.DirectToGui, RunMessageOutput);
        }

        /// <summary>
        /// Port capability for high speed.
        /// </summary>
        public bool IsHighSpeed()
        {
            return CurrentConnection?.IsHighSpeed() == true;
        }

        /// <summary>
        /// Gets the current connection status in ConnectionUpdateArgs form.
        /// </summary>
        /// <returns></returns>
        public ConnectionUpdateArgs GetCurrentConnectionUpdate()
        {
            return new ConnectionUpdateArgs(IsOpen, IsConnected, PortName);
        }

        /// <summary>
        /// Sends a command to the unit and check for an expected responce.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="expectedResponce">The responce to test for.</param>
        /// <param name="workingLine">The string returned by the unit.</param>
        /// <param name="removePrefix">When true, workingLine will only contain the string after expectedResponce.</param>
        /// <param name="prefixToRemove">The prefix to remove from the return string, defaults to command string.</param>
        /// <param name="echoComTraffic">Com traffic will be sent to registered output functions when true.</param>
        /// <returns>True if ExpectedResponce is received, False otherwise.</returns>
        public bool SendCommandSingleTest(string command, string expectedResponce, out string workingLine,
            bool removePrefix = false, string prefixToRemove = "", bool echoComTraffic = false)
        {
            workingLine = "";
            return CurrentConnection?.SendCommandSingleTest(command, expectedResponce, out workingLine, 
                removePrefix, prefixToRemove, echoComTraffic) ?? false;
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
            return CurrentConnection?.SendCommandSingle(command, removePrefix, prefixToRemove, echoComTraffic) ?? string.Empty;
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
            return CurrentConnection?.SendCommand(command, linesToRead, echoComTraffic) ?? new List<string>();
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
            return CurrentConnection?.SendCommand(command, linesToRead, echoComTraffic) ?? new List<string>();
        }
        
        /// <summary>
        /// Change the connection mode of the ThreadedComPortBase.
        /// </summary>
        /// <param name="mode">The new mode to use for selection of the ComPorts.</param>
        /// <param name="selectionRule">The new rule to use for selecting ComPorts when mode = SelectionRule.</param>
        public void ChangeMode(ConnectionMode mode, Func<ComPortInfo, bool> selectionRule = null)
        {
            TargetSelectionRule = selectionRule;
            TargetMode = mode;
            MachineFunctions.JumpToStep("Searching", WorkerStateMachine);
        }

        #endregion

        #region StateMachine Functions (Add new steps here)

        private bool StateMachine_Searching(StepDefinition currentStep)
        {
            // get list of current system com ports
            var descriptions = ComPortInfo.GetDescriptions();

            // clear out any old connections
            if (IsOpen)
            {
                IsConnected = false;
                CurrentConnection?.Disconnect();
                RunConnectionUpdate(new ConnectionUpdateArgs(IsOpen, IsConnected, PortName));
            }

            switch (TargetMode)
            {
                case ConnectionMode.AnyCom:
                    CurrentConnection = AutoConnectComPort(
                        descriptions.Select(s => s.Port).ToList(), 
                        ConnectionParameters);
                    break;

                case ConnectionMode.SelectionRule:
                    CurrentConnection = AutoConnectComPort(
                        descriptions.Where(TargetSelectionRule).Select(s => s.Port).ToList(),
                        ConnectionParameters);
                    break;

                case ConnectionMode.PersistentPort:
                    descriptions = descriptions.Where(TargetSelectionRule).ToList();
                    if (descriptions.Count == 1)
                    {
                        CurrentConnection = ConnectComPort(
                            descriptions.Select(s => s.Port).First(),
                            ConnectionParameters);

                        CurrentConnection?.Connect();
                        if (CurrentConnection?.IsConnected != true)
                        {
                            CurrentConnection?.Disconnect();
                            CurrentConnection = null;
                        }
                    }
                    else if (descriptions.Count > 1)
                    {
                        MachineFunctions.JumpToLast(currentStep);
                        return StepReturn.JumpCommandUsed;
                    }
                    break;

                default:
                    break;
            }

            // if we didn't open a port, try again
            if (!IsOpen)
                return StepReturn.RepeatStep;
            
            // we opened a port, so update the listeners
            ConnectionEventRegister();
            RunConnectionUpdate(new ConnectionUpdateArgs(IsOpen, IsConnected, PortName));

            // move to the next step
            return StepReturn.ContinueToNext;
        }

        private bool StateMachine_ConnectionCheck(StepDefinition currentStep)
        {
            // it has been long enough, we need to test the connection
            if (CurrentConnection?.SendConnectionTest() == true)
            {
                if (IsConnected)
                    return StepReturn.RepeatStep;

                IsConnected = true;
                RunConnectionUpdate(new ConnectionUpdateArgs(IsOpen, IsConnected, PortName));
            }
            else
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    RunConnectionUpdate(new ConnectionUpdateArgs(IsOpen, IsConnected, PortName));
                }

                if (TargetMode != ConnectionMode.PersistentPort)
                {
                    currentStep.SkipDelayTime = true;
                    MachineFunctions.JumpToFirst(currentStep);
                }
                else
                {
                    // see if the port is still listed in the registry
                    if (ComPortInfo.GetDescriptions().FirstOrDefault(x => x.Port == PortName) != null)
                        return StepReturn.RepeatStep;

                    // if the port is no longer there, we know the port was removed from the system
                    currentStep.SkipDelayTime = true;
                    MachineFunctions.JumpToFirst(currentStep);
                }
            }

            // this step should never move on automatically
            return StepReturn.RepeatStep;
        }

        private bool StateMachine_Complete(StepDefinition currentStep)
        {
            // clear out any old connections
            if (IsOpen)
            {
                IsConnected = false;
                CurrentConnection?.Disconnect();
                RunConnectionUpdate(new ConnectionUpdateArgs(IsOpen, IsConnected, PortName));
            }

            if (WorkerState.ThreadClosing)
            {
                WorkerState.StateMachineComplete = true;
            }
            return StepReturn.RepeatStep;
        }

        #endregion

    }

}
