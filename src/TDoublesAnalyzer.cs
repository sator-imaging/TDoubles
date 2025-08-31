using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;
using System;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// A Roslyn diagnostic analyzer for the TDoubles source generator.
    /// This analyzer provides diagnostics for incorrect usage of the MockAttribute and related issues.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TDoublesAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Gets a collection of diagnostic descriptors for the diagnostics that this analyzer is capable of producing.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                BlueprintDrivenDiagnosticDescriptors.MOCK001_MockClassNotPartial,
                BlueprintDrivenDiagnosticDescriptors.MOCK002_InvalidGenericTypeArgumentCount,
                BlueprintDrivenDiagnosticDescriptors.MOCK003_InvalidMockAttribute,
                BlueprintDrivenDiagnosticDescriptors.MOCK004_CircularDependency,
                BlueprintDrivenDiagnosticDescriptors.MOCK005_InvalidGenericTypeArguments,
                BlueprintDrivenDiagnosticDescriptors.MOCK006_InvalidMockTarget,
                BlueprintDrivenDiagnosticDescriptors.MOCK008_NoOverridableMembers,
                BlueprintDrivenDiagnosticDescriptors.MOCK007_GenerationFailure,
                BlueprintDrivenDiagnosticDescriptors.MOCK009_RecordMockMismatch,
                BlueprintDrivenDiagnosticDescriptors.MOCK010_RecordStructMockMismatch
            );

        /// <summary>
        /// Initializes the analyzer with a given analysis context.
        /// </summary>
        /// <param name="context">The analysis context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration,
                                                                      SyntaxKind.RecordDeclaration,
                                                                      SyntaxKind.RecordStructDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclarationForCircularDependency, SyntaxKind.ClassDeclaration,
                                                                      SyntaxKind.RecordDeclaration,
                                                                      SyntaxKind.RecordStructDeclaration);
        
        }

        /// <summary>
        /// Analyzes a class declaration for TDoubles-specific diagnostics.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        
            if (!IsMockCandidate(typeDeclaration))
                return;

            ValidateMockClassStructure(context, typeDeclaration);
            ValidateGenericTypeArguments(context, typeDeclaration);
            ValidateMockTarget(context, typeDeclaration);
        }

        /// <summary>
        /// Analyzes a class declaration to detect circular dependencies in mock targets.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private void AnalyzeClassDeclarationForCircularDependency(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;

            if (!IsMockCandidate(typeDeclaration))
                return;

            var semanticModel = context.SemanticModel;
            var targetType = GetTargetType(typeDeclaration, semanticModel);

            if (targetType != null && HasCircularDependency(targetType, semanticModel))
            {
                var mockAttributeLocation = GetMockAttributeLocation(typeDeclaration);
                var diagnostic = Diagnostic.Create(
                    BlueprintDrivenDiagnosticDescriptors.MOCK004_CircularDependency,
                    mockAttributeLocation,
                    targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determines if a type declaration is a candidate for mock analysis based on the presence of a Mock attribute.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax to check.</param>
        /// <returns>True if the type declaration has a Mock attribute, false otherwise.</returns>
        private bool IsMockCandidate(TypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => IsMockAttribute(attr));
        }

        /// <summary>
        /// Checks if the given attribute is a Mock attribute.
        /// </summary>
        /// <param name="attribute">The attribute syntax to check.</param>
        /// <returns>True if the attribute is a Mock attribute, false otherwise.</returns>
        private bool IsMockAttribute(AttributeSyntax attribute)
        {
            var name = attribute.Name.ToString();
            return name == "Mock" || name == "MockAttribute" || 
                   name.EndsWith(".Mock", System.StringComparison.Ordinal) || name.EndsWith(".MockAttribute", System.StringComparison.Ordinal);
        }
        
        /// <summary>
        /// Validates the structure of the mock class, ensuring it is partial.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        /// <param name="typeDeclaration">The type declaration syntax of the mock class.</param>
        private void ValidateMockClassStructure(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDeclaration)
        {
            // Validate that this is a partial class (required for new breaking changes)
            if (!typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                // Emit MOCK001 error diagnostic on class identifier (not Mock attribute)
                var diagnostic = Diagnostic.Create(
                    BlueprintDrivenDiagnosticDescriptors.MOCK001_MockClassNotPartial,
                    typeDeclaration.Identifier.GetLocation(),
                    typeDeclaration.Identifier.ValueText);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Validates the generic type arguments of the mock class against the target type.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        /// <param name="typeDeclaration">The type declaration syntax of the mock class.</param>
        private void ValidateGenericTypeArguments(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDeclaration)
        {
            var semanticModel = context.SemanticModel;
            var targetType = GetTargetType(typeDeclaration, semanticModel);
        
            if (targetType == null)
            {
                // Emit MOCK004 error diagnostic on Mock attribute (invalid Mock attribute usage)
                var mockAttributeLocation = GetMockAttributeLocation(typeDeclaration);
                var diagnostic = Diagnostic.Create(
                    BlueprintDrivenDiagnosticDescriptors.MOCK003_InvalidMockAttribute,
                    mockAttributeLocation,
                    typeDeclaration.Identifier.ValueText);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            var mode = DetermineGenericMockingMode(typeDeclaration);
            var mockClassTypeParameters = typeDeclaration.TypeParameterList?.Parameters.Count ?? 0;
            //var targetTypeParameters = targetType is INamedTypeSymbol namedType ? namedType.TypeParameters.Length : 0;
        
            if (!ValidateGenericTypeArgumentsInternal(
                targetType, 
                mockClassTypeParameters, 
                mode, 
                typeDeclaration.Identifier.ValueText,
                typeDeclaration.Identifier.GetLocation(),
                context))
            {
                return;
            }
        }

        /// <summary>
        /// Validates the mock target type, including its kind and record/record struct mismatches.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        /// <param name="typeDeclaration">The type declaration syntax of the mock class.</param>
        private void ValidateMockTarget(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDeclaration)
        {
            var semanticModel = context.SemanticModel;
            var targetType = GetTargetType(typeDeclaration, semanticModel);
        
            if (targetType == null)
                return; // Already handled in ValidateGenericTypeArguments

            // Validate that the target type is supported for mocking
            if (!IsValidMockTarget(targetType))
            {
                // Emit MOCK006 error diagnostic on Mock attribute (invalid mock target)
                var mockAttributeLocation = GetMockAttributeLocation(typeDeclaration);
                var diagnostic = Diagnostic.Create(
                    BlueprintDrivenDiagnosticDescriptors.MOCK006_InvalidMockTarget,
                    mockAttributeLocation,
                    targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // New logic for record/record struct mock type mismatch
            if (targetType is INamedTypeSymbol namedTargetType)
            {
                if (namedTargetType.IsRecord)
                {
                    if (namedTargetType.TypeKind == TypeKind.Class && !IsRecordDeclaration(typeDeclaration))
                    {
                        var mockAttributeLocation = GetMockAttributeLocation(typeDeclaration);
                        var diagnostic = Diagnostic.Create(
                            BlueprintDrivenDiagnosticDescriptors.MOCK009_RecordMockMismatch,
                            mockAttributeLocation,
                            targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                        context.ReportDiagnostic(diagnostic);
                    }
                    else if (namedTargetType.TypeKind == TypeKind.Struct && !IsRecordStructDeclaration(typeDeclaration))
                    {
                        var mockAttributeLocation = GetMockAttributeLocation(typeDeclaration);
                        var diagnostic = Diagnostic.Create(
                            BlueprintDrivenDiagnosticDescriptors.MOCK010_RecordStructMockMismatch,
                            mockAttributeLocation,
                            targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            // Check for MOCK008 - No overridable members warning
            if (InheritanceAnalyzer.ShouldInheritFromTarget(targetType) &&
                InheritanceAnalyzer.GetExplicitVirtualAndOverrideAndAbstractMemberCount(targetType) == 0)
            {
                // Emit MOCK008 warning diagnostic on Mock attribute for no overridable members
                var mockAttributeLocation = GetMockAttributeLocation(typeDeclaration);
                var diagnostic = Diagnostic.Create(
                    BlueprintDrivenDiagnosticDescriptors.MOCK008_NoOverridableMembers,
                    mockAttributeLocation,
                    targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Retrieves the target type symbol from the Mock attribute's argument.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax of the mock class.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>The ITypeSymbol for the target type, or null if not found or invalid.</returns>
        private ITypeSymbol? GetTargetType(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
        {
            var mockAttribute = typeDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(attr => IsMockAttribute(attr));

            if (mockAttribute?.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeOfExpr)
            {
                var typeInfo = semanticModel.GetTypeInfo(typeOfExpr.Type);
                return typeInfo.Type;
            }

            return null;
        }

        /// <summary>
        /// Determines the generic mocking mode based on the target type specified in the Mock attribute.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax of the mock class.</param>
        /// <returns>The determined GenericMockingMode.</returns>
        private GenericMockingMode DetermineGenericMockingMode(TypeDeclarationSyntax typeDeclaration)
        {
            var mockAttribute = typeDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(attr => IsMockAttribute(attr));

            if (mockAttribute?.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeOfExpr)
            {
                return AnalyzeTypeOfExpressionForGenericMode(typeOfExpr);
            }

            return GenericMockingMode.NonGeneric;
        }

        /// <summary>
        /// Analyzes a TypeOfExpressionSyntax to determine the generic mocking mode.
        /// </summary>
        /// <param name="typeOfExpr">The TypeOfExpressionSyntax to analyze.</param>
        /// <returns>The determined GenericMockingMode.</returns>
        private GenericMockingMode AnalyzeTypeOfExpressionForGenericMode(TypeOfExpressionSyntax typeOfExpr)
        {
            switch (typeOfExpr.Type)
            {
                case GenericNameSyntax genericName:
                    // Check if it has omitted type arguments (unbound generic)
                    if (genericName.TypeArgumentList.Arguments.Any(arg => arg is OmittedTypeArgumentSyntax))
                    {
                        return GenericMockingMode.UnboundGeneric;
                    }
                    else
                    {
                        return GenericMockingMode.ClosedGeneric;
                    }

                case QualifiedNameSyntax qualifiedName:
                    return AnalyzeQualifiedNameForGenericMode(qualifiedName);

                default:
                    return GenericMockingMode.NonGeneric;
            }
        }

        /// <summary>
        /// Analyzes a QualifiedNameSyntax to determine the generic mocking mode.
        /// </summary>
        /// <param name="qualifiedName">The QualifiedNameSyntax to analyze.</param>
        /// <returns>The determined GenericMockingMode.</returns>
        private GenericMockingMode AnalyzeQualifiedNameForGenericMode(QualifiedNameSyntax qualifiedName)
        {
            if (qualifiedName.Right is GenericNameSyntax genericRight)
            {
                if (genericRight.TypeArgumentList.Arguments.Any(arg => arg is OmittedTypeArgumentSyntax))
                {
                    return GenericMockingMode.UnboundGeneric;
                }
                else
                {
                    return GenericMockingMode.ClosedGeneric;
                }
            }

            return GenericMockingMode.NonGeneric;
        }
        
        /// <summary>
        /// Internal validation logic for generic type arguments based on the mocking mode.
        /// </summary>
        /// <param name="targetType">The target type symbol.</param>
        /// <param name="mockClassTypeParameters">The number of type parameters on the mock class.</param>
        /// <param name="genericMode">The determined generic mocking mode.</param>
        /// <param name="mockClassName">The name of the mock class.</param>
        /// <param name="classLocation">The location of the mock class declaration.</param>
        /// <param name="context">The syntax node analysis context.</param>
        /// <returns>True if the generic type arguments are valid, false otherwise.</returns>
        private bool ValidateGenericTypeArgumentsInternal(
            ITypeSymbol targetType, 
            int mockClassTypeParameters, 
            GenericMockingMode genericMode, 
            string mockClassName,
            Location classLocation,
            SyntaxNodeAnalysisContext context)
        {
            var targetTypeParameters = targetType is INamedTypeSymbol namedType ? namedType.TypeParameters.Length : 0;
        
            switch (genericMode)
            {
                case GenericMockingMode.UnboundGeneric:
                    // For unbound generic mode: validate mock class has matching type argument count with target
                    if (mockClassTypeParameters != targetTypeParameters)
                    {
                        // Emit MOCK002 diagnostic on class identifier name (not Mock attribute)
                        var diagnostic = Diagnostic.Create(
                            BlueprintDrivenDiagnosticDescriptors.MOCK002_InvalidGenericTypeArgumentCount,
                            classLocation,
                            mockClassName,
                            targetTypeParameters,
                            targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                        context.ReportDiagnostic(diagnostic);
                        return false;
                    }
                    break;
                
                case GenericMockingMode.ClosedGeneric:
                    // For concrete generic mode: allow any number of type arguments on mock class (ignored)
                    // No validation needed - mock class type arguments are ignored
                    break;
                
                case GenericMockingMode.NonGeneric:
                    // For non-generic mode: validate that mock class doesn't have generic parameters
                    if (mockClassTypeParameters > 0)
                    {
                        // Emit MOCK002 diagnostic on class identifier name (not Mock attribute)
                        var diagnostic = Diagnostic.Create(
                            BlueprintDrivenDiagnosticDescriptors.MOCK002_InvalidGenericTypeArgumentCount,
                            classLocation,
                            mockClassName,
                            0,
                            targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                        context.ReportDiagnostic(diagnostic);
                        return false;
                    }
                    break;
            }
        
            return true;
        }

        /// <summary>
        /// Recursively checks for circular dependencies in the mock target hierarchy.
        /// </summary>
        /// <param name="targetType">The current target type to check.</param>
        /// <param name="semanticModel">The semantic model for the current compilation.</param>
        /// <param name="visitedTypes">A set of types already visited in the current dependency path to detect cycles.</param>
        /// <returns>True if a circular dependency is detected, false otherwise.</returns>
        private bool HasCircularDependency(ITypeSymbol targetType, SemanticModel semanticModel, HashSet<string>? visitedTypes = null)
        {
            visitedTypes ??= new HashSet<string>(StringComparer.Ordinal);
        
            var typeFullName = targetType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        
            // If we've already visited this type, we have a circular dependency
            if (visitedTypes.Contains(typeFullName, StringComparer.Ordinal))
            {
                return true;
            }
        
            visitedTypes.Add(typeFullName);
        
            try
            {
                // Check if this type has any Mock attributes that could create circular references
                var typeDeclarations = targetType.DeclaringSyntaxReferences;
            
                foreach (var syntaxRef in typeDeclarations)
                {
                    if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl)
                    {
                        // Check if this class has Mock attributes
                        foreach (var attributeList in typeDecl.AttributeLists)
                        {
                            foreach (var attribute in attributeList.Attributes)
                            {
                                if (IsMockAttribute(attribute))
                                {
                                    var mockTargetType = GetTargetTypeFromAttribute(attribute, semanticModel);
                                    if (mockTargetType != null)
                                    {
                                        // Recursively check for circular dependencies
                                        if (HasCircularDependency(mockTargetType, semanticModel, new HashSet<string>(visitedTypes, StringComparer.Ordinal)))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            
                return false;
            }
            finally
            {
                visitedTypes.Remove(typeFullName);
            }
        }

        /// <summary>
        /// Extracts the target type symbol from a Mock attribute syntax.
        /// </summary>
        /// <param name="attribute">The Mock attribute syntax.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>The ITypeSymbol of the target type, or null if not found.</returns>
        private ITypeSymbol? GetTargetTypeFromAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeOfExpr)
            {
                var typeInfo = semanticModel.GetTypeInfo(typeOfExpr.Type);
                return typeInfo.Type;
            }
            return null;
        }

        /// <summary>
        /// Checks if the target type is a valid type for mocking (class, struct, or interface).
        /// </summary>
        /// <param name="targetType">The ITypeSymbol of the target type.</param>
        /// <returns>True if the target type is valid for mocking, false otherwise.</returns>
        private bool IsValidMockTarget(ITypeSymbol targetType)
        {
            // Check if the target type is supported for mocking
            return targetType.TypeKind switch
            {
                TypeKind.Class => true,
                TypeKind.Struct => true,
                TypeKind.Interface => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if the given type declaration is a record class declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax to check.</param>
        /// <returns>True if it is a record class declaration, false otherwise.</returns>
        private bool IsRecordDeclaration(TypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.Kind() == SyntaxKind.RecordDeclaration;
        }

        /// <summary>
        /// Checks if the given type declaration is a record struct declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax to check.</param>
        /// <returns>True if it is a record struct declaration, false otherwise.</returns>
        private bool IsRecordStructDeclaration(TypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.Kind() == SyntaxKind.RecordStructDeclaration;
        }

        /// <summary>
        /// Gets the location of the Mock attribute on a type declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax.</param>
        /// <returns>The location of the Mock attribute, or the type declaration's location if not found.</returns>
        private Location GetMockAttributeLocation(TypeDeclarationSyntax typeDeclaration)
        {
            var mockAttribute = typeDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(attr => IsMockAttribute(attr));

            // Return the location of just the attribute name, not the entire attribute
            return mockAttribute?/*.Name*/.GetLocation() ?? typeDeclaration.GetLocation();
        }
    }
}
