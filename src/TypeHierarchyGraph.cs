using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Represents the complete type hierarchy graph for a given target type,
    /// including base types, interfaces, and resolved members.
    /// </summary>
    public class TypeHierarchyGraph
    {
        /// <summary>
        /// Compilation context to resolve framework types like System.Object.
        /// </summary>
        public Compilation? Compilation { get; set; }

        /// <summary>
        /// Gets or sets the target type that is being analyzed.
        /// </summary>
        public ITypeSymbol TargetType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the list of base types in the inheritance hierarchy.
        /// Ordered from most derived to least derived (excluding the target type itself).
        /// </summary>
        public List<ITypeSymbol> BaseTypes { get; set; } = new List<ITypeSymbol>();

        /// <summary>
        /// Gets or sets all interfaces implemented by the target type and its base types.
        /// Includes both directly implemented and inherited interfaces.
        /// </summary>
        public List<INamedTypeSymbol> AllInterfaces { get; set; } = new List<INamedTypeSymbol>();

        /// <summary>
        /// Gets or sets the resolved members after conflict resolution.
        /// Key is the member signature, value contains the member information and its source.
        /// Type members take priority over interface members for identical signatures.
        /// </summary>
        public Dictionary<ISymbol, ResolvedMemberInfo> ResolvedMembers { get; set; } = new Dictionary<ISymbol, ResolvedMemberInfo>(SymbolEqualityComparer.Default);
        /// <summary>
        /// Gets or sets whether internal members should be included in the hierarchy.
        /// When true, both public and internal members are included.
        /// When false, only public members are included.
        /// </summary>
        public bool IncludeInternals { get; set; }

        /// <summary>
        /// Builds the type hierarchy graph for the specified target type.
        /// Searches recursively from target type to base types including interface implementations.
        /// Prioritizes type members over interface members for identical signatures.
        /// Drops duplicate members with identical signatures (interface or later-found members).
        /// </summary>
        /// <param name="targetType">The target type to analyze.</param>
        /// <param name="includeInternals">Whether to include internal members in the hierarchy.</param>
        /// <param name="compilation">Compilation to resolve System.Object and other framework symbols.</param>
        /// <returns>A new TypeHierarchyGraph instance with the complete hierarchy.</returns>
        /// <exception cref="ArgumentNullException">Thrown when targetType is null.</exception>
        public static TypeHierarchyGraph Build(ITypeSymbol targetType, bool includeInternals, Compilation? compilation = null)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            var graph = new TypeHierarchyGraph
            {
                TargetType = targetType,
                IncludeInternals = includeInternals,
                Compilation = compilation
            };

            // Build base type hierarchy (searches recursively up the inheritance chain)
            graph.BuildBaseTypeHierarchy();

            // Collect all interfaces (includes interfaces from target type and base types)
            graph.CollectAllInterfaces();

            // Resolve member conflicts (prioritize type members over interface members)
            graph.ResolveMembers();

            // Add System.Object virtual members to ensure complete type graph
            graph.AddSystemObjectVirtualMembers();

            return graph;
        }

        /// <summary>
        /// Builds the base type hierarchy by walking up the inheritance chain.
        /// </summary>
        private void BuildBaseTypeHierarchy()
        {
            var current = TargetType.BaseType;
            while (current != null &&
                   current.SpecialType is not SpecialType.System_Object and not SpecialType.System_ValueType)
            {
                if (!IncludeInternals && current.DeclaredAccessibility != Accessibility.Public)
                {
                    break;
                }

                BaseTypes.Add(current);
                current = current.BaseType;
            }
        }

        /// <summary>
        /// Collects all interfaces implemented by the target type and its base types.
        /// Searches recursively to include all interface implementations in the hierarchy.
        /// </summary>
        private void CollectAllInterfaces()
        {
            var allTypes = new List<ITypeSymbol> { TargetType };
            allTypes.AddRange(BaseTypes);

            var interfaceSet = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            if (TargetType is INamedTypeSymbol named && named.TypeKind == TypeKind.Interface)
            {
                interfaceSet.Add(named);
            }

            // Collect interfaces from target type and all base types
            foreach (var type in allTypes)
            {
                // Use GetTypeForMemberExtraction to handle unbound generic types correctly
                var typeForInterfaceExtraction = GetTypeForMemberExtraction(type);

                // Add directly implemented interfaces
                foreach (var interfaceType in typeForInterfaceExtraction.Interfaces)
                {
                    interfaceSet.Add(interfaceType);
                }

                // Add all interfaces (including inherited ones)
                foreach (var interfaceType in typeForInterfaceExtraction.AllInterfaces)
                {
                    interfaceSet.Add(interfaceType);
                }
            }

            AllInterfaces.AddRange(
                interfaceSet
                    .Where(x => IncludeInternals || x.DeclaredAccessibility == Accessibility.Public)
                    .OrderBy(x => x.Name)
                    .ThenByDescending(x => !x.IsGenericType ? 0 : x.TypeParameters.Length)
                    .ThenByDescending(x => x.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat))
                );
        }

        /// <summary>
        /// Resolves member conflicts by prioritizing type members over interface members.
        /// Drops duplicate members with identical signatures (interface or later-found members).
        /// Type members take priority over interface members for identical signatures.
        /// </summary>
        private void ResolveMembers()
        {
            var resolvedMembersInternal = new Dictionary<ISymbol, ResolvedMemberInfo>(SymbolEqualityComparer.Default);

            // First, add all type members (target type and base types) - these have highest priority
            var allTypes = new List<ITypeSymbol>();  // TargetType accessibility must be checked

            if (IncludeInternals || TargetType.DeclaredAccessibility == Accessibility.Public)
            {
                allTypes.Add(TargetType);
            }
            allTypes.AddRange(BaseTypes);

            // Process types in order: target type first, then base types (most derived to least derived)
            foreach (var type in allTypes)
            {
                // Handle unbound generic types by creating a temporary constructed version for member extraction
                var typeForMemberExtraction = GetTypeForMemberExtraction(type);
                
                foreach (var member in typeForMemberExtraction.GetMembers())
                {
                    var isExplicitImpl = IsExplicitInterfaceImplementation(member);
                    var explicitImplType = SymbolHelpers.GetExplicitInterfaceType(member);

                    if (isExplicitImpl)
                    {
                        if (explicitImplType == null ||
                            !AllInterfaces.Contains(explicitImplType, SymbolEqualityComparer.Default))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!IncludeInternals && member.DeclaredAccessibility != Accessibility.Public)
                        {
                            continue;
                        }
                    }

                    if (ShouldIncludeMember(member))
                    {
                        // Only add if not already present (first occurrence wins for type members)
                        if (!resolvedMembersInternal.ContainsKey(member))
                        {
                            resolvedMembersInternal[member] = new ResolvedMemberInfo
                            {
                                Member = member,
                                DeclaringType = type, // Use original type, not the constructed one
                                IsFromInterface = false,
                                IsExplicitInterfaceImplementation = isExplicitImpl,
                                InterfaceType = explicitImplType,
                                Priority = 0, // Type members have highest priority
                                IsSealed = member.IsSealed // Set IsSealed property for type members
                            };
                        }
                    }
                }
            }

            // When mock target is not interface, this path will generate unnecessary duplicate members
            // Only required when mock target is interface type
            if (TargetType.TypeKind is TypeKind.Interface)
            {
                // Then, add interface members (only if not already present - type members take priority)
                foreach (var interfaceType in AllInterfaces)
                {
                    // Handle unbound generic interfaces by creating a temporary constructed version for member extraction
                    var interfaceForMemberExtraction = GetTypeForMemberExtraction(interfaceType);

                    foreach (var member in interfaceForMemberExtraction.GetMembers())
                    {
                        if (ShouldIncludeMember(member))
                        {
                            // Only add interface member if no type member with same symbol exists
                            // This ensures type members take priority over interface members
                            if (!resolvedMembersInternal.ContainsKey(member))
                            {
                                resolvedMembersInternal[member] = new ResolvedMemberInfo
                                {
                                    Member = member,
                                    DeclaringType = interfaceType, // Use original interface type, not the constructed one
                                    IsFromInterface = true,
                                    IsExplicitInterfaceImplementation = false,
                                    InterfaceType = interfaceType,
                                    Priority = 1, // Interface members have lower priority
                                    IsSealed = member.IsSealed // Set IsSealed property for type members
                                };
                            }
                            // If a type member already exists with the same symbol, 
                            // the interface member is dropped (duplicate handling)
                        }
                    }
                }

                // Resolve interface member overload conflicts (e.g., GetEnumerator())
                var overloadConflictMap = new HashSet<string>(StringComparer.Ordinal);
                foreach (var resolvedMember in resolvedMembersInternal.Values)
                {
                    if (!resolvedMember.IsFromInterface ||
                        resolvedMember.InterfaceType == null)
                    {
                        continue;
                    }

                    var conflictResolutionKey = SymbolHelpers.GetMemberConflictResolutionKey(resolvedMember.Member);

                    var noConflict = overloadConflictMap.Add(conflictResolutionKey);
                    if (!noConflict)
                    {
                        resolvedMember.IsExplicitInterfaceImplementation = true;
                    }
                }
            }

            ResolvedMembers = resolvedMembersInternal;
        }

        /// <summary>
        /// Adds System.Object virtual members to the resolved members to ensure complete type graph.
        /// This ensures that all virtual members from System.Object are included in the type hierarchy.
        /// </summary>
        private void AddSystemObjectVirtualMembers()
        {
            if (Compilation == null)
            {
                return; // No compilation context; cannot reliably resolve System.Object
            }

            // Collect System.Object member overrides
            var systemObjectOverrides = ResolvedMembers.Where(x =>
            {
                if (x.Key is not IMethodSymbol method)
                {
                    return false;
                }

                var overridden = SymbolHelpers.GetOverriddenMethod(method);

                return overridden.ContainingType.SpecialType is SpecialType.System_Object
                                                             or SpecialType.System_ValueType;
            });

            // Remove overrides IF mock target is record or record struct
            if (TargetType.IsRecord)
            {
                foreach (var objectMember in systemObjectOverrides.ToList())
                {
                    ResolvedMembers.Remove(objectMember.Key);
                }
            }

            // Then, try add fresh System.Object members
            var systemObjectMembers = InheritanceAnalyzer.GetSystemObjectVirtualMembers(Compilation, TargetType);
            foreach (var member in systemObjectMembers)
            {
                // Only add if not already present (existing members take priority).
                // ResolvedMembers uses SymbolEqualityComparer.Default for key comparison,
                // ensuring that members representing the same symbol are not duplicated.
                if (!systemObjectOverrides.Any(x => x.Key.Name == member.Name))
                {
                    ResolvedMembers[member] = new ResolvedMemberInfo
                    {
                        Member = member,
                        DeclaringType = member.ContainingType!,
                        IsFromInterface = false,
                        IsExplicitInterfaceImplementation = false,
                        InterfaceType = null,
                        Priority = 2, // System.Object members have lowest priority
                        IsSealed = member.IsSealed // Set IsSealed property for type members
                    };
                }
            }
        }

        /// <summary>
        /// Gets a type suitable for member extraction. For unbound generic types, creates a temporary
        /// constructed version using placeholder type arguments so that GetMembers() will work.
        /// </summary>
        /// <param name="type">The type to get members from.</param>
        /// <returns>A type that can be used for GetMembers() calls.</returns>
        internal ITypeSymbol GetTypeForMemberExtraction(ITypeSymbol type)
        {
            // If this is an unbound generic type (has type parameters but no type arguments),
            // we need to construct it with placeholder type arguments to extract members
            if (type is INamedTypeSymbol namedType && 
                namedType.IsGenericType && 
                namedType.IsUnboundGenericType)
            {
                // Preserve generic type parameters by using the original definition,
                // so member signatures retain type parameter symbols (e.g., A) instead of placeholders.
                return namedType.OriginalDefinition;
            }
            
            // For non-generic types or already constructed generic types, return as-is
            return type;
        }

        /// <summary>
        /// Determines whether a member should be included in the resolved members.
        /// Includes ordinary methods, explicit interface implementations, and properties.
        /// Excludes constructors, destructors, operators, and compiler-generated members.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <returns>True if the member should be included, false otherwise.</returns>
        internal bool ShouldIncludeMember(ISymbol member)
        {
            // Exclude compiler-generated members
            if (member.IsImplicitlyDeclared)
            {
                return false;
            }

            // If it's a method, apply ShouldIncludeMethod logic
            if (member is IMethodSymbol method)
            {
                return ShouldIncludeMethod(method);
            }

            // If it's a property, include it (assuming ShouldIncludeProperty always returns true for now)
            if (member is IPropertySymbol)
            {
                return ShouldIncludeProperty();
            }

            // If it's an event, include it (assuming ShouldIncludeEvent always returns true for now)
            if (member is IEventSymbol)
            {
                return ShouldIncludeEvent();
            }

            return false; // Exclude other symbol types
        }

        /// <summary>
        /// Determines whether a method should be included in the resolved members.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <returns>True if the method should be included, false otherwise.</returns>
        private bool ShouldIncludeMethod(IMethodSymbol method)
        {
            // Exclude property accessor methods (get_PropertyName, set_PropertyName)
            if (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet)
            {
                return false;
            }

            // Exclude event accessor methods (add_EventName, remove_EventName)
            if (method.MethodKind == MethodKind.EventAdd || method.MethodKind == MethodKind.EventRemove)
            {
                return false;
            }

            // Exclude indexer-related hidden methods (get_Item, set_Item, etc.)
            if (method.AssociatedSymbol is IPropertySymbol property)
            {
                // Workaround for Roslyn bug: IPropertySymbol.IsIndexer is false for System.Collections.IList.Item
                // even though it is an indexer in the IList interface. This ensures it's treated as an indexer.
                if (SymbolHelpers.IsPropertyIndexer(property))
                {
                    return false;
                }
            }

            // Exclude constructors, destructors, operators, and other special methods
            if (method.MethodKind == MethodKind.Constructor || 
                method.MethodKind == MethodKind.StaticConstructor ||
                method.MethodKind == MethodKind.Destructor)
            {
                return false;
            }

            // Include ordinary methods
            if (method.MethodKind == MethodKind.Ordinary)
            {
                return true;
            }

            // Include explicit interface implementations (after filtering out accessors and special methods)
            if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                foreach (var explicitImpl in method.ExplicitInterfaceImplementations)
                {
                    if (ShouldIncludeMethod(explicitImpl))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a property should be included in the resolved members.
        /// </summary>
        /// <returns>True if the property should be included, false otherwise.</returns>
        private static bool ShouldIncludeProperty()
        {
            // Include all properties (regular and indexers)
            return true;
        }

        /// <summary>
        /// Determines whether an event should be included in the resolved members.
        /// </summary>
        /// <returns>True if the event should be included, false otherwise.</returns>
        private static bool ShouldIncludeEvent()
        {
            // Include all events
            return true;
        }

        /// <summary>
        /// Determines whether a member is an explicit interface implementation.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <returns>True if the member is an explicit interface implementation, false otherwise.</returns>
        private static bool IsExplicitInterfaceImplementation(ISymbol member)
        {
            return SymbolHelpers.GetExplicitInterfaceType(member) != null;
        }
    }
}
