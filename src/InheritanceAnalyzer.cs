using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Provides utility methods for analyzing type inheritance and member overrideability.
    /// </summary>
    public static class InheritanceAnalyzer
    {
        /// <summary>
        /// The set of System.Object virtual member names that are commonly overridden in mocks.
        /// Known System.Object virtual members: ToString(), Equals(object), GetHashCode().
        /// This is a readonly static field for performance and consistency.
        /// </summary>
        private static readonly HashSet<string> _includedSystemObjectMembers = new HashSet<string>(StringComparer.Ordinal) { "ToString", "Equals", "GetHashCode" };

        /// <summary>
        /// Determines if a given member name is one of the included System.Object virtual members.
        /// </summary>
        internal static bool IsSystemObjectMemberName(string memberName)
        {
            return _includedSystemObjectMembers.Contains(memberName, StringComparer.Ordinal);
        }

        /// <summary>
        /// Determines if the target type can be inherited from based on type characteristics.
        /// This checks only the technical ability to inherit (not sealed, not static, not struct, not interface).
        /// </summary>
        private static bool CanInherit(ITypeSymbol targetType)
        {
            if (targetType == null)
                return false;

            // Can't inherit from sealed types, static types, value types, or interfaces
            return !(targetType.IsSealed || targetType.IsStatic || targetType.IsValueType || targetType.TypeKind == TypeKind.Interface);
        }

        /// <summary>
        /// Determines if the target type should be inherited based on virtual/abstract members,
        /// excluding System.Object members from the decision.
        /// This is the single-source-of-truth for inheritance decisions.
        /// </summary>
        public static bool ShouldInheritFromTarget(ITypeSymbol targetType)
        {
            if (targetType == null)
                return false;

            // Use CanInherit to check if inheritance is technically possible
            if (!CanInherit(targetType))
                return false;

            // Update: inheritable type should be inherited.
            return true;

            // // Always inherit from record
            // if (targetType.IsRecord)
            // {
            //     return true;
            // }

            // // If the target type is abstract, it must be inherited
            // if (targetType.IsAbstract)
            //     return true;

            // // Count all virtual and abstract members in the type hierarchy
            // int totalVirtualAbstractCount = CountVirtualAndOverrideAndAbstractMembers(targetType);

            // int adjustedCount = totalVirtualAbstractCount - _includedSystemObjectMembers.Count;

            // // Only inherit if there are virtual/abstract members beyond System.Object
            // return adjustedCount > 0;
        }

        /// <summary>
        /// Checks if a member is a System.Object virtual member.
        /// </summary>
        private static bool IsSystemGeneratedOverridableMember(ISymbol member)
        {
            if (member is IMethodSymbol method)
            {
                member = SymbolHelpers.GetOverriddenMethod(method);
            }

            if (member.ContainingType.SpecialType is not SpecialType.System_Object
                                                  and not SpecialType.System_ValueType)
            {
                return false;
            }

            return !member.IsStatic
                && (member.IsVirtual || member.IsAbstract || member.IsOverride);
        }

        /// <summary>
        /// Gets all System.Object virtual members for inclusion in mock implementations.
        /// Only includes the core virtual members: ToString, Equals(object), GetHashCode.
        /// </summary>
        public static IEnumerable<ISymbol> GetSystemObjectVirtualMembers(Compilation compilation, ITypeSymbol? mockTargetType)
        {
            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
            if (objectType == null)
                return Enumerable.Empty<ISymbol>();

            return GetSystemObjectVirtualMembersInternal(objectType, mockTargetType);
        }

        /// <summary>
        /// Internal helper to get System.Object virtual members, considering specific mock target type behaviors.
        /// </summary>
        /// <param name="systemObjectSymbol">The System.Object type symbol.</param>
        /// <param name="mockTargetType">The mock target type symbol (can be null).</param>
        /// <returns>An enumerable of ISymbol representing the relevant System.Object virtual members.</returns>
        private static IEnumerable<ISymbol> GetSystemObjectVirtualMembersInternal(INamedTypeSymbol systemObjectSymbol, ITypeSymbol? mockTargetType)
        {
            if (systemObjectSymbol == null)
            {
                return Enumerable.Empty<ISymbol>();
            }

            // Include System.Object virtual members because everything in C# can be cast to System.Object
            // But only include the commonly overridable ones that make sense in mocks
            return systemObjectSymbol.GetMembers()
                .Where(member => !member.IsStatic && member.IsVirtual) // Only virtual, non-static members
                // .Where(member => member.DeclaredAccessibility == Accessibility.Public) // Only public members for mocks
                // .Where(member => _includedSystemObjectMembers.Contains(member.Name, StringComparer.Ordinal))
                .Where(member =>
                {
                    // // For Equals, only include the single-parameter version
                    // if (member.Name == "Equals" && member is IMethodSymbol method)
                    // {
                    //     return method.Parameters.Length == 1
                    //         && method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
                    // }

                    // Record and RecordStruct cannot implement --> bool Equals(object?) ...sealed override?
                    if (mockTargetType?.IsRecord == true)
                    {
                        if (member is IMethodSymbol method &&
                            method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                            method.Parameters.Length == 1 &&
                            method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                            method.Name == "Equals")
                        {
                            return false;
                        }
                    }

                    return true;
                });
        }

        /// <summary>
        /// Counts all virtual, abstract, and override members in the type hierarchy of the target type
        /// except for special type members.
        /// </summary>
        /// <param name="targetType">The target type to analyze.</param>
        /// <returns>The total count of relevant members.</returns>
        public static int GetExplicitVirtualAndOverrideAndAbstractMemberCount(ITypeSymbol targetType)
        {
            var members = new HashSet<string>(StringComparer.Ordinal);
            var currentType = targetType;

            while (currentType != null)
            {
                foreach (var member in currentType.GetMembers())
                {
                    if (member.IsImplicitlyDeclared || member.IsStatic ||
                        !(member.IsVirtual || member.IsAbstract || member.IsOverride))
                    {
                        continue;
                    }

                    if (IsSystemGeneratedOverridableMember(member))
                    {
                        continue;
                    }

                    // Use signature to avoid counting overridden members multiple times
                    var signature = GetMemberSignature(member);
                    members.Add(signature);
                }

                currentType = currentType.BaseType;
            }

            return members.Count;
        }

        /// <summary>
        /// Generates a unique signature string for a given member symbol.
        /// Used to identify members uniquely across a type hierarchy.
        /// </summary>
        /// <param name="member">The member symbol.</param>
        /// <returns>A string representing the member's unique signature.</returns>
        private static string GetMemberSignature(ISymbol member)
        {
            return (member switch
            {
                IMethodSymbol method => $"M:{method.Name}({string.Join(",", method.Parameters.Select(p => p.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)))})",
                IPropertySymbol property => $"P:{property.Name}",
                IEventSymbol eventSymbol => $"E:{eventSymbol.Name}",
                _ => $"{member.Kind}:{member.Name}"
            })
            .Replace("?", string.Empty)  // Ignore nullability difference;
            ;
        }
    }
}
