namespace TDoubles.DataModels
{
    public enum MemberImplementationStrategy
    {
        /// <summary>
        /// Inherit from target and override the member.
        /// </summary>
        InheritAndOverride,

        /// <summary>
        /// Inherit from target and use 'new' modifier.
        /// </summary>
        InheritAndNew,

        /// <summary>
        /// Only implement interface (no inheritance).
        /// </summary>
        InterfaceOnly,

        /// <summary>
        /// Explicit interface implementation.
        /// </summary>
        ExplicitInterface
    }
}
