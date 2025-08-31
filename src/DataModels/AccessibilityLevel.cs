namespace TDoubles.DataModels
{
    /// <summary>
    /// Represents the accessibility levels for members and types.
    /// </summary>
    public enum AccessibilityLevel
    {
        /// <summary>
        /// Public accessibility - accessible from anywhere.
        /// </summary>
        Public,

        /// <summary>
        /// Private accessibility - accessible only within the same class.
        /// </summary>
        Private,

        /// <summary>
        /// Protected accessibility - accessible within the same class and derived classes.
        /// </summary>
        Protected,

        /// <summary>
        /// Internal accessibility - accessible within the same assembly.
        /// </summary>
        Internal,

        /// <summary>
        /// Protected internal accessibility - accessible within the same assembly or derived classes.
        /// </summary>
        ProtectedInternal,

        /// <summary>
        /// Private protected accessibility - accessible within the same assembly and derived classes.
        /// </summary>
        PrivateProtected
    }
}
