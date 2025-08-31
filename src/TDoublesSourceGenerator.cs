using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Source generator that uses blueprint-driven architecture to generate mock classes.
    /// This replaces the case-by-case generation approach with comprehensive blueprint building.
    /// </summary>
    [Generator]
    public class TDoublesSourceGenerator : IIncrementalGenerator
    {
        private readonly BlueprintBuilder _blueprintBuilder;
        private readonly BlueprintDrivenCodeGenerator _codeGenerator;

        public TDoublesSourceGenerator()
        {
            _blueprintBuilder = new BlueprintBuilder();
            _codeGenerator = new BlueprintDrivenCodeGenerator();
        }

        /// <summary>
        /// Initializes the source generator with the incremental generation context.
        /// </summary>
        /// <param name="context">The incremental generator initialization context.</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Generate MockAttribute and TDoublesException once per compilation
            context.RegisterPostInitializationOutput((context) =>
            {
                GeneratorUtilities.GenerateMockAttribute(context);
                GeneratorUtilities.GenerateTDoublesException(context);
            });

            // 2. Create an IncrementalValuesProvider for mock candidates
            var mockCandidates = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, cancellationToken) => node is TypeDeclarationSyntax typeDeclaration && IsMockCandidate(typeDeclaration),
                transform: (generatorSyntaxContext, cancellationToken) =>
                {
                    // Return the TypeDeclarationSyntax for further processing
                    return (TypeDeclarationSyntax)generatorSyntaxContext.Node;
                }
            ).Where(candidate => candidate != null); // Filter out nulls if any

            // 3. Combine candidates with compilation for semantic analysis
            var compilationAndCandidates = context.CompilationProvider.Combine(mockCandidates.Collect());

            // 4. Register source output for mock class generation
            context.RegisterSourceOutput(compilationAndCandidates, (sourceProductionContext, tuple) =>
            {
                var compilation = tuple.Left;
                var candidates = tuple.Right;

                // TODO: Eliminate compilation dependency
                // // Initialize InheritanceAnalyzer with compilation context
                // InheritanceAnalyzer.ValidateSystemObjectVirtualMemberCount(compilation);

                foreach (var candidate in candidates)
                {
                    try
                    {
                        // Get semantic model for this syntax tree
                        var semanticModel = compilation.GetSemanticModel(candidate.SyntaxTree);
                        
                        // Skip non-partial classes - analyzer will emit diagnostics
                        if (!candidate.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                        {
                            continue;
                        }

                        // Extract information from candidate
                        var mockClass = GetMockClass(candidate, sourceProductionContext, semanticModel);
                        var targetType = GetTargetType(candidate, sourceProductionContext, semanticModel);
                        var includeInternals = GetIncludeInternals(candidate, semanticModel);
                        var mode = DetermineGenericMockingMode(candidate);
                        var excludeMemberShortNames = GetExcludeMemberShortNames(candidate, semanticModel);

                        // Skip invalid mock attributes - analyzer will emit diagnostics
                        if (mockClass == null || targetType == null)
                        {
                            continue;
                        }

                        // Skip invalid mock targets - analyzer will emit diagnostics
                        if (!IsValidMockTarget(targetType))
                        {
                            continue;
                        }

                        // Build complete blueprint before any code generation
                        var blueprint = _blueprintBuilder.BuildBlueprint(
                            targetType: targetType,
                            mockClass: mockClass,
                            includeInternals: includeInternals,
                            mode: mode,
                            compilation: compilation,
                            excludeMemberShortNames: excludeMemberShortNames
                        );

                        // Skip invalid blueprint - analyzer handles validation
                        if (blueprint == null)
                            continue;

                        // Generate code from complete blueprint
                        var generatedCode = _codeGenerator.GenerateMockImplementation(blueprint);
                        var hintName = GenerateHintName(blueprint);

                        sourceProductionContext.AddSource(hintName, SourceText.From(generatedCode, Encoding.UTF8));
                    }
                    catch (System.Exception ex)
                    {
                        // Only report generation failures that are not validation issues
                        // Analyzer handles all validation diagnostics
                        var diagnostic = Diagnostic.Create(
                            BlueprintDrivenDiagnosticDescriptors.MOCK007_GenerationFailure,
                            candidate.Identifier.GetLocation(),
                            ex.Message
                        );
                        sourceProductionContext.ReportDiagnostic(diagnostic);
                    }
                }

#if DEBUG
                // Generate debug information for the compilation
                sourceProductionContext.AddSource("__DEBUG.txt", SourceText.From(
                    DebugUtilities.GenerateCompilationDebugInfo(compilation),
                    Encoding.UTF8));

                sourceProductionContext.AddSource("__DEBUG_ListT.txt", SourceText.From(
                    DebugUtilities.GenerateTypeHierarchyDebugInfo(
                        compilation.GetTypeByMetadataName("System.Collections.Generic.List`1") ?? throw new InvalidOperationException("List<T> type not found."),
                        compilation),
                    Encoding.UTF8));
#endif
            });
        }

        /// <summary>
        /// Determines if a type declaration is a candidate for mocking based on the presence of a Mock attribute.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration syntax to check.</param>
        /// <returns>True if the type declaration has a Mock attribute, false otherwise.</returns>
        private bool IsMockCandidate(TypeDeclarationSyntax typeDeclaration)
        {
            // Check if the class has any Mock attribute (even invalid ones) so we can emit diagnostics
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
        /// Retrieves the named type symbol for the mock class itself.
        /// </summary>
        /// <param name="candidate">The type declaration syntax of the mock class.</param>
        /// <param name="context">The source production context.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>The INamedTypeSymbol for the mock class, or null if not found.</returns>
        private INamedTypeSymbol? GetMockClass(TypeDeclarationSyntax candidate, SourceProductionContext context, SemanticModel semanticModel)
        {
            return semanticModel.GetDeclaredSymbol(candidate);
        }

        /// <summary>
        /// Retrieves the target type symbol from the Mock attribute's argument.
        /// </summary>
        /// <param name="candidate">The type declaration syntax of the mock class.</param>
        /// <param name="context">The source production context.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>The ITypeSymbol for the target type, or null if not found or invalid.</returns>
        private ITypeSymbol? GetTargetType(TypeDeclarationSyntax candidate, SourceProductionContext context, SemanticModel semanticModel)
        {
            var mockAttribute = candidate.AttributeLists
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
        /// Determines whether internal members should be included in the mock, based on the Mock attribute's IncludeInternals property.
        /// </summary>
        /// <param name="candidate">The type declaration syntax of the mock class.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>True if internal members should be included, false otherwise.</returns>
        private bool GetIncludeInternals(TypeDeclarationSyntax candidate, SemanticModel semanticModel)
        {
            var mockAttribute = candidate.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(attr => IsMockAttribute(attr));

            if (mockAttribute == null || mockAttribute.ArgumentList == null)
            {
                return false;
            }

            // Check for named argument: IncludeInternals = true/false
            var namedArg = mockAttribute.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "IncludeInternals");

            if (namedArg != null)
            {
                var constantValue = semanticModel.GetConstantValue(namedArg.Expression);
                if (constantValue.HasValue && constantValue.Value is bool boolValue)
                {
                    return boolValue;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves a list of member short names to be excluded from mocking, as specified in the Mock attribute.
        /// </summary>
        /// <param name="candidate">The type declaration syntax of the mock class.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of member short names to exclude.</returns>
        private ImmutableList<string> GetExcludeMemberShortNames(TypeDeclarationSyntax candidate, SemanticModel semanticModel)
        {
            var mockAttribute = candidate.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(attr => IsMockAttribute(attr));

            if (mockAttribute == null || mockAttribute.ArgumentList == null)
            {
                return ImmutableList<string>.Empty;
            }

            var excludedNames = new List<string>();

            // The first argument is TargetType (positional).
            // Subsequent positional string literals are part of ExcludeMemberShortNames (params array).
            // Named arguments are handled separately (e.g., IncludeInternals).

            foreach (var arg in mockAttribute.ArgumentList.Arguments)
            {
                // If it's a named argument, it's not part of the params array for the constructor.
                if (arg.NameEquals != null)
                {
                    continue;
                }

                // The first positional argument is TargetType. Skip it.
                var argIndex = mockAttribute.ArgumentList.Arguments.IndexOf(arg);
                if (argIndex == 0)
                {
                    continue;
                }

                // Check if it's a string literal (part of params string[])
                if (arg.Expression is LiteralExpressionSyntax literal && literal.Token.ValueText is string strValue)
                {
                    excludedNames.Add(strValue);
                }
                // Handle array creation expression for params (e.g., new string[] { "Method1" })
                else if (arg.Expression is ArrayCreationExpressionSyntax arrayCreation)
                {
                    var initializer = arrayCreation.Initializer;
                    if (initializer != null)
                    {
                        foreach (var element in initializer.Expressions)
                        {
                            var constantValue = semanticModel.GetConstantValue(element);
                            if (constantValue.HasValue && constantValue.Value is string strValueFromArray)
                            {
                                excludedNames.Add(strValueFromArray);
                            }
                        }
                    }
                }
                // Handle implicit array creation expression for params (e.g., new[] { "Method1" })
                else if (arg.Expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
                {
                    var initializer = implicitArrayCreation.Initializer;
                    if (initializer != null)
                    {
                        foreach (var element in initializer.Expressions)
                        {
                            var constantValue = semanticModel.GetConstantValue(element);
                            if (constantValue.HasValue && constantValue.Value is string strValueFromArray)
                            {
                                excludedNames.Add(strValueFromArray);
                            }
                        }
                    }
                }
            }

            return excludedNames.ToImmutableList();
        }

        /// <summary>
        /// Determines the generic mocking mode based on the target type specified in the Mock attribute.
        /// </summary>
        /// <param name="candidate">The type declaration syntax of the mock class.</param>
        /// <returns>The determined GenericMockingMode.</returns>
        private GenericMockingMode DetermineGenericMockingMode(TypeDeclarationSyntax candidate)
        {
            var mockAttribute = candidate.AttributeLists
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
                    if (genericName.TypeArgumentList.Arguments.Count == 0 || genericName.TypeArgumentList.Arguments.Any(arg => arg is OmittedTypeArgumentSyntax))
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
        /// Generates a unique hint name for the generated source file based on the mock class blueprint.
        /// </summary>
        /// <param name="blueprint">The MockClassBlueprint containing information about the mock class.</param>
        /// <returns>A unique hint name for the generated source file.</returns>
        private string GenerateHintName(MockClassBlueprint blueprint)
        {
            if (blueprint.MockClassSymbol == null)
            {
                return $"{Constants.ErrorString}.g.cs"; // Fallback
            }

            var sb = new StringBuilder();

            // 1. Append namespace (if not global)
            if (blueprint.MockClassSymbol.ContainingNamespace != null && !blueprint.MockClassSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                sb.Append(blueprint.MockClassSymbol.ContainingNamespace.ToDisplayString());
                sb.Append('.');
            }

            // 2. Collect containing types
            var containingTypes = new Stack<ITypeSymbol>();
            var currentType = blueprint.MockClassSymbol.ContainingType;
            while (currentType is INamedTypeSymbol)
            {
                containingTypes.Push(currentType);
                currentType = currentType.ContainingType;
            }

            foreach (var containingType in containingTypes)
            {
                sb.Append(SymbolHelpers.ToIdentifierString(containingType));
                sb.Append('.');
            }

            // 3. Append the mock class name
            sb.Append(SymbolHelpers.ToIdentifierString(blueprint.MockClassSymbol));

            sb.Append(".g.cs");

            return sb.ToString();
        }
    }
}
