using SCHOTT.Core.Threading;
using SCHOTT.Core.Utilities;
using System;
using System.Windows.Forms;

namespace SCHOTT.Core.StateMachine
{
    /// <summary>
    /// Delegate for functions to be called from the StateMachine
    /// </summary>
    /// <param name="currentStep">The current step definition will be passed into the function call.</param>
    /// <returns>The instruction for how to continue.</returns>
    public delegate bool CallFunction(StepDefinition currentStep);

    /// <summary>
    /// The command type for a step function to return.
    /// </summary>
    public static class StepReturn
    {
        // NOTE: true will continue to the next step in the machine, false will repeat the step. 
        // This class is just for easy reference in the logic flow.

        /// <summary>
        /// Continue to the next step in the StateMachine.
        /// </summary>
        public static bool ContinueToNext { get; private set; } = true;

        /// <summary>
        /// Repeat the current step in the StateMachine.
        /// </summary>
        public static bool RepeatStep { get; private set; } = false;

        /// <summary>
        /// A jump command was used.
        /// </summary>
        public static bool JumpCommandUsed { get; private set; } = false;
    }

    /// <summary>
    /// Class to define a ThreadedStateMachine
    /// </summary>
    public class ThreadedStateMachine : ThreadingBase
    {
        /// <summary>
        /// The RootState of the StateMachine.
        /// </summary>
        protected StateDefinition WorkerState;

        /// <summary>
        /// The RootStep of the StateMachine
        /// </summary>
        protected StepDefinition WorkerStateMachine;

        /// <summary>
        /// Flag to start the StateMachine.
        /// </summary>
        protected bool StartWorker { get; set; }

        /// <summary>
        /// Flag to stop the StateMachine.
        /// </summary>
        protected bool StopWorker { get; set; }

        /// <summary>
        /// Create the threaded state machine.
        /// </summary>
        /// <param name="threadName"></param>
        public ThreadedStateMachine(string threadName) : base(threadName)
        {
        }

        /// <summary>
        /// Causes a waiting worker to continue imediatly. 
        /// </summary>
        protected void PulseWorker()
        {
            WorkerStateMachine.RootThread?.WaitHandle?.Set();
        }

        /// <summary>
        /// Check to see if this is the first time entering this step. Will reset after jumps.
        /// </summary>
        /// <param name="currentStep">The step to check.</param>
        /// <returns>True if first time entering the step.</returns>
        protected bool IsFirstPass(StepDefinition currentStep)
        {
            return currentStep.FailCount == 0;
        }

        /// <summary>
        /// See if the current step has timed out.
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="currentStep"></param>
        /// <returns></returns>
        protected bool IsTimedOut(int timeoutMilliseconds, StepDefinition currentStep)
        {
            if (timeoutMilliseconds == 0)
                return false;

            var stepsBeforeTimeout = (int)Math.Round(timeoutMilliseconds / (double)currentStep.DelayTimeMs, 0);
            return currentStep.FailCount >= stepsBeforeTimeout;
        }

        /// <summary>
        /// Start the worker thread.
        /// </summary>
        /// <param name="timeoutMilliseconds">Number of milliseconds to wait for state machine to start.</param>
        /// <returns>True if state machine is started, false otherwise.</returns>
        protected virtual bool Start(int timeoutMilliseconds = 50)
        {
            // tell the state machine to start
            StartWorker = true;
            PulseWorker();

            var timeout = DateTime.Now.AddMilliseconds(timeoutMilliseconds);
            while (StartWorker == true)
            {
                // timed out waiting
                if (timeout < DateTime.Now)
                    return false;

                TimeFunctions.Wait(10);
            }

            // stop successfull
            return true;
        }

        /// <summary>
        /// Stop the worker thread.
        /// </summary>
        /// <param name="timeoutMilliseconds">Number of milliseconds to wait for state machine to Stop.</param>
        /// <returns>True if state machine is stoped, false otherwise.</returns>
        protected virtual bool Stop(int timeoutMilliseconds = 50)
        {
            // tell the state machine to stop
            StopWorker = true;
            PulseWorker();
            
            var timeout = DateTime.Now.AddMilliseconds(timeoutMilliseconds);
            while (StopWorker == true)
            {
                // timed out waiting
                if (timeout < DateTime.Now)
                    return false;

                TimeFunctions.Wait(10);
            }

            // stop successfull
            return true;
        }

        /// <summary>
        /// The worker of the state machine. This executes steps and advances the state machine.
        /// </summary>
        /// <param name="workerInfo">The ThreadInfo class to advance.</param>
        protected void Worker(object workerInfo)
        {
            while (true)
            {
                // check user flags for state machine
                if (StartWorker)
                {
                    MachineFunctions.JumpToFirst(WorkerStateMachine);
                    StartWorker = false;
                }
                if (StopWorker)
                {
                    MachineFunctions.JumpToLast(WorkerStateMachine);
                    StopWorker = false;
                }

                // see if we are waiting to close the thread
                if (((ThreadInfo)workerInfo).CloseThread)
                {
                    // set flag to exit worker when complete
                    WorkerState.ThreadClosing = true;

                    // if we have finished closing the state machine, 
                    // we can exit the while loop to close the thread
                    if (WorkerState.StateMachineComplete)
                        break;

                    // otherwise, jump to the last step
                    MachineFunctions.JumpToLast(WorkerStateMachine);
                }

                // run the state machine
                MachineFunctions.ExecuteStep(((ThreadInfo)workerInfo).WorkerStateMachine);

                // wait for the time defined in the current step
                if (WorkerState.DelayTimeMs > 0)
                {
                    ((ThreadInfo)workerInfo).WaitHandle.Reset();
                    ((ThreadInfo)workerInfo).WaitHandle.WaitOne(WorkerState.DelayTimeMs);
                }

                // make sure the GUI is staying up to date
                Application.DoEvents();
            }
        }
    }
    
}
