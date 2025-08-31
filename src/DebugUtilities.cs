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
    /// Provides utility methods for generating debug information related to Roslyn symbols and compilations.
    /// </summary>
    public static class DebugUtilities
    {
        /// <summary>
        /// Converts a StringBuilder's content into a multi-line C# comment string.
        /// </summary>
        /// <param name="sb">The StringBuilder instance.</param>
        /// <returns>The content of the StringBuilder formatted as a C# comment.</returns>
        private static string ToCommentString(this StringBuilder sb) => "// " + sb.ToString().TrimEnd().Replace("\n", "\n// ");

        /// <summary>
        /// Generates debug information about the type hierarchy and resolved members for a given target type.
        /// </summary>
        /// <param name="targetType">The target type to analyze.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>A string containing formatted debug information.</returns>
        public static string GenerateTypeHierarchyDebugInfo(ITypeSymbol targetType, Compilation compilation)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Analyzing members of type {targetType.Name} using TypeHierarchyGraph:");

            var graph = TypeHierarchyGraph.Build(targetType, includeInternals: true, compilation);
            var namedTargetType = targetType as INamedTypeSymbol ?? throw new InvalidOperationException("Target type must be a named type symbol for blueprint building.");
            var bp = new BlueprintBuilder().BuildBlueprint(targetType, namedTargetType, includeInternals: true, GenericMockingMode.UnboundGeneric, compilation, new List<string>());

            foreach (var i in graph.AllInterfaces)
            {
                sb.AppendLine();
                var typeAssemInfo = GenerateTypeAssemblyDebugInfo(i);
                sb.AppendLine(typeAssemInfo);
            }

            sb.AppendLine();
            sb.AppendLine($"Total resolved members found: {bp.Members.Count} (TypeGraph: {graph.ResolvedMembers.Count})");
            sb.AppendLine();

            foreach (var resolvedMember in bp.Members//graph.ResolvedMembers.Values
                .OrderBy(x =>
                {
                    var name = x.OriginalSymbol?.Name ?? string.Empty;
                    int pos = name.LastIndexOf('.');
                    if (pos >= 0)
                    {
                        name = name.Substring(pos + 1);
                    }
                    return name;
                })
                .ThenBy(x => x.OriginalSymbol?.Kind)
                .ThenBy(x => x.OriginalSymbol?.ContainingType?.Name)
                )
            {
                if (resolvedMember.OriginalSymbol == null) continue;
                var member = resolvedMember.OriginalSymbol;
                sb.AppendLine($"  [{member.Kind}] {member.Name} ({member.ContainingType}.{member.Name})");
                sb.AppendLine($"    IsExplicitInterfaceImplementation: {resolvedMember.IsExplicitInterfaceImplementation}");
                if (resolvedMember.IsExplicitInterfaceImplementation)
                {
                    sb.AppendLine($"    Explicitly implements: {resolvedMember.ExplicitInterfaceType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                }

                if (member is IMethodSymbol methodSymbol)
                {
                    sb.AppendLine($"    Method Return Type: {methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    sb.AppendLine($"    Method Return Nullability: {methodSymbol.ReturnType.NullableAnnotation}");

                    if (methodSymbol.Parameters.Any())
                    {
                        sb.AppendLine($"    Parameters:");
                        int i = -1;
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            i++;
                            sb.AppendLine($"      - ({BlueprintHelpers.ToParameterDeclarationString(resolvedMember, i)})  Name: {parameter.Name}, Type: {parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, Nullability: {parameter.Type.NullableAnnotation}");
                        }
                        sb.AppendLine($"    Unified Parameters:");
                        foreach (var parameter in resolvedMember.Parameters)
                        {
                            i++;
                            sb.AppendLine($"      - ({BlueprintHelpers.ToParameterDeclarationString(resolvedMember, i)})  Name: {parameter.ParameterSymbol.Name}, Type: {parameter.ParameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, Nullability: {parameter.ParameterSymbol.Type.NullableAnnotation}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"    No parameters.");
                    }
                }
                else if (member is IPropertySymbol propertySymbol)
                {
                    sb.AppendLine($"    Property Type: {propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    sb.AppendLine($"    Property Nullability: {propertySymbol.Type.NullableAnnotation}");
                }
                else if (member is IEventSymbol eventSymbol)
                {
                    sb.AppendLine($"    Event Type: {eventSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    sb.AppendLine($"    Event Nullability: {eventSymbol.Type.NullableAnnotation}");
                }

                sb.AppendLine();
            }

            return sb.ToCommentString();
        }

        /// <summary>
        /// Generates debug information about the current compilation.
        /// </summary>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>A string containing formatted debug information about the compilation.</returns>
        public static string GenerateCompilationDebugInfo(Compilation compilation)
        {
            var sb = new StringBuilder();

            sb.AppendLine("--- Compilation Information ---");
            sb.AppendLine($"Assembly Name: {compilation.AssemblyName}");
            sb.AppendLine($"Language: {compilation.Language}");
            sb.AppendLine($"Output Kind: {compilation.Options.OutputKind}");
            // sb.AppendLine($"Is Main Assembly: {compilation.IsMainCompilation}");
            // sb.AppendLine($"Is Script: {compilation.IsScript}");
            // sb.AppendLine($"Is Submission: {compilation.IsSubmission}");

            // Attempt to infer framework, though not always precise from Compilation alone
            var systemRuntimeAssembly = compilation.References
                .Select(r => compilation.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol)
                .FirstOrDefault(a => a?.Name == "System.Runtime");

            if (systemRuntimeAssembly != null)
            {
                sb.AppendLine();
                sb.AppendLine($"--- Inferred Framework ---");
                sb.AppendLine($"System.Runtime Version: {systemRuntimeAssembly.Identity.Version}");
                // More sophisticated framework inference would involve checking well-known types
                // or parsing target framework monikers from project files, which is outside
                // the scope of what's directly available from a Compilation object.
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine($"--- Inferred Framework ---");
                sb.AppendLine($"Could not reliably infer framework from System.Runtime reference.");
            }

            sb.AppendLine();
            sb.AppendLine("--- References ---");
            foreach (var reference in compilation.References)
            {
                sb.AppendLine($"  Display: {reference.Display}");
                if (reference is PortableExecutableReference peReference)
                {
                    sb.AppendLine($"    Path: {peReference.FilePath}");
                }
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol != null)
                {
                    sb.AppendLine($"    Identity Name: {assemblySymbol.Identity.Name}");
                    sb.AppendLine($"    Identity Version: {assemblySymbol.Identity.Version}");
                    sb.AppendLine($"    Identity Culture: {assemblySymbol.Identity.CultureName}");
                    sb.AppendLine($"    Identity Public Key Token: {string.Join("", assemblySymbol.Identity.PublicKeyToken.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)))}");
                }
            }

            return sb.ToCommentString();
        }

        /// <summary>
        /// Generates debug information about the assembly containing a given type symbol.
        /// </summary>
        /// <param name="typeSymbol">The type symbol whose containing assembly is to be analyzed.</param>
        /// <returns>A string containing formatted debug information about the assembly.</returns>
        public static string GenerateTypeAssemblyDebugInfo(ITypeSymbol typeSymbol)
        {
            var sb = new StringBuilder();

            if (typeSymbol == null)
            {
                sb.AppendLine("Type Symbol is null.");
                return sb.ToString();
            }

            var containingAssembly = typeSymbol.ContainingAssembly;
            if (containingAssembly == null)
            {
                sb.AppendLine($"Type {typeSymbol.ToDisplayString()} does not have a containing assembly.");
                return sb.ToString();
            }

            sb.AppendLine($"--- Assembly Info for {typeSymbol.ToDisplayString()} ---");
            sb.Append($"  Name: {containingAssembly.Identity.Name}");
            sb.Append($"  Version: {containingAssembly.Identity.Version}");
            sb.Append($"  Culture: {containingAssembly.Identity.CultureName}");
            sb.Append($"  Public Key Token: {string.Join("", containingAssembly.Identity.PublicKeyToken.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)))}");
            sb.AppendLine();

            if (containingAssembly.Locations.Any())
            {
                if (containingAssembly is PortableExecutableReference peRef)
                {
                    sb.AppendLine($"  File Path: {peRef.FilePath}");
                }
                else
                {
                    sb.AppendLine($"  Location: {containingAssembly.Locations.FirstOrDefault()?.GetLineSpan().Path}");
                }
            }
            else
            {
                sb.AppendLine($"  No physical location found for assembly.");
            }

            return sb.ToCommentString();
        }

    }
}