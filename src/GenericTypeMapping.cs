using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Represents a mapping between generic type parameters of a target type and a mock class.
    /// This is crucial for handling generic types during mock code generation.
    /// </summary>
    public class GenericTypeMapping
    {
        /// <summary>
        /// Gets or sets the mapping from target type parameters to mock type parameters.
        /// </summary>
        public Dictionary<ITypeParameterSymbol, ITypeParameterSymbol> TypeParameterSymbolMap { get; set; } = new Dictionary<ITypeParameterSymbol, ITypeParameterSymbol>(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets or sets the mock class type parameter symbols.
        /// </summary>
        public List<ITypeParameterSymbol> MockClassTypeParameterSymbols { get; set; } = new List<ITypeParameterSymbol>();

        /// <summary>
        /// Gets or sets the target type argument symbols.
        /// </summary>
        public List<ITypeSymbol> TargetTypeArgumentSymbols { get; set; } = new List<ITypeSymbol>();
        /// <summary>
        /// Original type parameters from the target type definition (e.g., TKey, TValue from Dictionary&lt;TKey, TValue&gt;).
        /// Used for concrete generic mapping where we need to replace type parameters with concrete types.
        /// </summary>
        public List<ITypeParameterSymbol> OriginalTargetTypeParameters { get; set; } = new List<ITypeParameterSymbol>();

        /// <summary>
        /// Gets whether the mapping is valid.
        /// </summary>
        public bool IsValidMapping => TypeParameterSymbolMap.Count > 0 && 
                                     MockClassTypeParameterSymbols.Count == TargetTypeArgumentSymbols.Count;

        /// <summary>
        /// Applies the type parameter mapping to a type symbol, replacing target type parameters with mock type parameters.
        /// This is used for unbound generic scenarios where we need to map IDictionary&lt;TKey, TValue&gt; to Mock&lt;X, Y&gt;.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to map.</param>
        /// <returns>The mapped type symbol, or the original if no mapping is needed.</returns>
        public ITypeSymbol? ApplyTypeMapping(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol == null || TypeParameterSymbolMap.Count == 0)
            {
                return typeSymbol;
            }

            // Handle type parameters directly
            if (typeSymbol is ITypeParameterSymbol typeParam)
            {
                if (TypeParameterSymbolMap.TryGetValue(typeParam, out var mappedTypeParam))
                {
                    return mappedTypeParam;
                }
                return typeSymbol;
            }

            // Handle named types (generic types)
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // Check if any type arguments need mapping
                var needsMapping = namedType.TypeArguments.Any(ta => 
                    ta is ITypeParameterSymbol tp && TypeParameterSymbolMap.ContainsKey(tp));

                if (needsMapping)
                {
                    // We can't easily construct new ITypeSymbol instances
                    // The mapping will be handled at the string conversion level in TypeConverter
                    return typeSymbol;
                }
            }

            // Handle array types
            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                var mappedElementType = ApplyTypeMapping(arrayType.ElementType);
                if (!ReferenceEquals(mappedElementType, arrayType.ElementType))
                {
                    // Array element type was mapped, but we can't create new array symbols easily
                    return typeSymbol;
                }
            }

            return typeSymbol;
        }

        /// <summary>
        /// Applies the type parameter mapping to a type string, replacing target type parameter names with mock type parameter names.
        /// This is the primary method for applying type mapping during code generation.
        /// </summary>
        /// <param name="typeString">The type string to map (e.g., "System.Collections.Generic.IDictionary&lt;TKey, TValue&gt;").</param>
        /// <returns>The mapped type string (e.g., "System.Collections.Generic.IDictionary&lt;X, Y&gt;").</returns>
        public string ApplyTypeMappingToString(string typeString)
        {
            const string MARK = @"/*ApplyTypeMappingToString*/";

            if (string.IsNullOrEmpty(typeString))
            {
                return MARK;//string.Empty;
            }

            var result = typeString;

            // Handle unbound generic display patterns like "Type<>", "Type<,>", "Type<,,>", etc.
            // When present, expand with mock class type parameter names to avoid generating unbound generic names.
            // This keeps expansion logic centralized in the mapping layer.
            var unboundMatch = System.Text.RegularExpressions.Regex.Match(result, @"<(,*)>");
            if (unboundMatch.Success)
            {
                var commas = unboundMatch.Groups[1].Value; // ",,," -> arity = commas.Length + 1
                var arity = commas.Length + 1;

                var mockParams = MockClassTypeParameterSymbols?.Select(tp => tp.Name).ToList() ?? new List<string>();
                if (mockParams.Count >= arity && arity > 0)
                {
                    var replacement = "<" + string.Join(", ", mockParams.Take(arity)) + ">";
                    result = System.Text.RegularExpressions.Regex.Replace(result, @"<(,*)>", replacement);
                }
            }
            
            // Handle concrete generic mapping (e.g., Dictionary<int, string>)
            if (OriginalTargetTypeParameters.Count > 0 && TargetTypeArgumentSymbols.Count == OriginalTargetTypeParameters.Count)
            {
                // Replace original type parameters (TKey, TValue) with concrete types (int, string)
                for (int i = 0; i < OriginalTargetTypeParameters.Count; i++)
                {
                    var originalParam = OriginalTargetTypeParameters[i].Name;
                    var concreteType = TargetTypeArgumentSymbols[i].ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    
                    // Replace type parameter names, being careful about word boundaries
                    result = System.Text.RegularExpressions.Regex.Replace(
                        result, 
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(originalParam)}\b", 
                        concreteType);
                }
                
                return MARK + result;
            }
            
            // Handle unbound generic mapping (e.g., Dictionary<T, U> -> Dictionary<X, Y>)
            if (TypeParameterSymbolMap.Count > 0)
            {
                // Apply each mapping
                foreach (var mapping in TypeParameterSymbolMap)
                {
                    var targetParam = mapping.Key.Name;
                    var mockParam = mapping.Value.Name;
                    
                    // Replace type parameter names, being careful about word boundaries
                    result = System.Text.RegularExpressions.Regex.Replace(
                        result, 
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(targetParam)}\b", 
                        mockParam);
                }
            }
            
            return MARK + result;
        }

        /// <summary>
        /// Determines if this mapping should be applied based on the generic mocking mode.
        /// </summary>
        /// <returns>True if type mapping should be applied, false otherwise.</returns>
        public bool ShouldApplyMapping()
        {
            return IsValidMapping && (TypeParameterSymbolMap.Count > 0 || OriginalTargetTypeParameters.Count > 0);
        }
    }
}
