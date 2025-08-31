using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    public class ResolvedMemberInfo
    {
        /// <summary>
        /// Gets or sets the member symbol.
        /// </summary>
        public ISymbol Member { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type that declares this member.
        /// </summary>
        public ITypeSymbol DeclaringType { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether this member comes from an interface.
        /// </summary>
        public bool IsFromInterface { get; set; }

        /// <summary>
        /// Gets or sets whether this member is an explicit interface implementation.
        /// </summary>
        public bool IsExplicitInterfaceImplementation { get; set; }

        /// <summary>
        /// Gets or sets the interface type if this is an interface member.
        /// </summary>
        public INamedTypeSymbol? InterfaceType { get; set; }

        /// <summary>
        /// Gets or sets the priority of this member (lower values have higher priority).
        /// Type members have priority 0, interface members have priority 1.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets whether this member is sealed.
        /// </summary>
        public bool IsSealed { get; set; }
    }
}
