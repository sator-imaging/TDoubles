namespace TDoubles.DataModels
{
    /// <summary>
    /// Represents the modifier that should be applied to a member in the generated mock class.
    /// </summary>
    public enum MemberModifier
    {
        /// <summary>
        /// No modifier (interface-only implementations or explicit interface implementations).
        /// </summary>
        None,

        /// <summary>
        /// Override modifier (virtual/abstract members when inheriting from target type).
        /// </summary>
        Override,

        /// <summary>
        /// New modifier (non-virtual members when inheriting from target type).
        /// </summary>
        New,

        /// <summary>
        /// Virtual modifier (if needed for base implementations).
        /// </summary>
        Virtual
    }
}
