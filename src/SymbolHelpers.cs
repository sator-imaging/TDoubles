using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Provides helper methods for working with Roslyn symbols.
    /// </summary>
    public static class SymbolHelpers
    {
        public static readonly SymbolDisplayFormat FullyQualifiedNoNullableAnnotationFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// SymbolDisplayFormat for fully qualified names including nullable reference type modifier.
        /// </summary>
        public static readonly SymbolDisplayFormat FullyQualifiedNullableFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        /// <summary>
        /// SymbolDisplayFormat for C# short error messages.
        /// </summary>
        public static readonly SymbolDisplayFormat CSharpShortErrorMessageFormat = SymbolDisplayFormat.CSharpShortErrorMessageFormat;

        /// <summary>
        /// SymbolDisplayFormat for short names including type parameters and nullable reference type modifier.
        /// </summary>
        public static readonly SymbolDisplayFormat ShortFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        /// <summary>
        /// SymbolDisplayFormat for names without namespace or type arguments.
        /// </summary>
        public static readonly SymbolDisplayFormat NoNamespaceNoTypeArgsFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.None,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// Regular expression to match the innermost generic type arguments.
        /// </summary>
        private static readonly Regex _innermostGenericRegex = new Regex(@"<([^<>]*)>", RegexOptions.Compiled | RegexOptions.RightToLeft);

        /// <summary>
        /// Gets the actual interface type for an explicitly implemented member.
        /// </summary>
        /// <param name="symbol">The member symbol (IMethodSymbol, IPropertySymbol, or IEventSymbol).</param>
        /// <returns>The INamedTypeSymbol of the interface, or null if not an explicit implementation.</returns>
        public static INamedTypeSymbol? GetExplicitInterfaceType(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                return method.ExplicitInterfaceImplementations.FirstOrDefault()?.ContainingType;
            }

            if (symbol is IPropertySymbol property)
            {
                return property.ExplicitInterfaceImplementations.FirstOrDefault()?.ContainingType;
            }

            if (symbol is IEventSymbol eventSymbol)
            {
                return eventSymbol.ExplicitInterfaceImplementations.FirstOrDefault()?.ContainingType;
            }

            return null;
        }

        /// <summary>
        /// Legacy method for nullability checking - does not require unified blueprint.
        /// Determines if a type symbol represents a nullable reference type using Roslyn's nullability information.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check.</param>
        /// <returns>True if the type is a nullable reference type, false otherwise.</returns>
        public static bool IsNullableReferenceType(ITypeSymbol typeSymbol)
        {
            return typeSymbol?.CanBeReferencedByName == true &&
                   typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        }

        /// <summary>
        /// Legacy method for type checking - does not require unified blueprint.
        /// Determines if a type symbol represents a value type using Roslyn's type information.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check.</param>
        /// <returns>True if the type is a value type, false otherwise.</returns>
        public static bool IsValueType(ITypeSymbol typeSymbol)
        {
            return typeSymbol?.IsValueType == true;
        }

        /// <summary>
        /// Legacy method for conflict resolution - does not require unified blueprint.
        /// Converts a type symbol to a valid C# identifier suffix for resolving member naming conflicts.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to convert.</param>
        /// <returns>A valid C# identifier suffix.</returns>
        public static string ToConflictedMemberNameSuffix(ITypeSymbol typeSymbol)
        {
            string resolved;

            // Handle array types
            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementIdentifier = ToConflictedMemberNameSuffix(arrayType.ElementType);
                var arraySuffix = GetArraySuffix(arrayType);
                resolved = elementIdentifier + arraySuffix;
            }

            // Handle generic types
            else if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var baseName = GetCSharpKeywordOrTypeName(namedType.ConstructedFrom);
                var typeArgs = namedType.TypeArguments.Select(ToConflictedMemberNameSuffix);
                resolved = baseName + "_" + string.Join("", typeArgs.Select(CapitalizeFirstLetter));
            }

            // Handle simple types
            else
            {
                resolved = GetCSharpKeywordOrTypeName(typeSymbol);
            }

            return resolved.Replace("?", "").Replace("<", "_").Replace(">", "_");
        }

        /// <summary>
        /// LEGACY METHOD: Converts a type symbol to its C# keyword equivalent when applicable.
        /// This method should be replaced with unified blueprint methods where possible.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to convert.</param>
        /// <returns>The C# keyword equivalent.</returns>
        public static string ToCSharpKeywordString(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return string.Empty;
            }

            return typeSymbol.ToDisplayString(CSharpShortErrorMessageFormat);
        }

        /// <summary>
        /// Legacy method for default value conversion - does not require unified blueprint.
        /// Converts a default value object to its string representation for code generation.
        /// </summary>
        /// <param name="defaultValue">The default value object to convert.</param>
        /// <param name="parameterType">The type of the parameter (for context).</param>
        /// <returns>A string representation suitable for C# code generation.</returns>
        public static string ToDefaultValueString(object? defaultValue)
        {
            if (defaultValue == null)
            {
                return "null";
            }

            // Handle string literals with proper escaping
            if (defaultValue is string stringValue)
            {
                return $"\"{stringValue.Replace("\"", "\\\"")}\"";
            }

            // Handle boolean literals (must be lowercase in C#)
            if (defaultValue is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            // Handle other types
            return defaultValue.ToString() ?? "null";
        }

        /// <summary>
        /// Converts a type symbol to a valid C# identifier string for member naming.
        /// Handles arrays, generics, and special characters to produce valid identifiers.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to convert.</param>
        /// <returns>A valid C# identifier string.</returns>
        public static string ToIdentifierString(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return "Object";
            }

            // Handle array types
            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementIdentifier = ToIdentifierString(arrayType.ElementType);
                var arraySuffix = GetArrayIdentifierSuffix(arrayType);
                return elementIdentifier + arraySuffix;
            }

            // Handle generic types
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var baseName = GetCSharpKeywordOrTypeName(namedType.ConstructedFrom);

                // Remove angle brackets if somehow present (extra safety)
                var angleBracketIndex = baseName.IndexOf('<');
                if (angleBracketIndex > 0)
                {
                    baseName = baseName.Substring(0, angleBracketIndex);
                }

                var typeArgs = namedType.TypeArguments.Select(ToIdentifierString);
                return baseName + string.Join("", typeArgs.Select(CapitalizeFirstLetter));
            }

            // Handle simple types - use C# keywords when available
            return GetCSharpKeywordOrTypeName(typeSymbol);
        }

        /// <summary>
        /// Legacy method for namespace conversion - does not require unified blueprint.
        /// Converts a namespace symbol to its string representation.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace symbol to convert.</param>
        /// <returns>The namespace string, or empty string if null.</returns>
        public static string ToNamespaceString(INamespaceSymbol? namespaceSymbol)
        {
            if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
            {
                return string.Empty;
            }

            return namespaceSymbol.ToDisplayString();
        }

        /// <summary>
        /// Workaround for Roslyn bug where IPropertySymbol.IsIndexer is false for System.Collections.IList.Item or
        /// other explicit interface implementations of indexer.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property is indexer.</returns>
        public static bool IsPropertyIndexer(IPropertySymbol property)
        {
            // Roslyn bug: IPropertySymbol.IsIndexer is false for System.Collections.IList.Item
            // even though it is an indexer in the IList interface. This is a workaround.
            // We check for the name and parameter count to identify it.
            return property.IsIndexer
                || (
                    property.Parameters.Length == 1 &&
                    property.ExplicitInterfaceImplementations.Any() &&
                    property.Name.EndsWith("." + Constants.IndexerInternalName, StringComparison.Ordinal)
                );
        }

        // Recursively detects if a type uses any method-level type parameter from the given list.
        /// <summary>
        /// Recursively detects if a type uses any method-level type parameter from the given list.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="methodTypeParams">The list of method-level type parameters.</param>
        /// <returns>True if the type uses any of the method-level type parameters, false otherwise.</returns>
        internal static bool UsesMethodLevelTypeParameter(ITypeSymbol type, IReadOnlyList<ITypeParameterSymbol> methodTypeParams)
        {
            if (type is ITypeParameterSymbol tps)
            {
                return methodTypeParams.Contains(tps, SymbolEqualityComparer.Default);
            }

            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var arg in named.TypeArguments)
                {
                    if (UsesMethodLevelTypeParameter(arg, methodTypeParams))
                        return true;
                }
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                return UsesMethodLevelTypeParameter(arrayType.ElementType, methodTypeParams);
            }

            if (type is IPointerTypeSymbol ptrType)
            {
                return UsesMethodLevelTypeParameter(ptrType.PointedAtType, methodTypeParams);
            }

            return false;
        }

        // Private helper methods

        /// <summary>
        /// Capitalizes the first letter of a string.
        /// </summary>
        private static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (input.Length == 1)
            {
                return input.ToUpper(CultureInfo.InvariantCulture);
            }

            return char.ToUpper(input[0], CultureInfo.InvariantCulture) + input.Substring(1);
        }

        /// <summary>
        /// Gets the appropriate array identifier suffix for an array type symbol.
        /// </summary>
        private static string GetArrayIdentifierSuffix(IArrayTypeSymbol arrayType)
        {
            if (arrayType.Rank == 1)
            {
                // Check if it's a jagged array by examining the element type
                if (arrayType.ElementType is IArrayTypeSymbol)
                {
                    return "Array" + GetArrayIdentifierSuffix((IArrayTypeSymbol)arrayType.ElementType);
                }
                return "Array";
            }
            else if (arrayType.Rank == 2)
            {
                return "Array2D";
            }
            else if (arrayType.Rank == 3)
            {
                return "Array3D";
            }
            else
            {
                return $"Array{arrayType.Rank}D";
            }
        }

        /// <summary>
        /// Gets the appropriate array suffix for an array type symbol.
        /// </summary>
        private static string GetArraySuffix(IArrayTypeSymbol arrayType)
        {
            if (arrayType.Rank == 1)
            {
                // Check if it's a jagged array by examining the element type
                if (arrayType.ElementType is IArrayTypeSymbol)
                {
                    return "Array" + GetArraySuffix((IArrayTypeSymbol)arrayType.ElementType);
                }
                return "Array";
            }
            else if (arrayType.Rank == 2)
            {
                return "Array2D";
            }
            else if (arrayType.Rank == 3)
            {
                return "Array3D";
            }
            else
            {
                return $"Array{arrayType.Rank}D";
            }
        }

        /// <summary>
        /// Gets the C# keyword equivalent or type name for a type symbol.
        /// </summary>
        private static string GetCSharpKeywordOrTypeName(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return string.Empty;
            }

            // For primitive types, use the special types format which gives us keywords
            if (typeSymbol.SpecialType != SpecialType.None)
            {
                return typeSymbol.ToDisplayString(CSharpShortErrorMessageFormat);
            }

            // For other types, use just the name
            return typeSymbol.Name;
        }

        /// <summary>
        /// Gets the original name of a member from its symbol.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The original member name.</returns>
        public static string GetShortNameWithoutTypeArgs(ISymbol? symbol)
        {
            if (symbol is IPropertySymbol property && IsPropertyIndexer(property))
            {
                return "this"; // Force "this" for indexers
            }

            var name = symbol?.Name ?? Constants.ErrorString;

            // For explicit interface implementations, Roslyn symbol names may include the interface prefix
            // like "IInterface.Member". Use the member part as the original name for identifier purposes.
            if (name.Contains('.'))
            {
                name = name.Substring(name.LastIndexOf('.') + 1);
            }

            return name;
        }

        /// <summary>
        /// Generates a unique key for a symbol to resolve naming conflicts, especially for overloaded methods or properties.
        /// The key includes the symbol's short name, and for methods/properties, it appends generic type parameters and parameter types.
        /// </summary>
        /// <param name="symbol">The symbol (e.g., IMethodSymbol, IPropertySymbol) for which to generate the key.</param>
        /// <returns>A string representing the conflict resolution key.</returns>
        public static string GetMemberConflictResolutionKey(ISymbol? symbol)
        {
            var key = GetShortNameWithoutTypeArgs(symbol);

            switch (symbol)
            {
                case IMethodSymbol method:
                    {
                        if (method.IsGenericMethod)
                        {
                            var typeParams = string.Join(",", method.TypeParameters.Select(x => x.ToDisplayString(FullyQualifiedNoNullableAnnotationFormat)));
                            key += $"<{typeParams}>";
                        }

                        var parameters = string.Join(",", method.Parameters.Select(x => x.Type.ToDisplayString(FullyQualifiedNoNullableAnnotationFormat)));
                        key += $"({parameters})";
                    }
                    break;

                case IPropertySymbol property:
                    {
                        var parameters = string.Join(",", property.Parameters.Select(x => x.Type.ToDisplayString(FullyQualifiedNoNullableAnnotationFormat)));
                        key += $"[{parameters}]";
                    }
                    break;
            }

            return key;//.Replace("?", string.Empty);  // Ignore nullability difference
        }

        /// <summary>
        /// Generates a sanitized suffix from a collection of generic type parameters.
        /// </summary>
        /// <param name="typeParameters">The collection of ITypeParameterSymbol to sanitize.</param>
        /// <returns>A string representing the sanitized generic type parameter suffix.</returns>
        public static string GetSanitizedGenericTypeParameterSuffix(ISymbol? symbol)
        {
            if (symbol is not INamedTypeSymbol namedSymbol || !namedSymbol.IsGenericType)
            {
                return string.Empty;
            }

            return $"T{namedSymbol.TypeArguments.Length}";
        }

        /// <summary>
        /// Generates a sanitized short type name from a fully qualified type name string.
        /// </summary>
        /// <param name="fullyQualifiedTypeName">The fully qualified type name string.</param>
        /// <returns>A string representing the sanitized short generic type name.</returns>
        public static string GetSanitizedShortMemberName(string fullyQualifiedTypeName)
        {
            var originalName = fullyQualifiedTypeName;
            var sanitizedName = fullyQualifiedTypeName;

            sanitizedName = sanitizedName.Replace("[]", "Array");

            while (_innermostGenericRegex.IsMatch(sanitizedName))
            {
                sanitizedName = _innermostGenericRegex.Replace(sanitizedName, match =>
                {
                    var content = match.Groups[1].Value;

                    // Count type arguments by splitting by comma, handling empty content case
                    var typeArgCount = string.IsNullOrWhiteSpace(content) ? 0 : content.Split(',').Length;
                    return $"T{typeArgCount}";
                });
            }

            // Must perform at last (input may be 'Container<X, Y.Z>')
            int lastDotIndex = sanitizedName.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                sanitizedName = sanitizedName.Substring(lastDotIndex + 1);
            }

            return $"/*GetSanitizedShortMemberName({originalName})*/" + sanitizedName;
        }

        /// <summary>
        /// Extracts the explicit interface implementation target name from a symbol's name.
        /// For members like 'IMyInterface.MyMethod', this method returns 'IMyInterface'.
        /// </summary>
        /// <param name="symbol">The symbol (e.g., IMethodSymbol, IPropertySymbol, IEventSymbol) to get the explicit interface target name for.</param>
        /// <returns>The explicit interface target name, or an empty string if no target is found.</returns>
        public static string GetExplicitInterfaceTargetName(ISymbol? symbol)
        {
            if (symbol != null)
            {
                var name = (GetExplicitInterfaceType(symbol) ?? symbol.ContainingType).ToDisplayString(FullyQualifiedNullableFormat);

                // // Roslyn cannot get correct explicit interface implementation from complex type hierarchy
                // // Example:
                // //   interface A : B { }
                // //   interface B { void X(); }
                // //   class C : A { void A.X(){} void B.X(){} }
                // //   --> always returns B by symbol analysis but symbol.Name allows taking A/B in ...string!!
                // var name = symbol.Name;

                // var lastDotIndex = name.LastIndexOf('.');
                // if (lastDotIndex >= 0 && lastDotIndex < name.Length - 1)
                // {
                //     name = name.Substring(0, lastDotIndex);
                // }

                return "/*GetExplicitInterfaceTargetName*/" + name;
            }

            return string.Empty;
        }

        /// <summary>
        /// Generates the 'where' clause for a given type parameter symbol, including all its constraints.
        /// </summary>
        /// <param name="typeParameterSymbol">The ITypeParameterSymbol to generate the 'where' clause for.</param>
        /// <returns>A string representing the 'where' clause (e.g., "where T : class, IMyInterface, new()"), or an empty string if no constraints.</returns>
        public static string? GenerateWhereClause(ITypeParameterSymbol typeParameterSymbol, string? overrideTypeParameterName = null)
        {
            var constraints = new List<string>();

            // Class constraint
            if (typeParameterSymbol.HasReferenceTypeConstraint)
            {
                if (typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                {
                    constraints.Add("class?");
                }
                else
                {
                    constraints.Add("class");
                }
            }
            // Notnull constraint
            else if (typeParameterSymbol.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }
            // Unmanaged constraint
            else if (typeParameterSymbol.HasUnmanagedTypeConstraint)  // Must check before 'struct' constraint
            {
                constraints.Add("unmanaged");
            }
            // Struct constraint
            else if (typeParameterSymbol.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            // Base type and interface constraints
            foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
            {
                var typeName = constraintType.ToDisplayString(FullyQualifiedNullableFormat);

                // if (constraintType.NullableAnnotation == NullableAnnotation.Annotated)
                // {
                //     typeName += "?";
                // }

                constraints.Add(typeName);
            }

            // New() constraint
            if (typeParameterSymbol.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                return $"where {overrideTypeParameterName ?? typeParameterSymbol.Name} : {string.Join(", ", constraints)}";
            }

            return null;
        }

        /// <summary>
        /// Gets the topmost overridden method in the inheritance hierarchy for a given method.
        /// </summary>
        /// <param name="method">The method to find the topmost overridden method for.</param>
        /// <returns>The topmost overridden method, or the original method if it doesn't override anything.</returns>
        public static IMethodSymbol GetOverriddenMethod(IMethodSymbol method)
        {
            if (!method.IsOverride)
            {
                return method;
            }

            IMethodSymbol overridden = method;
            do
            {
                var next = overridden.OverriddenMethod;
                if (next == null)
                    break;

                overridden = next;
            }
            while (true);

            return overridden;
        }
    }
}