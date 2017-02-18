using System.Collections.Generic;

namespace SCHOTT.Core.StateMachine
{
    /// <summary>
    /// Definition of a StateMachine State
    /// </summary>
    public class StateDefinition
    {
        /// <summary>
        /// Gets a tree of the StateMachine StackInfo descriptors.
        /// </summary>
        public List<StackInfo> Stack { get; set; }

        /// <summary>
        /// The tree level of the State
        /// </summary>
        public int TreeLevel { get; set; }

        /// <summary>
        /// How long after execution of this step to wait before advancing the StateMachine.
        /// </summary>
        public int DelayTimeMs { get; set; }

        /// <summary>
        /// Current index of substeps
        /// </summary>
        public int WorkingIndex { get; set; }

        /// <summary>
        /// Bool to track if the thread is closing
        /// </summary>
        public bool ThreadClosing { get; set; }

        /// <summary>
        /// Bool to record if the statemachine is complete
        /// </summary>
        public bool StateMachineComplete { get; set; }

        /// <summary>
        /// Create a new StateDefinition
        /// </summary>
        public StateDefinition()
        {
            Stack = new List<StackInfo>();
            TreeLevel = 0;
            DelayTimeMs = 200;

            ThreadClosing = false;
            StateMachineComplete = false;
        }
    }

}
