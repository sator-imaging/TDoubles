using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Builds complete mock class blueprint before any code generation occurs.
    /// This replaces the previous case-by-case generation approach.
    /// </summary>
    public class BlueprintBuilder
    {
        public BlueprintBuilder()
        {
            // Constructor - no dependencies needed for current implementation
        }

        /// <summary>
        /// Builds a complete mock class blueprint with all conflicts resolved.
        /// </summary>
        public MockClassBlueprint BuildBlueprint(
            ITypeSymbol targetType,
            INamedTypeSymbol mockClass,
            bool includeInternals,
            GenericMockingMode mode,
            Compilation compilation,
            IReadOnlyList<string> excludeMemberShortNames)
        {
            var blueprint = new MockClassBlueprint()
            {
                MockClassSymbol = mockClass,
                MockNamespaceSymbol = mockClass.ContainingNamespace,
                TargetTypeSymbol = targetType,
                ExcludeMemberShortNames = excludeMemberShortNames,
                GenericMode = mode,
                IncludeInternals = includeInternals,
                IsStaticTarget = targetType.IsStatic,
                TargetTypeKind = targetType.TypeKind,
                IsRecord = targetType.IsRecord,
                IsRecordStruct = targetType.IsRecord && targetType.IsValueType
            };

            // Build the type hierarchy graph first as it's the single source of truth
            var typeGraph = TypeHierarchyGraph.Build(targetType, includeInternals, compilation);

            // 1. Build generic type mapping
            blueprint.TypeMapping = BuildGenericTypeMapping(mockClass, targetType, mode);

            // 2. Determine inheritance strategy
            blueprint.InheritanceStrategy = DetermineInheritanceStrategy(targetType, typeGraph); // Pass typeGraph

            // 3. Resolve interface implementations
            blueprint.InterfaceImplementations = ResolveInterfaceImplementations(targetType, typeGraph); // Pass typeGraph

            // 4. Extract and unify all members (now using the pre-built typeGraph)
            var allMembers = ExtractAndUnifyMembers(typeGraph); // Pass typeGraph

            // 5. Assign generic mappings to all members
            allMembers = AssignGenericMappingsToMembers(allMembers, blueprint.TypeMapping);

            // 6. Resolve all conflicts upfront
            blueprint.ConflictResolution = ResolveAllConflicts(allMembers);

            //// 7. Apply resolved names to members
            //ApplyResolvedNames(allMembers, blueprint.ConflictResolution);

            // 8. Determine implementation strategies and modifiers
            allMembers = DetermineImplementationStrategies(allMembers, blueprint.InheritanceStrategy);

            // 9. Set return value strategies
            allMembers = DetermineReturnValueStrategies(allMembers);

            // 10. Remove members based on the name
            allMembers.RemoveAll(x =>
            {
                return excludeMemberShortNames.Contains(BlueprintHelpers.GetOriginalName(x), StringComparer.Ordinal);
            });

            blueprint.Members = allMembers;

            // 10. Set base type symbol if inheriting (inherit directly from the target type)
            if (blueprint.InheritanceStrategy.ShouldInherit)
            {
                blueprint.BaseTypeSymbol = targetType;
            }

            // 11. Determine required namespaces
            blueprint.RequiredNamespaces = DetermineRequiredNamespaces(targetType, allMembers);

            return blueprint;
        }

        /// <summary>
        /// Determines the inheritance strategy based on target type analysis.
        /// </summary>
        public InheritanceStrategy DetermineInheritanceStrategy(ITypeSymbol targetType, TypeHierarchyGraph typeGraph)
        {
            var strategy = new InheritanceStrategy();

            // Use the single-source-of-truth method from InheritanceAnalyzer
            strategy.ShouldInherit = InheritanceAnalyzer.ShouldInheritFromTarget(targetType);

            if (!strategy.ShouldInherit)
            {
                strategy.HasOverridableMembers = false;
                return strategy;
            }

            // Collect overridable members from the pre-built typeGraph.ResolvedMembers
            // These are already unified and resolved for conflicts
            var overridableSymbols = new List<ISymbol>();
            foreach (var resolvedMember in typeGraph.ResolvedMembers.Values)
            {
                if (resolvedMember.Member is IMethodSymbol method && (method.IsVirtual || method.IsAbstract || method.IsOverride))
                {
                    overridableSymbols.Add(method);
                }
                else if (resolvedMember.Member is IPropertySymbol property && (property.IsVirtual || property.IsAbstract || property.IsOverride))
                {
                    overridableSymbols.Add(property);
                }
                else if (resolvedMember.Member is IEventSymbol eventSymbol && (eventSymbol.IsVirtual || eventSymbol.IsAbstract || eventSymbol.IsOverride))
                {
                    overridableSymbols.Add(eventSymbol);
                }
            }

            strategy.HasOverridableMembers = overridableSymbols.Count > 0;
            strategy.OverridableSymbols = overridableSymbols;

            return strategy;
        }

        /// <summary>
        /// Resolves all interface implementations required for the mock.
        /// All interfaces must be implemented on the mock class.
        /// </summary>
        /// <param name="targetType">The target type for which to resolve interface implementations.</param>
        /// <param name="typeGraph">The type hierarchy graph.</param>
        /// <returns>A list of InterfaceImplementationBlueprint objects.</returns>
        public List<InterfaceImplementationBlueprint> ResolveInterfaceImplementations(ITypeSymbol targetType, TypeHierarchyGraph typeGraph)
        {
            // All interfaces are already collected and unified in typeGraph.AllInterfaces
            // No need to re-collect from targetType.AllInterfaces or base types
            var finalInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            // // If the target itself is an interface, ensure it's included if not already
            // // (TypeHierarchyGraph.CollectAllInterfaces should handle this, but as a safeguard)
            // if (targetType is INamedTypeSymbol named && targetType.TypeKind == TypeKind.Interface)
            // {
            //     finalInterfaces.Add(named);
            // }

            foreach (var iface in typeGraph.AllInterfaces)
            {
                finalInterfaces.Add(iface);
            }

            return finalInterfaces.Select(iface => new InterfaceImplementationBlueprint
            {
                InterfaceType = iface,
                RequiresExplicitImplementation = DetermineExplicitImplementationNeed(iface, targetType)
            }).ToList();
        }

        /// <summary>
        /// Extracts and unifies all members from the target type hierarchy.
        /// </summary>
        private List<UnifiedMemberBlueprint> ExtractAndUnifyMembers(TypeHierarchyGraph typeGraph)
        {
            // TypeHierarchyGraph is now built once at the beginning of BuildBlueprint
            // and passed in. We directly use its ResolvedMembers.
            var members = new List<UnifiedMemberBlueprint>();

            // Convert resolved members from TypeHierarchyGraph to UnifiedMemberBlueprint
            foreach (var resolvedMember in typeGraph.ResolvedMembers.Values)
            {
                if (resolvedMember.IsSealed)
                {
                    continue;
                }

                var unifiedMember = ConvertToUnifiedMember(resolvedMember, resolvedMember.DeclaringType);
                if (unifiedMember != null)
                {
                    // Set additional information from the resolved member
                    unifiedMember.IsExplicitInterfaceImplementation = resolvedMember.IsExplicitInterfaceImplementation;
                    if (resolvedMember.InterfaceType != null)
                    {
                        unifiedMember.ExplicitInterfaceType = resolvedMember.InterfaceType;
                    }

                    members.Add(unifiedMember);
                }
            }

            // Remove duplicates (this is MockBlueprintBuilder's responsibility)
            members = RemoveDuplicateMembers(members);

            return members;
        }

        /// <summary>
        /// Assigns generic mappings to all members to ensure unified data model.
        /// For non-generic types, assigns empty mapping. For generic types, assigns the class-level mapping.
        /// This ensures all members have a GenericMapping property for consistent type handling.
        /// </summary>
        /// <param name="members">The list of members to assign mappings to.</param>
        /// <param name="classLevelMapping">The class-level generic type mapping.</param>
        /// <returns>The updated list of members with generic mappings assigned.</returns>
        private List<UnifiedMemberBlueprint> AssignGenericMappingsToMembers(List<UnifiedMemberBlueprint> members, GenericTypeMapping classLevelMapping)
        {
            foreach (var member in members)
            {
                if (classLevelMapping.ShouldApplyMapping())
                {
                    // For generic scenarios, assign the class-level mapping to each member
                    member.GenericMapping = classLevelMapping;
                }
                else
                {
                    // For non-generic scenarios, ensure empty mapping is assigned
                    // This eliminates control flow in code generation - all members have mapping
                    member.GenericMapping = new GenericTypeMapping();
                }
            }

            return members;
        }

        /// <summary>
        /// Resolves all naming conflicts upfront.
        /// </summary>
        public ConflictResolutionMap ResolveAllConflicts(List<UnifiedMemberBlueprint> members)
        {
            var conflictMap = new ConflictResolutionMap();
            var nameGroups = members.GroupBy(m => BlueprintHelpers.GetOriginalName(m)).ToList();

            foreach (var group in nameGroups)
            {
                if (group.Count() == 1)
                {
                    // No conflict - use original name
                    var member = group.First();
                    conflictMap.ResolvedNames[member] = InitializeAndPrefixResolvedName(member);
                }
                else
                {
                    // Potential conflict exists - apply optimized resolution
                    var conflictedMembers = group.ToList();
                    var resolvedNames = ResolveNamingConflictOptimized(conflictedMembers);

                    for (int i = 0; i < conflictedMembers.Count; i++)
                    {
                        var member = conflictedMembers[i];
                        var resolvedName = resolvedNames[i];
                        conflictMap.ResolvedNames[member] = resolvedName;
                    }

                    conflictMap.ConflictGroups[group.Key] = resolvedNames;
                }
            }

            return conflictMap;
        }

        /// <summary>
        /// Determines implementation strategies for all members.
        /// </summary>
        public List<UnifiedMemberBlueprint> DetermineImplementationStrategies(
            List<UnifiedMemberBlueprint> members,
            InheritanceStrategy inheritanceStrategy)
        {
            foreach (var member in members)
            {
                member.MemberModifier = DetermineMemberModifier(member, inheritanceStrategy);

                // Set implementation strategy based on modifier
                member.ImplementationStrategy = member.MemberModifier switch
                {
                    MemberModifier.Override => MemberImplementationStrategy.InheritAndOverride,
                    MemberModifier.New => MemberImplementationStrategy.InheritAndNew,
                    MemberModifier.None when member.IsExplicitInterfaceImplementation => MemberImplementationStrategy.ExplicitInterface,
                    MemberModifier.None => MemberImplementationStrategy.InterfaceOnly,
                    _ => MemberImplementationStrategy.InterfaceOnly
                };
            }

            return members;
        }

        /// <summary>
        /// Builds generic type mapping for the blueprint.
        /// Ensures all blueprints have a GenericMapping property (empty for non-generic scenarios).
        /// </summary>
        /// <param name="mockClass">The symbol of the mock class.</param>
        /// <param name="targetType">The symbol of the target type.</param>
        /// <param name="mode">The generic mocking mode.</param>
        /// <returns>A GenericTypeMapping object.</returns>
        public GenericTypeMapping BuildGenericTypeMapping(INamedTypeSymbol mockClass, ITypeSymbol targetType, GenericMockingMode mode)
        {
            var mapping = new GenericTypeMapping();

            if (mode == GenericMockingMode.UnboundGeneric && targetType is INamedTypeSymbol namedTarget)
            {
                var mockTypeParams = mockClass.TypeParameters;
                var targetTypeParams = namedTarget.TypeParameters;

                if (mockTypeParams.Length == targetTypeParams.Length && targetTypeParams.Length > 0)
                {
                    var paramMap = new Dictionary<ITypeParameterSymbol, ITypeParameterSymbol>(SymbolEqualityComparer.Default);
                    for (int i = 0; i < targetTypeParams.Length; i++)
                    {
                        paramMap[targetTypeParams[i]] = mockTypeParams[i];
                    }

                    mapping.TypeParameterSymbolMap = paramMap;
                    mapping.MockClassTypeParameterSymbols = mockTypeParams.ToList();
                    mapping.TargetTypeArgumentSymbols = targetTypeParams.Cast<ITypeSymbol>().ToList();
                }
            }
            else if (mode == GenericMockingMode.ClosedGeneric && targetType is INamedTypeSymbol concreteTarget)
            {
                // For concrete generics, we need to map from original type parameters to concrete type arguments
                var targetDefinition = concreteTarget.OriginalDefinition;
                var targetTypeParams = targetDefinition.TypeParameters;
                var concreteTypeArgs = concreteTarget.TypeArguments;

                if (targetTypeParams.Length == concreteTypeArgs.Length && targetTypeParams.Length > 0)
                {
                    // Store the original type parameters and concrete type arguments for mapping
                    mapping.OriginalTargetTypeParameters = targetTypeParams.ToList();
                    mapping.TargetTypeArgumentSymbols = concreteTypeArgs.ToList();
                    mapping.MockClassTypeParameterSymbols = mockClass.TypeParameters.ToList();

                    // Empty TypeParameterSymbolMap for concrete generics - we use OriginalTargetTypeParameters instead
                    mapping.TypeParameterSymbolMap = new Dictionary<ITypeParameterSymbol, ITypeParameterSymbol>(SymbolEqualityComparer.Default);
                }
            }
            // For NonGeneric mode, mapping remains empty but is still assigned to ensure unified data model

            return mapping;
        }

        // Private helper methods

        /// <summary>
        /// Converts a ResolvedMemberInfo object into a UnifiedMemberBlueprint.
        /// </summary>
        /// <param name="info">The ResolvedMemberInfo to convert.</param>
        /// <param name="containingType">The containing type of the member.</param>
        /// <returns>A UnifiedMemberBlueprint, or null if the member type is not supported.</returns>
        private UnifiedMemberBlueprint? ConvertToUnifiedMember(ResolvedMemberInfo info, ITypeSymbol containingType)
        {
            var bp = new UnifiedMemberBlueprint
            {
                // OriginalName removed - stored in OriginalSymbol property
                OriginalSymbol = info.Member,
                ContainingType = containingType,

                AccessibilityLevel = ConvertAccessibility(info.Member.DeclaredAccessibility),
                IsStatic = info.Member.IsStatic,
                // Initialize with empty generic mapping - will be populated later in BuildBlueprint
                GenericMapping = new GenericTypeMapping()
            };

            switch (info.Member)
            {
                case IMethodSymbol method:
                    return ConvertMethod(info, method, bp);
                case IPropertySymbol property:
                    return ConvertProperty(info, property, bp);
                case IEventSymbol eventSymbol:
                    return ConvertEvent(info, eventSymbol, bp);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts an IMethodSymbol into a UnifiedMemberBlueprint.
        /// </summary>
        /// <param name="info">The ResolvedMemberInfo associated with the method.</param>
        /// <param name="method">The IMethodSymbol to convert.</param>
        /// <param name="bp">The UnifiedMemberBlueprint to populate.</param>
        /// <returns>The populated UnifiedMemberBlueprint.</returns>
        private UnifiedMemberBlueprint ConvertMethod(ResolvedMemberInfo info, IMethodSymbol method, UnifiedMemberBlueprint bp)
        {
            bp.MemberType = MemberType.Method;
            bp.ReturnTypeSymbol = method.ReturnType;
            bp.IsVoid = method.ReturnsVoid;
            bp.IsVirtual = info.Member.IsVirtual;
            bp.IsAbstract = info.Member.IsAbstract;
            bp.CanBeOverridden = info.Member.IsVirtual || info.Member.IsAbstract || info.Member.IsOverride;
            bp.IsGeneric = method.IsGenericMethod;
            bp.IsAsync = method.IsAsync;
            bp.IsExplicitInterfaceImplementation = info.IsExplicitInterfaceImplementation;
            bp.ExplicitInterfaceType = info.InterfaceType;

            // Convert parameters - only store symbol, no strings
            bp.Parameters = method.Parameters.Select(ConvertParameterToBlueprint).ToList();

            // Handle generic type parameters - store symbols, not strings
            if (method.IsGenericMethod)
            {
                bp.GenericTypeParameterSymbols = method.TypeParameters.ToList();
                // bp.GenericConstraintSymbols = method.TypeParameters.SelectMany(tp => tp.ConstraintTypes).ToList();
            }

            // // Check for explicit interface implementation
            // if (method.ExplicitInterfaceImplementations.Length > 0)
            // {
            //     // // Update
            //     // var explicitImpl = method.ExplicitInterfaceImplementations.First();
            //     // bp.ReturnTypeSymbol = explicitImpl.ReturnType;
            //     // bp.Parameters = explicitImpl.Parameters.Select(ConvertParameterToBlueprint).ToList();
            // }

            return bp;
        }

        /// <summary>
        /// Converts an IPropertySymbol into a UnifiedMemberBlueprint.
        /// </summary>
        /// <param name="info">The ResolvedMemberInfo associated with the property.</param>
        /// <param name="property">The IPropertySymbol to convert.</param>
        /// <param name="bp">The UnifiedMemberBlueprint to populate.</param>
        /// <returns>The populated UnifiedMemberBlueprint.</returns>
        private UnifiedMemberBlueprint ConvertProperty(ResolvedMemberInfo info, IPropertySymbol property, UnifiedMemberBlueprint bp)
        {
            // Workaround for Roslyn bug: IPropertySymbol.IsIndexer is false for System.Collections.IList.Item
            // even though it is an indexer in the IList interface. This ensures it's treated as an indexer.
            if (SymbolHelpers.IsPropertyIndexer(property))
            {
                bp.MemberType = MemberType.Indexer;
                bp.Parameters = property.Parameters.Select(p => ConvertParameterToBlueprint(p)).ToList();
            }
            else
            {
                bp.MemberType = MemberType.Property;
            }

            bp.ReturnTypeSymbol = property.Type;
            bp.HasGetter = property.GetMethod != null;
            bp.HasSetter = property.SetMethod != null;
            bp.IsVirtual = info.Member.IsVirtual;
            bp.IsAbstract = info.Member.IsAbstract;
            bp.CanBeOverridden = info.Member.IsVirtual || info.Member.IsAbstract || info.Member.IsOverride;
            bp.IsExplicitInterfaceImplementation = info.IsExplicitInterfaceImplementation;
            bp.ExplicitInterfaceType = info.InterfaceType;

            // // Check for explicit interface implementation
            // if (property.ExplicitInterfaceImplementations.Length > 0)
            // {
            //     // Update
            //     var explicitImpl = property.ExplicitInterfaceImplementations.First();
            //     if (bp.MemberType is MemberType.Indexer)
            //     {
            //         bp.Parameters = explicitImpl.Parameters.Select(p => ConvertParameterToBlueprint(p)).ToList();
            //     }
            //     bp.ReturnTypeSymbol = explicitImpl.Type;
            // }

            return bp;
        }

        /// <summary>
        /// Converts an IEventSymbol into a UnifiedMemberBlueprint.
        /// </summary>
        /// <param name="info">The ResolvedMemberInfo associated with the event.</param>
        /// <param name="eventSymbol">The IEventSymbol to convert.</param>
        /// <param name="bp">The UnifiedMemberBlueprint to populate.</param>
        /// <returns>The populated UnifiedMemberBlueprint.</returns>
        private UnifiedMemberBlueprint ConvertEvent(ResolvedMemberInfo info, IEventSymbol eventSymbol, UnifiedMemberBlueprint bp)
        {
            bp.MemberType = MemberType.Event;
            bp.ReturnTypeSymbol = eventSymbol.Type;
            bp.IsVirtual = info.Member.IsVirtual;
            bp.IsAbstract = info.Member.IsAbstract;
            bp.CanBeOverridden = info.Member.IsVirtual || info.Member.IsAbstract || info.Member.IsOverride;
            bp.IsExplicitInterfaceImplementation = info.IsExplicitInterfaceImplementation;
            bp.ExplicitInterfaceType = info.InterfaceType;

            // // Check for explicit interface implementation
            // if (eventSymbol.ExplicitInterfaceImplementations.Length > 0)
            // {
            //     // Update
            //     var explicitImpl = eventSymbol.ExplicitInterfaceImplementations.First();
            //     bp.ReturnTypeSymbol = explicitImpl.Type;
            // }

            return bp;
        }

        /// <summary>
        /// Removes duplicate members from the list, prioritizing non-interface members.
        /// </summary>
        /// <param name="members">The list of UnifiedMemberBlueprint objects.</param>
        /// <returns>A new list with duplicate members removed.</returns>
        private List<UnifiedMemberBlueprint> RemoveDuplicateMembers(List<UnifiedMemberBlueprint> members)
        {
            // Group by signature and prioritize type members over interface members
            var groups = members.GroupBy(m => BlueprintHelpers.GetResolvedNameKey(m));
            var result = new HashSet<UnifiedMemberBlueprint>();

            foreach (var group in groups)
            {
                result.UnionWith(group.Where(m => m.IsExplicitInterfaceImplementation));

                // Prioritize overridable, then non-interface members
                var preferred = group.FirstOrDefault(m => m.CanBeOverridden)
                                ?? group.FirstOrDefault(m => !m.IsExplicitInterfaceImplementation &&
                                                              m.ContainingType?.TypeKind != TypeKind.Interface)
                                ?? group.FirstOrDefault(m => !m.IsExplicitInterfaceImplementation)
                                ?? group.First();

                result.Add(preferred);
            }

            return result.ToList();
        }

        /// <summary>
        /// Initializes the base name for a member and applies explicit interface implementation prefix if applicable.
        /// </summary>
        /// <param name="member">The UnifiedMemberBlueprint to process.</param>
        /// <returns>The initialized and potentially prefixed resolved name.</returns>
        private string InitializeAndPrefixResolvedName(UnifiedMemberBlueprint member)
        {
            // Step 1: Initialize base name (original name or "indexer")
            var baseName = BlueprintHelpers.GetOriginalName(member);
            if (member.MemberType == MemberType.Indexer)
            {
                baseName = Constants.IndexerName;
            }

            // Step 2: Apply explicit interface implementation prefix (if applicable)
            if (member.IsExplicitInterfaceImplementation && member.ExplicitInterfaceType != null)
            {
                // var interfaceType = member.ExplicitInterfaceType;
                // var baseInterfaceName = interfaceType.ToDisplayString(SymbolHelpers.NoNamespaceNoTypeArgsFormat);

                var baseInterfaceName = SymbolHelpers.GetExplicitInterfaceTargetName(member.OriginalSymbol);
                if (member.GenericMapping.ShouldApplyMapping())
                {
                    baseInterfaceName = member.GenericMapping.ApplyTypeMappingToString(baseInterfaceName);
                }

                // Use IndexOf as debug comment is prefixed to name
                int globalPrefixIndex = baseInterfaceName.IndexOf(Constants.GlobalNamespace, StringComparison.Ordinal);
                if (globalPrefixIndex >= 0)
                {
                    baseInterfaceName = baseInterfaceName.Substring(globalPrefixIndex + Constants.GlobalNamespace.Length);
                }

                baseInterfaceName = SymbolHelpers.GetSanitizedShortMemberName(baseInterfaceName);
                baseName = $"{baseInterfaceName}_{baseName}";
            }

            return baseName;
        }

        /// <summary>
        /// Resolves naming conflicts among a group of members with the same original name.
        /// Applies generic type suffixes and parameter type suffixes as needed.
        /// </summary>
        /// <param name="conflictedMembers">The list of UnifiedMemberBlueprint objects that have naming conflicts.</param>
        /// <returns>A list of resolved names for the conflicted members.</returns>
        private List<string> ResolveNamingConflictOptimized(List<UnifiedMemberBlueprint> conflictedMembers)
        {
            var resolvedNames = new List<string>();

            // Step 1: Initialize all names
            foreach (var member in conflictedMembers)
            {
                var baseName = InitializeAndPrefixResolvedName(member);
                resolvedNames.Add(baseName);
            }

            // Step 2: Since we're in this method, there are multiple members with the same name
            // This means there ARE conflicts that need to be resolved.

            // Apply generic type suffixes to generic methods (requirement 8.10)
            for (int i = 0; i < conflictedMembers.Count; i++)
            {
                var member = conflictedMembers[i];
                var baseName = resolvedNames[i]; // Use the potentially prefixed name

                if (member.IsGeneric && BlueprintHelpers.GetGenericTypeParameters(member).Count > 0)
                {
                    // Apply generic suffix to all generic methods in conflict
                    var genericSuffix = SymbolHelpers.GetSanitizedGenericTypeParameterSuffix(member.OriginalSymbol);
                    resolvedNames[i] = $"{baseName}{genericSuffix}";
                }
            }

            // Step 3: Check if conflicts still exist after explicit interface prefixes and generic suffixes
            if (HasRemainingConflicts(resolvedNames))
            {
                // Apply parameter type suffixes to remaining conflicts (requirement 8.11)
                for (int i = 0; i < conflictedMembers.Count; i++)
                {
                    var member = conflictedMembers[i];

                    if (member.Parameters.Count > 0)
                    {
                        var paramTypes = member.Parameters.Select(p =>
                        {
                            return SymbolHelpers.ToConflictedMemberNameSuffix(
                                BlueprintHelpers.GetParameterTypeSymbol(p) ?? throw new InvalidOperationException("TypeSymbol is null")
                            );
                        });
                        var paramSuffix = "_" + string.Join("_", paramTypes);
                        resolvedNames[i] += paramSuffix;
                    }
                    else if (i > 0)
                    {
                        // Add index suffix for parameterless methods if still conflicted
                        resolvedNames[i] += $"__{Constants.ErrorString}_{i}";
                    }
                }
            }

            return resolvedNames;
        }

        /// <summary>
        /// Checks if there are remaining conflicts in the resolved names list.
        /// </summary>
        /// <summary>
        /// Checks if there are any remaining naming conflicts in a list of resolved names.
        /// </summary>
        /// <param name="resolvedNames">The list of resolved names to check.</param>
        /// <returns>True if conflicts still exist, false otherwise.</returns>
        private bool HasRemainingConflicts(List<string> resolvedNames)
        {
            return resolvedNames.GroupBy(name => name).Any(g => g.Count() > 1);
        }

        /// <summary>
        /// Determines the return value strategy for each member based on its return type and constraints.
        /// </summary>
        /// <param name="members">The list of UnifiedMemberBlueprint objects.</param>
        /// <returns>The updated list of UnifiedMemberBlueprint objects with return strategies assigned.</returns>
        private List<UnifiedMemberBlueprint> DetermineReturnValueStrategies(List<UnifiedMemberBlueprint> members)
        {
            foreach (var member in members)
            {
                if (member.IsVoid || member.MemberType == MemberType.Event)
                {
                    member.ReturnStrategy = ReturnValueStrategy.Default;
                    continue;
                }

                // Analyze return type to determine strategy using TypeConverter
                var returnTypeSymbol = member.ReturnTypeSymbol;

                // If return type is a type parameter and we have a concrete mapping, use the mapped target type
                if (returnTypeSymbol is ITypeParameterSymbol typeParam && member.GenericMapping != null && member.GenericMapping.OriginalTargetTypeParameters != null && member.GenericMapping.TargetTypeArgumentSymbols != null)
                {
                    var list = member.GenericMapping.OriginalTargetTypeParameters;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (SymbolEqualityComparer.Default.Equals(list[i], typeParam))
                        {
                            if (i < member.GenericMapping.TargetTypeArgumentSymbols.Count)
                            {
                                returnTypeSymbol = member.GenericMapping.TargetTypeArgumentSymbols[i];
                            }
                            break;
                        }
                    }
                }

                member.ReturnStrategy = ReturnValueStrategy.ThrowException; // Default to throwing exception

                if (returnTypeSymbol != null)
                {
                    if (SymbolHelpers.IsValueType(returnTypeSymbol))
                    {
                        member.ReturnStrategy = ReturnValueStrategy.Default;
                    }
                    else if (SymbolHelpers.IsNullableReferenceType(returnTypeSymbol))
                    {
                        member.ReturnStrategy = ReturnValueStrategy.NullableDefault;
                    }
                }
                else if (HasNewConstraint(member))
                {
                    member.ReturnStrategy = ReturnValueStrategy.NewInstance;
                }
            }

            return members;
        }

        // REMOVED: ApplyGenericTypeMappingToMembers method
        // Generic type mapping is now handled at the member level through UnifiedMemberBlueprint.GenericMapping
        // This eliminates string-based type operations and ensures unified data model approach
        // Each member now has its own GenericMapping property assigned in AssignGenericMappingsToMembers

        /// <summary>
        /// Determines the correct modifier for a member based on inheritance strategy and member characteristics.
        /// </summary>
        /// <param name="member">The member to determine the modifier for.</param>
        /// <param name="targetInheritanceStrategy">The inheritance strategy for the target type.</param>
        /// <returns>The appropriate member modifier.</returns>
        /// <summary>
        /// Determines the correct modifier for a member based on inheritance strategy and member characteristics.
        /// </summary>
        /// <param name="member">The member to determine the modifier for.</param>
        /// <param name="targetInheritanceStrategy">The inheritance strategy for the target type.</param>
        /// <returns>The appropriate member modifier.</returns>
        private MemberModifier DetermineMemberModifier(UnifiedMemberBlueprint member, InheritanceStrategy targetInheritanceStrategy)
        {
            // 1. Explicit interface implementations never have modifiers
            if (member.IsExplicitInterfaceImplementation)
            {
                return MemberModifier.None;
            }

            // 2. Special type member should always be overridden
            if (member.ContainingType?.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
            {
                return MemberModifier.Override;
            }

            // 3. If mock class is NOT inheriting from target type -> no modifiers
            if (!targetInheritanceStrategy.ShouldInherit)
            {
                return MemberModifier.None;
            }

            // Check if this specific member can be overridden (i.e., it's virtual or abstract but not already an override)
            if (member.CanBeOverridden)
            {
                return MemberModifier.Override;
            }
            else // Member is non-virtual
            {
                return MemberModifier.New;
            }
        }

        /// <summary>
        /// Determines if an explicit interface implementation is needed for a given interface on the target type.
        /// </summary>
        /// <param name="iface">The interface symbol.</param>
        /// <param name="targetType">The target type symbol.</param>
        /// <returns>True if explicit implementation is needed, false otherwise.</returns>
        private bool DetermineExplicitImplementationNeed(INamedTypeSymbol iface, ITypeSymbol targetType)
        {
            // Check if target type has explicit implementations for this interface
            foreach (var member in targetType.GetMembers())
            {
                if (member is IMethodSymbol method && method.ExplicitInterfaceImplementations.Any(ei => SymbolEqualityComparer.Default.Equals(ei.ContainingType, iface)))
                {
                    return true;
                }
                if (member is IPropertySymbol property && property.ExplicitInterfaceImplementations.Any(ei => SymbolEqualityComparer.Default.Equals(ei.ContainingType, iface)))
                {
                    return true;
                }
            }
            return false;
        }

        // REMOVED: GetBaseClassName method - no longer needed since BaseClassName property was removed
        // Base type information is now stored in BaseTypeSymbol property

        /// <summary>
        /// Determines the set of namespaces required for the generated mock class.
        /// </summary>
        /// <param name="targetType">The target type symbol.</param>
        /// <param name="members">The list of UnifiedMemberBlueprint objects.</param>
        /// <returns>A HashSet of INamespaceSymbol representing the required namespaces.</returns>
        private HashSet<INamespaceSymbol> DetermineRequiredNamespaces(ITypeSymbol targetType, List<UnifiedMemberBlueprint> members)
        {
            var namespaces = new HashSet<INamespaceSymbol>(SymbolEqualityComparer.Default);

            // Add System namespace
            // Note: We can't easily get INamespaceSymbol for "System" without compilation context
            // This will be handled in TypeConverter.GetUsingDirectives() method

            // Add target type namespace
            if (targetType.ContainingNamespace != null && !targetType.ContainingNamespace.IsGlobalNamespace)
            {
                namespaces.Add(targetType.ContainingNamespace);
            }

            // Add namespaces from member types
            foreach (var member in members)
            {
                if (member.ReturnTypeSymbol?.ContainingNamespace != null && !member.ReturnTypeSymbol.ContainingNamespace.IsGlobalNamespace)
                {
                    namespaces.Add(member.ReturnTypeSymbol.ContainingNamespace);
                }

                foreach (var param in member.Parameters)
                {
                    if (param.ParameterSymbol?.Type?.ContainingNamespace != null && !param.ParameterSymbol.Type.ContainingNamespace.IsGlobalNamespace)
                    {
                        namespaces.Add(param.ParameterSymbol.Type.ContainingNamespace);
                    }
                }
            }

            return namespaces;
        }

        // Helper methods for type analysis

        /// <summary>
        /// Checks if a member's generic type parameters have a 'new()' constraint.
        /// </summary>
        /// <param name="member">The UnifiedMemberBlueprint to check.</param>
        /// <returns>True if a 'new()' constraint is present, false otherwise.</returns>
        private bool HasNewConstraint(UnifiedMemberBlueprint member)
        {
            return BlueprintHelpers.GetGenericConstraints(member)?.Any(x => x.Contains("new()")) == true;
        }

        // REMOVED: GetParameterDeclaration method - no longer needed since FullDeclaration property was removed
        // Parameter information is now accessed through ParameterSymbol property

        // REMOVED: GetGenericConstraints method - no longer needed since GenericConstraints string property was removed
        // Generic constraint information is now stored in GenericConstraintSymbols property

        /// <summary>
        /// Converts a Roslyn Accessibility enum to the custom AccessibilityLevel enum.
        /// </summary>
        /// <param name="accessibility">The Roslyn Accessibility enum value.</param>
        /// <returns>The corresponding AccessibilityLevel enum value.</returns>
        private AccessibilityLevel ConvertAccessibility(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => AccessibilityLevel.Public,
                Accessibility.Private => AccessibilityLevel.Private,
                Accessibility.Protected => AccessibilityLevel.Protected,
                Accessibility.Internal => AccessibilityLevel.Internal,
                Accessibility.ProtectedOrInternal => AccessibilityLevel.ProtectedInternal,
                Accessibility.ProtectedAndInternal => AccessibilityLevel.PrivateProtected,
                _ => AccessibilityLevel.Private
            };
        }

        /// <summary>
        /// Converts an IParameterSymbol into a ParameterBlueprint.
        /// </summary>
        /// <param name="p">The IParameterSymbol to convert.</param>
        /// <returns>A ParameterBlueprint object.</returns>
        private ParameterBlueprint ConvertParameterToBlueprint(IParameterSymbol p)
        {
            return new ParameterBlueprint
            {
                ParameterSymbol = p,
                IsRef = p.RefKind == RefKind.Ref,
                IsOut = p.RefKind == RefKind.Out,
                IsIn = p.RefKind == RefKind.In,
                IsParams = p.IsParams
            };
        }
    }
}
