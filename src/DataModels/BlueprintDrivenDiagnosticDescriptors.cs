using Microsoft.CodeAnalysis;

#pragma warning disable RS2008

namespace TDoubles.DataModels
{
    internal static class BlueprintDrivenDiagnosticDescriptors
    {
        // MOCK001 - Emit on class identifier (not Mock attribute)
        public static readonly DiagnosticDescriptor MOCK001_MockClassNotPartial = new DiagnosticDescriptor(
            "MOCK001",
            "Mock class must be partial",
            "The class '{0}' decorated with [Mock] attribute must be declared as partial",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK002 - Emit on class identifier (not Mock attribute)
        public static readonly DiagnosticDescriptor MOCK002_InvalidGenericTypeArgumentCount = new DiagnosticDescriptor(
            "MOCK002",
            "Invalid generic type argument count",
            "The mock class '{0}' must have {1} generic type arguments to match target type '{2}'",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK003 - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK003_InvalidMockAttribute = new DiagnosticDescriptor(
            "MOCK003",
            "Invalid Mock attribute usage",
            "The [Mock] attribute on class '{0}' must specify a target type using typeof(TargetType)",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK004 - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK004_CircularDependency = new DiagnosticDescriptor(
            "MOCK004", 
            "Circular mock dependency detected",
            "Circular dependency detected in mock generation for type '{0}'. Mock types cannot reference each other in a circular manner.",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK005 - Alternative for generic type mapping issues - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK005_InvalidGenericTypeArguments = new DiagnosticDescriptor(
            "MOCK005",
            "Invalid generic type arguments",
            "Generic type argument count mismatch: {0}",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK006 - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK006_InvalidMockTarget = new DiagnosticDescriptor(
            "MOCK006",
            "Invalid mock target type",
            "The target type '{0}' specified in [Mock] attribute is not supported for mocking. Supported types are classes, structs, interfaces, records, and record structs.",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK007 - Generation failure - Emit on class identifier
        public static readonly DiagnosticDescriptor MOCK007_GenerationFailure = new DiagnosticDescriptor(
            "MOCK007",
            "Mock generation failure",
            "Failed to generate mock: {0}",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK008 - Warning for no overridable members - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK008_NoOverridableMembers = new DiagnosticDescriptor(
            "MOCK008",
            "Target type has no overridable members",
            "The target type '{0}' has no virtual, abstract, or interface members that can be mocked. Consider using a different target type or making members virtual.",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        // MOCK009 - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK009_RecordMockMismatch = new DiagnosticDescriptor(
            "MOCK009",
            "Record mock type mismatch",
            "The mock class for record type '{0}' must be declared as a record",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        // MOCK010 - Emit on Mock attribute syntax
        public static readonly DiagnosticDescriptor MOCK010_RecordStructMockMismatch = new DiagnosticDescriptor(
            "MOCK010",
            "Record struct mock type mismatch",
            "The mock class for record struct type '{0}' must be declared as a record struct",
            Constants.DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }

}
