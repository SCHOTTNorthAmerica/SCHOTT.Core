using System;
using SCHOTT.Core.Extensions;
using System.Linq;

namespace SCHOTT.Core.StateMachine
{
    /// <summary>
    /// Class to provide utilities functions for the state machines.
    /// </summary>
    public static class MachineFunctions
    {
        /// <summary>
        /// Initialize all the steps and the step machine state.
        /// </summary>
        /// <param name="currentStep">The root step of the machine</param>
        /// <returns></returns>
        public static bool InitializeStates(StepDefinition currentStep)
        {
            // initialze the tree on the root step
            if (currentStep.IsRootStep)
            {
                currentStep.Root = currentStep;
                currentStep.RootState.Stack.Clear();
                currentStep.RootState.WorkingIndex = 0;
            }

            // step up the tree
            currentStep.RootState.TreeLevel++;

            // reset the current step
            currentStep.Reset();

            // loop over all substeps
            foreach (var step in currentStep.SubSteps)
            {
                // add step to root tree
                currentStep.RootState.Stack.Add(new StackInfo { Name = step.Name, TreeLevel = currentStep.RootState.TreeLevel, Parent = currentStep });

                // initalize global state information
                step.StackIndex = currentStep.RootState.WorkingIndex++;
                step.Parent = currentStep;
                step.Root = currentStep.IsRootStep ? currentStep : currentStep.Root;
                step.RootState = currentStep.RootState;

                // recursive step
                InitializeStates(step);
            }

            // step back down the tree
            currentStep.RootState.TreeLevel--;

            // check for name duplicates
            return CheckDuplicates(currentStep);
        }

        private static bool CheckDuplicates(StepDefinition currentStep)
        {
            // if we are not a root step, we are done
            if (!currentStep.IsRootStep)
                return true;

            // we are a root step, so get duplicates list
            var duplicateSteps = currentStep.RootState.Stack.FindDuplicates(p => p.Name);

            // if we have no duplicates we are done
            if (!duplicateSteps.Any())
                return true;

            // we found duplicates, so print them out for the user
            var message = "";
            foreach (var name in duplicateSteps)
            {
                if (message.Length == 0)
                {
                    message += "Duplicate name found in state machine!" + Environment.NewLine + Environment.NewLine +
                               "State Machine:" + Environment.NewLine +
                               $"{currentStep.Name}" + Environment.NewLine + Environment.NewLine +
                               "Steps:" + Environment.NewLine;
                }

                message += $"{currentStep.RootState.Stack.Count(o => o.Name == name)}x {name}\n";
            }

            throw new Exception(message);
        }

        /// <summary>
        /// Allows the user to skip the current step without completion.
        /// </summary>
        /// <param name="currentStep">The step the function is called from.</param>
        /// <returns>True = Jump Successfull, False = Jump Failed</returns>
        public static bool JumpToNext(StepDefinition currentStep)
        {
            return JumpToStep(currentStep.RootState.Stack[currentStep.StackIndex + 1].Name, currentStep);
        }

        /// <summary>
        /// Allows the user to jump to the last step in the state machine, which should be
        /// a closing function to end the test.
        /// </summary>
        /// <param name="currentStep">The step the function is called from.</param>
        /// <returns>True = Jump Successfull, False = Jump Failed</returns>
        public static bool JumpToLast(StepDefinition currentStep)
        {
            return JumpToStep(currentStep.RootState.Stack.Last().Name, currentStep);
        }

        /// <summary>
        /// Allows the user to jump to the first step in the state machine, which should be
        /// a closing function to end the test.
        /// </summary>
        /// <param name="currentStep">The step the function is called from.</param>
        /// <returns>True = Jump Successfull, False = Jump Failed</returns>
        public static bool JumpToFirst(StepDefinition currentStep)
        {
            return JumpToStep(currentStep.RootState.Stack.First().Name, currentStep);
        }

        /// <summary>
        /// Allows the user to specify a step label to jump too. This will not execute any additional steps
        /// between the current and target steps.
        /// </summary>
        /// <param name="name">The Name of the step to jump too.</param>
        /// <param name="currentStep">The step the function is called from.</param>
        /// <returns>True = Jump Successfull, False = Jump Failed</returns>
        public static bool JumpToStep(string name, StepDefinition currentStep)
        {
            InitializeStates(currentStep.Root);
            if (JumpToStepWorker(name, currentStep.Root) < 0)
            {
                throw new Exception($"State Machine Error! Unable to jump to step: {name}");
            }

            // pulse the worker
            currentStep.SkipDelayTime = true;
            currentStep.RootThread?.WaitHandle?.Set();
            return true;
        }

        /// <summary>
        /// Recursive part of JumpToStep, do not use this function except from inside JumpToStep!
        /// </summary>
        /// <param name="name">The step Name to jump too.</param>
        /// <param name="currentStep">The root step to loop over.</param>
        /// <returns></returns>
        private static int JumpToStepWorker(string name, StepDefinition currentStep)
        {
            var foundName = false;
            currentStep.SubStepIndex = 0;

            for (var i = 0; i < currentStep.SubSteps.Count; i++)
            {
                if (currentStep.SubSteps[i].Name != name && JumpToStepWorker(name, currentStep.SubSteps[i]) < 0)
                    continue;

                currentStep.SubStepIndex = i;
                foundName = true;
                break;
            }

            if (foundName)
                return currentStep.SubStepIndex;

            currentStep.SubStepIndex = currentStep.SubSteps.Count;
            return -1;
        }

        /// <summary>
        /// Execute the supplied step recursivly. This will loop over the step and all children until it finds
        /// the current state step, then execute it. User should always apply the root step to this function,
        /// and allow the state machine find the correct child to execute.
        /// </summary>
        /// <param name="currentStep">The root step of the state machine.</param>
        /// <returns>Returns if a step was executed.</returns>
        public static bool ExecuteStep(StepDefinition currentStep)
        {
            if (currentStep.SubStepIndex < currentStep.SubSteps.Count)
            {
                // execute the current substep
                if (ExecuteStep(currentStep.SubSteps[currentStep.SubStepIndex]))
                {
                    // substep is complete, so advance to the next one
                    currentStep.SubStepIndex++;
                }
            }
            else
            {
                // set delay for step completion
                currentStep.RootState.DelayTimeMs = currentStep.DelayTimeMs;

                // all substeps are complete, execute final function
                var result = currentStep.Delegate(currentStep);

                // if the user doesn't want to skip the delay, go ahead and return
                if (!currentStep.SkipDelayTime)
                    return result;

                // skip the delay, then return the results
                currentStep.SkipDelayTime = false;
                currentStep.RootState.DelayTimeMs = 0;
                return result;
            }
            return false;
        }
    }
    
}
