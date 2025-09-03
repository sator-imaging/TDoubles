using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Provides helper methods for working with blueprints and code generation.
    /// </summary>
    public static class BlueprintHelpers
    {
        /// <summary>
        /// Converts the return type of a unified member blueprint to its fully qualified string representation.
        /// Uses the member's generic mapping to replace target type parameters with mock type parameters.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <returns>The fully qualified return type name with generic mapping applied.</returns>
        public static string ToReturnTypeString(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint?.ReturnTypeSymbol == null)
            {
                return memberBlueprint?.IsVoid == true ? "void" : "object";
            }

            var fullyQualifiedString = memberBlueprint.ReturnTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
            
            // Apply member's generic mapping if available
            if (memberBlueprint.GenericMapping.ShouldApplyMapping())
            {
                fullyQualifiedString = memberBlueprint.GenericMapping.ApplyTypeMappingToString(fullyQualifiedString);
            }
            
            return fullyQualifiedString;
        }

        /// <summary>
        /// Converts a specific parameter type from a unified member blueprint to its fully qualified string representation.
        /// Uses the member's generic mapping to replace target type parameters with mock type parameters.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <param name="parameterIndex">The index of the parameter to convert.</param>
        /// <returns>The fully qualified parameter type name with generic mapping applied.</returns>
        public static string ToParameterTypeString(UnifiedMemberBlueprint memberBlueprint, int parameterIndex)
        {
            if (memberBlueprint?.Parameters == null || parameterIndex < 0 || parameterIndex >= memberBlueprint.Parameters.Count)
            {
                return Constants.ErrorString;
            }

            var parameter = memberBlueprint.Parameters[parameterIndex];
            if (parameter.ParameterSymbol?.Type == null)
            {
                return "/*ToParameterTypeString::EarlyReturn*/ object";
            }

            var fullyQualifiedString = parameter.ParameterSymbol.Type.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);

            // Apply member's generic mapping if available
            if (memberBlueprint.GenericMapping.ShouldApplyMapping())
            {
                fullyQualifiedString = memberBlueprint.GenericMapping.ApplyTypeMappingToString(fullyQualifiedString);
            }

            return fullyQualifiedString;
        }

        /// <summary>
        /// For override property signatures: if the return type is a method-level generic type parameter,
        /// substitutes it with 'object'. Otherwise, uses the normal return type string.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The return type string suitable for override properties.</returns>
        public static string ToOverridePropertyReturnTypeString(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint == null) throw new System.ArgumentNullException(nameof(memberBlueprint));

            var type = memberBlueprint.ReturnTypeSymbol;
            if (type != null && memberBlueprint.GenericTypeParameterSymbols != null && SymbolHelpers.UsesMethodLevelTypeParameter(type, memberBlueprint.GenericTypeParameterSymbols))
            {
                return "object";
            }
            return ToReturnTypeString(memberBlueprint);
        }

        /// <summary>
        /// For override property signatures: if a parameter type is a method-level generic type parameter
        /// (declared on the method itself, not on the containing type), replaces it with 'object'.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <param name="parameterIndex">The index of the parameter to convert.</param>
        /// <returns>The parameter type string suitable for override properties.</returns>
        public static string ToOverridePropertyParameterTypeString(UnifiedMemberBlueprint memberBlueprint, int parameterIndex)
        {
            if (memberBlueprint == null) throw new System.ArgumentNullException(nameof(memberBlueprint));

            if (memberBlueprint.Parameters == null || parameterIndex < 0 || parameterIndex >= memberBlueprint.Parameters.Count)
            {
                return "/*ToOverridePropertyParameterTypeString::EarlyReturn_1*/ object";
            }

            var parameter = memberBlueprint.Parameters[parameterIndex];
            var type = parameter.ParameterSymbol?.Type;
            if (type != null && memberBlueprint.GenericTypeParameterSymbols != null && SymbolHelpers.UsesMethodLevelTypeParameter(type, memberBlueprint.GenericTypeParameterSymbols))
            {
                return "/*ToOverridePropertyParameterTypeString::EarlyReturn_2*/ object";
            }

            return ToParameterTypeString(memberBlueprint, parameterIndex);
        }

        /// <summary>
        /// Converts the containing type of a unified member blueprint to its fully qualified string representation.
        /// Uses the member's generic mapping to replace target type parameters with mock type parameters.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <returns>The fully qualified containing type name with generic mapping applied.</returns>
        public static string ToContainingTypeString(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint?.ContainingType == null)
            {
                return "object";
            }

            var fullyQualifiedString = memberBlueprint.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);

            // if (fullyQualifiedString == Constants.ValueTypeFullName)
            // {
            //     return "object";
            // }

            // Apply member's generic mapping if available
                if (memberBlueprint.GenericMapping.ShouldApplyMapping())
                {
                    return memberBlueprint.GenericMapping.ApplyTypeMappingToString(fullyQualifiedString);
                }
            
            return fullyQualifiedString;
        }

        /// <summary>
        /// Converts a specific parameter from a unified member blueprint to a complete parameter declaration string.
        /// Includes modifiers, type (with generic mapping), and parameter name.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <param name="parameterIndex">The index of the parameter to convert.</param>
        /// <returns>A complete parameter declaration string (e.g., "ref System.Collections.Generic.IDictionary&lt;X, Y&gt; dict").</returns>
        public static string ToParameterDeclarationString(UnifiedMemberBlueprint memberBlueprint, int parameterIndex, string? overrideTypeString = null)
        {
            if (memberBlueprint?.Parameters == null || parameterIndex < 0 || parameterIndex >= memberBlueprint.Parameters.Count)
            {
                return "/*ToParameterDeclarationString::EarlyReturn*/ object param";
            }

            var parameter = memberBlueprint.Parameters[parameterIndex];
            
            var declaration = new StringBuilder();
            
            // Add modifier based on parameter symbol properties
            if (parameter.IsRef)
            {
                declaration.Append("ref ");
            }
            else if (parameter.IsOut)
            {
                declaration.Append("out ");
            }
            else if (parameter.IsIn)
            {
                declaration.Append("in ");
            }
            else if (parameter.IsParams)
            {
                declaration.Append("params ");
            }
            
            // Add type and name from parameter symbol
            var typeString = overrideTypeString ?? ToParameterTypeString(memberBlueprint, parameterIndex);
            var parameterName = parameter.ParameterSymbol?.Name ?? "param";
            declaration.Append($"{typeString} {parameterName}");
            
            // Add default value if present
            if (parameter.ParameterSymbol?.HasExplicitDefaultValue == true)
            {
                var defaultValue = SymbolHelpers.ToDefaultValueString(parameter.ParameterSymbol.ExplicitDefaultValue);
                declaration.Append($" = {defaultValue}");
            }
            
            return declaration.ToString();
        }

        /// <summary>
        /// Converts the return type of a unified member blueprint to its C# keyword equivalent when applicable.
        /// Uses the member's generic mapping to replace target type parameters with mock type parameters.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <returns>The C# keyword equivalent return type with generic mapping applied.</returns>
        public static string ToReturnTypeCSharpKeywordString(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint?.ReturnTypeSymbol == null)
            {
                return memberBlueprint?.IsVoid == true ? "void" : "object";
            }

            var keywordString = memberBlueprint.ReturnTypeSymbol.ToDisplayString(SymbolHelpers.CSharpShortErrorMessageFormat);
            
            // Apply member's generic mapping if available
            if (memberBlueprint.GenericMapping.ShouldApplyMapping())
            {
                return memberBlueprint.GenericMapping.ApplyTypeMappingToString(keywordString);
            }
            
            return keywordString;
        }

        /// <summary>
        /// Converts the return type of a unified member blueprint to a short string representation for documentation.
        /// Uses the member's generic mapping and formats for XML documentation with proper escaping.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <returns>A documentation-formatted return type string with proper XML escaping and generic mapping applied.</returns>
        public static string ToReturnTypeDocumentationString(UnifiedMemberBlueprint memberBlueprint)
        {
            var docString = ToReturnTypeCSharpKeywordString(memberBlueprint);

            // Escape XML characters for documentation
            return docString
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                ;
        }

        /// <summary>
        /// Converts a specific parameter type from a unified member blueprint to a documentation string.
        /// Uses the member's generic mapping and formats for XML documentation with proper escaping.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <param name="parameterIndex">The index of the parameter to convert.</param>
        /// <returns>A documentation-formatted parameter type string with proper XML escaping and generic mapping applied.</returns>
        public static string ToParameterTypeDocumentationString(UnifiedMemberBlueprint memberBlueprint, int parameterIndex)
        {
            if (memberBlueprint?.Parameters == null || parameterIndex < 0 || parameterIndex >= memberBlueprint.Parameters.Count)
            {
                return "/*ToParameterTypeDocumentationString::EarlyReturn_1*/ object";
            }

            var parameter = memberBlueprint.Parameters[parameterIndex];
            if (parameter.ParameterSymbol?.Type == null)
            {
                return "/*ToParameterTypeDocumentationString::EarlyReturn_2*/object";
            }

            var keywordString = parameter.ParameterSymbol.Type.ToDisplayString(SymbolHelpers.CSharpShortErrorMessageFormat);
            
            // Apply member's generic mapping if available
            if (memberBlueprint.GenericMapping.ShouldApplyMapping())
            {
                keywordString = memberBlueprint.GenericMapping.ApplyTypeMappingToString(keywordString);
            }

            // Escape XML characters for documentation
            return keywordString
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                ;
        }

        /// <summary>
        /// Converts a specific indexer parameter type from a unified member blueprint to a documentation string.
        /// Uses the member's generic mapping and formats for XML documentation with proper escaping.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint containing all type information.</param>
        /// <param name="parameterIndex">The index of the indexer parameter to convert.</param>
        /// <returns>A documentation-formatted indexer parameter type string with proper XML escaping and generic mapping applied.</returns>
        public static string ToIndexerParameterTypeDocumentationString(UnifiedMemberBlueprint memberBlueprint, int parameterIndex)
        {
            if (memberBlueprint?.Parameters == null || parameterIndex < 0 || parameterIndex >= memberBlueprint.Parameters.Count)
            {
                return "object";
            }

            var parameter = memberBlueprint.Parameters[parameterIndex];
            if (parameter.ParameterSymbol?.Type == null)
            {
                return "object";
            }

            var keywordString = parameter.ParameterSymbol.Type.ToDisplayString(SymbolHelpers.CSharpShortErrorMessageFormat);
            
            // Apply member's generic mapping if available
            if (memberBlueprint.GenericMapping.ShouldApplyMapping())
            {
                keywordString = memberBlueprint.GenericMapping.ApplyTypeMappingToString(keywordString);
            }

            // Escape XML characters for documentation
            return keywordString
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                ;
        }

        /// <summary>
        /// Converts an AccessibilityLevel enum to its string representation.
        /// </summary>
        /// <param name="accessibilityLevel">The accessibility level to convert.</param>
        /// <returns>The accessibility string in lowercase format.</returns>
        public static string ToAccessibilityString(AccessibilityLevel accessibilityLevel)
        {
            return accessibilityLevel switch
            {
                AccessibilityLevel.Public => "public",
                AccessibilityLevel.Private => "private",
                AccessibilityLevel.Protected => "protected",
                AccessibilityLevel.Internal => "internal",
                AccessibilityLevel.ProtectedInternal => "protected internal",
                AccessibilityLevel.PrivateProtected => "private protected",
                _ => "private"
            };
        }

        /// <summary>
        /// Gets the original name of a member from its symbol.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The original member name.</returns>
        public static string GetOriginalName(UnifiedMemberBlueprint memberBlueprint)
        {
            return SymbolHelpers.GetShortNameWithoutTypeArgs(memberBlueprint.OriginalSymbol);
        }

        /// <summary>
        /// Gets the original name of a method, including its method type arguments if it's a generic method.
        /// </summary>
        /// <param name="methodSymbol">The IMethodSymbol to get the name for.</param>
        /// <returns>The original name of the method with its type arguments (e.g., "MyMethod<T>").</returns>
        public static string GetOriginalNameWithTypeArgs(UnifiedMemberBlueprint memberBlueprint)
        {
            var name = GetOriginalName(memberBlueprint);
            if (memberBlueprint.IsGeneric)
            {
                var typeArgs = string.Join(", ", memberBlueprint.GenericTypeParameterSymbols.Select(tp => tp.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)));
                name += $"<{typeArgs}>";

                if (memberBlueprint.GenericMapping.ShouldApplyMapping())
                {
                    name = memberBlueprint.GenericMapping.ApplyTypeMappingToString(name);
                }
            }

            return name;
        }

        /// <summary>
        /// Gets the resolved name of a member from conflict resolution or original name.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <param name="conflictResolution">The conflict resolution map.</param>
        /// <returns>The resolved member name.</returns>
        public static string GetResolvedName(UnifiedMemberBlueprint memberBlueprint, MockClassBlueprint blueprint)
        {
            if (blueprint?.ConflictResolution == null)
            {
                return GetOriginalName(memberBlueprint);
            }
            
            //var memberKey = GetResolvedNameKey(memberBlueprint);
            if (blueprint.ConflictResolution.ResolvedNames.TryGetValue(memberBlueprint, out var resolvedName))
            {
                return resolvedName;
            }
            
            var originalName = GetOriginalName(memberBlueprint);
            return originalName;
        }

        /// <summary>
        /// Gets the member key for conflict resolution.
        /// </summary>
        public static string GetResolvedNameConflictResolutionKey(UnifiedMemberBlueprint member)
        {
            var signatureBuilder = new StringBuilder();

            signatureBuilder.Append($"{member.MemberType}#{GetOriginalName(member)}");

            if (member.Parameters.Count > 0)
            {
                var paramTypes = string.Join(",", member.Parameters.Select(p => GetParameterType(p)));
                signatureBuilder.Append($"({paramTypes})");
            }

            // Add return type to the signature
            if (member.ReturnTypeSymbol != null)
            {
                string returnTypeString;

                // Check if the return type is a generic type parameter
                if (member.ReturnTypeSymbol is ITypeParameterSymbol typeParameterSymbol)
                {
                    returnTypeString = typeParameterSymbol.Name; // Use its name (e.g., "T", "U")
                }
                else
                {
                    returnTypeString = member.ReturnTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                }

                signatureBuilder.Append($"/{returnTypeString}");
            }
            else
            {
                signatureBuilder.Append("/void");
            }

            return signatureBuilder.ToString()
                                   .Replace("?", string.Empty)  // Ignore nullability difference
                                   ;
        }

        /// <summary>
        /// Gets the explicit implementation name for interface members.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The explicit implementation name.</returns>
        public static string GetExplicitImplementationName(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint?.ExplicitInterfaceType == null)
            {
                return string.Empty;
            }

            // var interfaceName = ToGenericTypeMappingAwareFullyQualifiedTypeName(memberBlueprint.ExplicitInterfaceType, memberBlueprint);
            var interfaceName = SymbolHelpers.GetExplicitInterfaceTargetName(memberBlueprint.OriginalSymbol);

            if (memberBlueprint.GenericMapping.ShouldApplyMapping())
            {
                interfaceName = memberBlueprint.GenericMapping.ApplyTypeMappingToString(interfaceName);
            }

            var memberName = GetOriginalName(memberBlueprint);

            return $"{interfaceName}.{memberName}";
        }

        /// <summary>
        /// Gets the containing type name from a member blueprint.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The containing type name.</returns>
        public static string GetContainingTypeName(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint?.ContainingType == null)
            {
                return string.Empty;
            }
            return memberBlueprint.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        }

        /// <summary>
        /// Gets the generic type parameters as strings.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>List of generic type parameter names.</returns>
        public static List<string> GetGenericTypeParameters(UnifiedMemberBlueprint memberBlueprint)
        {
            return memberBlueprint?.GenericTypeParameterSymbols?.Select(tp => tp.Name).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Gets the generic constraints as a string.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The generic constraints string.</returns>
        public static List<string>? GetGenericConstraints(UnifiedMemberBlueprint memberBlueprint)
        {
            if (memberBlueprint?.GenericTypeParameterSymbols == null || memberBlueprint.GenericTypeParameterSymbols.Count == 0)
            {
                return null;
            }

            var constraints = memberBlueprint.GenericTypeParameterSymbols
                .Select(tp => SymbolHelpers.GenerateWhereClause(tp))
                .Where(x => x != null)
                .OfType<string>()
                .Select(x =>
                {
                    if (memberBlueprint.GenericMapping.ShouldApplyMapping())
                    {
                        return memberBlueprint.GenericMapping.ApplyTypeMappingToString(x);
                    }
                    return x;
                })
                .ToList();

            return constraints;
        }

        /// <summary>
        /// Gets the parameter name from a parameter blueprint.
        /// </summary>
        /// <param name="parameter">The parameter blueprint.</param>
        /// <returns>The parameter name.</returns>
        public static string GetParameterName(ParameterBlueprint parameter)
        {
            return parameter?.ParameterSymbol?.Name ?? "param";
        }

        /// <summary>
        /// Gets the mock class name from the mock class symbol.
        /// </summary>
        /// <param name="blueprint">The mock class blueprint.</param>
        /// <returns>The mock class name.</returns>
        public static string GetMockClassName(MockClassBlueprint blueprint)
        {
            return blueprint?.MockClassSymbol?.Name ?? "MockClass";
        }

        /// <summary>
        /// Gets the mock namespace from the mock namespace symbol.
        /// </summary>
        /// <param name="blueprint">The mock class blueprint.</param>
        /// <returns>The mock namespace string.</returns>
        public static string GetMockNamespace(MockClassBlueprint blueprint)
        {
            return SymbolHelpers.ToNamespaceString(blueprint?.MockNamespaceSymbol);
        }

        /// <summary>
        /// Gets the required namespace symbols from a mock class blueprint for generating using directives.
        /// </summary>
        /// <param name="blueprint">The mock class blueprint.</param>
        /// <returns>Collection of namespace symbols for using directives.</returns>
        public static IEnumerable<INamespaceSymbol> GetUsingDirectives(MockClassBlueprint blueprint)
        {
            if (blueprint?.RequiredNamespaces == null)
            {
                return Enumerable.Empty<INamespaceSymbol>();
            }

            return blueprint.RequiredNamespaces
                .Where(ns => ns != null && !ns.IsGlobalNamespace);
        }

        /// <summary>
        /// Gets the mock class type parameters from generic type mapping.
        /// </summary>
        /// <param name="typeMapping">The generic type mapping.</param>
        /// <returns>List of mock class type parameter names.</returns>
        public static List<string> GetMockClassTypeParameters(GenericTypeMapping typeMapping)
        {
            return typeMapping?.MockClassTypeParameterSymbols?.Select(tp => tp.Name).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Gets the accessibility level from a unified member blueprint.
        /// </summary>
        /// <param name="memberBlueprint">The unified member blueprint.</param>
        /// <returns>The accessibility level.</returns>
        public static string GetAccessibility(UnifiedMemberBlueprint memberBlueprint)
        {
            return ToAccessibilityString(memberBlueprint?.AccessibilityLevel ?? AccessibilityLevel.Private);
        }

        /// <summary>
        /// Gets the parameter type string from a parameter blueprint.
        /// </summary>
        /// <param name="parameter">The parameter blueprint.</param>
        /// <returns>The parameter type string.</returns>
        public static string GetParameterType(ParameterBlueprint parameter)
        {
            if (parameter?.ParameterSymbol?.Type == null)
            {
                return "object";
            }
            return parameter.ParameterSymbol.Type.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        }

        /// <summary>
        /// Gets the type symbol from a parameter blueprint.
        /// </summary>
        /// <param name="parameter">The parameter blueprint.</param>
        /// <returns>The type symbol.</returns>
        public static ITypeSymbol? GetParameterTypeSymbol(ParameterBlueprint parameter)
        {
            return parameter?.ParameterSymbol?.Type;
        }

        /// <summary>
        /// Gets the modifier string from a parameter blueprint.
        /// </summary>
        /// <param name="parameter">The parameter blueprint.</param>
        /// <returns>The modifier string.</returns>
        public static string GetParameterModifier(ParameterBlueprint parameter)
        {
            if (parameter == null) return string.Empty;
            
            if (parameter.IsRef) return "ref";
            if (parameter.IsOut) return "out";
            if (parameter.IsIn) return "in";
            if (parameter.IsParams) return "params";
            
            return string.Empty;
        }

        /// <summary>
        /// Gets the parameter name with appropriate modifiers for method invocation.
        /// </summary>
        /// <param name="parameter">The parameter blueprint.</param>
        /// <returns>The parameter name prefixed with "ref" or "out" if applicable.</returns>
        public static string ToParameterInvocationString(ParameterBlueprint parameter, string? refAndOutParameterPrefix = null)
        {
            if (parameter == null) return "param";

            var name = parameter.ParameterSymbol?.Name ?? "param";

            if (parameter.IsRef)
            {
                return $"ref {refAndOutParameterPrefix}{name}";
            }
            else if (parameter.IsOut)
            {
                return $"out {refAndOutParameterPrefix}{name}";
            }

            // 'in' and 'params' modifiers are part of the declaration, not the invocation
            return name;
        }
   }
}