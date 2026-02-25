using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    /// <summary>
    /// Represents the accessibility levels for members and types.
    /// </summary>
    public enum AccessibilityLevel
    {
        /// <summary>
        /// Not applicable accessibility.
        /// </summary>
        NotApplicable = Accessibility.NotApplicable,

        /// <summary>
        /// Public accessibility - accessible from anywhere.
        /// </summary>
        Public = Accessibility.Public,

        /// <summary>
        /// Private accessibility - accessible only within the same class.
        /// </summary>
        Private = Accessibility.Private,

        /// <summary>
        /// Protected accessibility - accessible within the same class and derived classes.
        /// </summary>
        Protected = Accessibility.Protected,

        /// <summary>
        /// Internal accessibility - accessible within the same assembly.
        /// </summary>
        Internal = Accessibility.Internal,

        /// <summary>
        /// Protected internal accessibility - accessible within the same assembly or derived classes.
        /// </summary>
        ProtectedInternal = Accessibility.ProtectedOrInternal,

        /// <summary>
        /// Private protected accessibility - accessible within the same assembly and derived classes.
        /// </summary>
        PrivateProtected = Accessibility.ProtectedAndInternal
    }
}
