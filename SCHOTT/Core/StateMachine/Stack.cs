namespace SCHOTT.Core.StateMachine
{
    /// <summary>
    /// Holds stack information for the stack machine.
    /// </summary>
    public class StackInfo
    {
        /// <summary>
        /// The name of the step.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tree indention level of the step.
        /// </summary>
        public int TreeLevel { get; set; }

        /// <summary>
        /// A link to the step's Parent.
        /// </summary>
        public StepDefinition Parent { get; set; }

        /// <summary>
        /// Pretty print to string for debug purposes.
        /// </summary>
        /// <returns>The a tostring of format: $"Name ({Name}), TreeLevel({TreeLevel}), Parent({Parent.Name})"</returns>
        public override string ToString()
        {
            return $"Name ({Name}), TreeLevel({TreeLevel}), Parent({Parent.Name})";
        }
    }

}
