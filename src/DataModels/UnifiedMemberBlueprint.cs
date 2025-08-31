using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    public class UnifiedMemberBlueprint
    {
        /// <summary>
        /// Gets or sets the member type.
        /// </summary>
        public MemberType MemberType { get; set; }

        /// <summary>
        /// Gets or sets the accessibility level.
        /// </summary>
        public AccessibilityLevel AccessibilityLevel { get; set; }

        /// <summary>
        /// Gets or sets whether the member is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// The return type symbol for Roslyn-based type operations.
        /// This is the source of truth for type information.
        /// </summary>
        public ITypeSymbol? ReturnTypeSymbol { get; set; }

        /// <summary>
        /// The original symbol for this member.
        /// This is used for type checking and System.Object member detection.
        /// </summary>
        public ISymbol? OriginalSymbol { get; set; }

        /// <summary>
        /// Gets or sets the generic type mapping for this member.
        /// For non-generic types, this will be an empty mapping.
        /// This ensures all members have a unified approach to type parameter handling.
        /// </summary>
        public GenericTypeMapping GenericMapping { get; set; } = new GenericTypeMapping();

        /// <summary>
        /// Gets or sets the parameters for methods.
        /// </summary>
        public List<ParameterBlueprint> Parameters { get; set; } = new List<ParameterBlueprint>();

        /// <summary>
        /// Gets or sets the implementation strategy for this member.
        /// </summary>
        public MemberImplementationStrategy ImplementationStrategy { get; set; }

        /// <summary>
        /// Gets or sets the member modifier (new, override, virtual, etc.).
        /// </summary>
        public MemberModifier MemberModifier { get; set; } = MemberModifier.None;

        /// <summary>
        /// Gets or sets whether this is an explicit interface implementation.
        /// </summary>
        public bool IsExplicitInterfaceImplementation { get; set; }

        /// <summary>
        /// Gets or sets the interface type symbol for explicit implementations.
        /// </summary>
        public INamedTypeSymbol? ExplicitInterfaceType { get; set; }

        /// <summary>
        /// Gets or sets the containing type symbol.
        /// </summary>
        public ITypeSymbol? ContainingType { get; set; }

        /// <summary>
        /// Gets or sets whether the property has a getter (properties only).
        /// </summary>
        public bool HasGetter { get; set; }

        /// <summary>
        /// Gets or sets whether the property has a setter (properties only).
        /// </summary>
        public bool HasSetter { get; set; }

        /// <summary>
        /// Gets or sets whether the member is generic.
        /// </summary>
        public bool IsGeneric { get; set; }

        /// <summary>
        /// Gets or sets the generic type parameter symbols.
        /// </summary>
        public List<ITypeParameterSymbol> GenericTypeParameterSymbols { get; set; } = new List<ITypeParameterSymbol>();

        // /// <summary>
        // /// Gets or sets the generic constraint symbols.
        // /// </summary>
        // public List<ITypeSymbol> GenericConstraintSymbols { get; set; } = new List<ITypeSymbol>();

        /// <summary>
        /// Gets or sets the return value strategy.
        /// </summary>
        public ReturnValueStrategy ReturnStrategy { get; set; }

        /// <summary>
        /// Gets or sets whether the method returns void (methods only).
        /// </summary>
        public bool IsVoid { get; set; }

        /// <summary>
        /// Gets or sets whether the member is virtual.
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// Gets or sets whether the member is abstract.
        /// </summary>
        public bool IsAbstract { get; set; }

        /// <summary>
        /// Gets or sets whether the member can be overridden.
        /// </summary>
        public bool CanBeOverridden { get; set; }

        /// <summary>
        /// Gets or sets whether the method is async (methods only).
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Gets or sets the getter accessibility (properties only).
        /// </summary>
        public AccessibilityLevel? GetterAccessibilityLevel { get; set; }

        /// <summary>
        /// Gets or sets the setter accessibility (properties only).
        /// </summary>
        public AccessibilityLevel? SetterAccessibilityLevel { get; set; }
    }
}
