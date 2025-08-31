namespace TDoubles.DataModels
{
    public enum ReturnValueStrategy
    {
        /// <summary>
        /// Return default value.
        /// </summary>
        Default,

        /// <summary>
        /// Throw TDoublesException.
        /// </summary>
        ThrowException,

        /// <summary>
        /// Return new instance (for types with new() constraint).
        /// </summary>
        NewInstance,

        /// <summary>
        /// Return default for nullable types.
        /// </summary>
        NullableDefault
    }
}
