using System;
using SCHOTT.Core.Threading;
using System.Collections.Generic;

namespace SCHOTT.Core.StateMachine
{
    /// <summary>
    /// A class to hold information for a StateMachine step.
    /// </summary>
    public class StepDefinition
    {
        /// <summary>
        /// Step Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Bool for IsRootStep
        /// </summary>
        public bool IsRootStep { get; private set; }

        /// <summary>
        /// Link to RootState
        /// </summary>
        public StateDefinition RootState { get; set; }

        /// <summary>
        /// Current StackIndex
        /// </summary>
        public int StackIndex { get; set; }

        /// <summary>
        /// Link to the RootStep.
        /// </summary>
        public StepDefinition Root { get; set; }

        /// <summary>
        /// RootThread information
        /// </summary>
        public ThreadInfo RootThread { get; set; }

        /// <summary>
        /// Link to the ParentStep
        /// </summary>
        public StepDefinition Parent { get; set; }

        /// <summary>
        /// List of all substeps
        /// </summary>
        public List<StepDefinition> SubSteps { get; set; }

        /// <summary>
        /// The function to call when all substeps are complete
        /// </summary>
        public CallFunction Delegate { get; set; }

        /// <summary>
        /// Time to delay after current step is complete
        /// </summary>
        public int DelayTimeMs { get; set; }

        /// <summary>
        /// Bool to flag if delay should be skipped this execution.
        /// </summary>
        public bool SkipDelayTime { get; set; }

        /// <summary>
        /// A string to use when parsing steps.
        /// </summary>
        public string WorkingLine;

        /// <summary>
        /// SubStepIndex for current sub step.
        /// </summary>
        public int SubStepIndex { get; set; }

        /// <summary>
        /// How many times this step has failed.
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// Create step definition.
        /// </summary>
        /// <param name="name">Step Name, used in jumps</param>
        /// <param name="isRootStep">Flag to indicate if this should be the root step</param>
        /// <param name="rootState">Pass in the root state for reference in the StateMachine</param>
        public StepDefinition(string name, bool isRootStep = false, StateDefinition rootState = null)
        {
            Name = name;
            IsRootStep = isRootStep;

            // check root configuration error
            if (isRootStep && rootState == null)
            {
                throw new Exception("StepDefinition Missconfiguration! The root step must have a state defined in the initializer.");
            }

            RootState = rootState;
            SubSteps = new List<StepDefinition>();
            Delegate = DefaultFinalFunction;
            
            DelayTimeMs = 0;
            StackIndex = 0;

            Reset();
        }

        /// <summary>
        /// Set all values that should be set during a reset or jump.
        /// </summary>
        public void Reset()
        {
            SubStepIndex = 0;
            FailCount = 0;
        }

        /// <summary>
        /// Default function to call if one is not provided.
        /// </summary>
        /// <param name="currentStep"></param>
        /// <returns></returns>
        private static bool DefaultFinalFunction(StepDefinition currentStep)
        {
            return StepReturn.ContinueToNext;
        }
    }

}
