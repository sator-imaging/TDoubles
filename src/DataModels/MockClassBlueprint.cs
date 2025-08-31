using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    public class MockClassBlueprint
    {
        /// <summary>
        /// Gets or sets the mock class symbol.
        /// </summary>
        public INamedTypeSymbol? MockClassSymbol { get; set; }

        /// <summary>
        /// Gets or sets the mock namespace symbol.
        /// </summary>
        public INamespaceSymbol? MockNamespaceSymbol { get; set; }

        /// <summary>
        /// Gets or sets the target type symbol.
        /// </summary>
        public ITypeSymbol? TargetTypeSymbol { get; set; }

        /// <summary>
        /// Gets or sets the short names of members to exclude from the generated mock.
        /// </summary>
        public IReadOnlyList<string> ExcludeMemberShortNames { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the inheritance strategy for this mock class.
        /// </summary>
        public InheritanceStrategy InheritanceStrategy { get; set; } = new InheritanceStrategy();

        /// <summary>
        /// Gets or sets the base type symbol if inheriting from target type.
        /// </summary>
        public ITypeSymbol? BaseTypeSymbol { get; set; }

        /// <summary>
        /// Gets or sets the list of interface implementations required for this mock.
        /// </summary>
        public List<InterfaceImplementationBlueprint> InterfaceImplementations { get; set; } = new List<InterfaceImplementationBlueprint>();

        /// <summary>
        /// Gets or sets the unified member blueprints for all members.
        /// </summary>
        public List<UnifiedMemberBlueprint> Members { get; set; } = new List<UnifiedMemberBlueprint>();

        /// <summary>
        /// Gets or sets the generic mocking mode.
        /// </summary>
        public GenericMockingMode GenericMode { get; set; } = GenericMockingMode.NonGeneric;

        /// <summary>
        /// Gets or sets the generic type mapping information.
        /// </summary>
        public GenericTypeMapping TypeMapping { get; set; } = new GenericTypeMapping();

        /// <summary>
        /// Gets or sets the conflict resolution mapping.
        /// </summary>
        public ConflictResolutionMap ConflictResolution { get; set; } = new ConflictResolutionMap();

        /// <summary>
        /// Gets or sets whether to include internal members.
        /// </summary>
        public bool IncludeInternals { get; set; }

        /// <summary>
        /// Gets or sets the required namespaces for the generated code.
        /// </summary>
        public HashSet<INamespaceSymbol> RequiredNamespaces { get; set; } = new HashSet<INamespaceSymbol>(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets or sets whether the target type is static.
        /// </summary>
        public bool IsStaticTarget { get; set; }

        /// <summary>
        /// Gets or sets the target type kind.
        /// </summary>
        public TypeKind TargetTypeKind { get; set; }

        /// <summary>
        /// Gets or sets whether the target is a record.
        /// </summary>
        public bool IsRecord { get; set; }

        /// <summary>
        /// Gets or sets whether the target is a record struct.
        /// </summary>
        public bool IsRecordStruct { get; set; }
    }
}
