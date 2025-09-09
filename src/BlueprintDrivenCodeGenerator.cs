using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Generates mock implementation code from complete MockClassBlueprint.
    /// This eliminates case-by-case generation issues by working from pre-resolved blueprint.
    /// </summary>
    public class BlueprintDrivenCodeGenerator
    {
        private static readonly string EnvironmentNewLine = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\r\n" : "\n";

        /// <summary>
        /// Generates complete mock implementation from blueprint.
        /// All conflicts and strategies are already resolved in the blueprint.
        /// </summary>
        public string GenerateMockImplementation(MockClassBlueprint blueprint)
        {
            var sb = new StringBuilder();

            // // Generate using directives
            // GenerateUsingDirectives(sb, BlueprintHelpers.GetUsingDirectives(blueprint));

            // Add nullable directive for generated code
            sb.AppendLine("#nullable enable");

#if DEBUG
            sb.AppendLine("#pragma warning disable IDE0079  // Remove unnecessary suppression");

            sb.AppendLine("#pragma warning disable IDE0018  // Inline variable declaration");
            sb.AppendLine("#pragma warning disable IDE0031  // Use null propagation");
            sb.AppendLine("#pragma warning disable IDE0075  // Simplify conditional expression");
            sb.AppendLine("#pragma warning disable IDE0130  // Namespace does not match folder structure");
            sb.AppendLine("#pragma warning disable IDE0250  // Struct can be made 'readonly'");
            sb.AppendLine("#pragma warning disable IDE0251  // Member can be made 'readonly'");
            sb.AppendLine("#pragma warning disable IDE0290  // Use primary constructor");
            sb.AppendLine("#pragma warning disable IDE1005  // Use conditional delegate call");
            sb.AppendLine("#pragma warning disable SMA0031  // Mutable Struct Field marked as Read-Only");
            sb.AppendLine("#pragma warning disable SMA0040  // Missing Using Statement");
#endif
            sb.AppendLine();

            // Generate namespace
            if (!string.IsNullOrEmpty(BlueprintHelpers.GetMockNamespace(blueprint)))
            {
                sb.AppendLine($"namespace {BlueprintHelpers.GetMockNamespace(blueprint)}");
                sb.AppendLine("{");
            }

            // Generate class declaration
            GenerateClassDeclaration(sb, blueprint);

            // Generate constructor
            GenerateConstructor(sb, blueprint);

            // Generate MockTarget property
            GenerateMockTargetProperty(sb, blueprint);

            // Generate MockOverrides property
            GenerateMockOverridesProperty(sb);

            // Generate all members
            foreach (var member in blueprint.Members)
            {
                GenerateUnifiedMember(sb, member, blueprint);
            }

            // Generate MockOverrideContainer
            GenerateMockOverrideContainer(sb, blueprint);

            // Record and record struct need to implement IEquatable<MOCK_TARGET_TYPE>
            if (blueprint.IsRecord || blueprint.IsRecordStruct)
            {
                var mockTargetType = blueprint.TargetTypeSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                var nullableSuffix = blueprint.IsRecordStruct ? string.Empty : "?";

                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// Override for auto implemented record method.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        bool {Constants.IEquatableFullName}<{mockTargetType}>.Equals({mockTargetType}{nullableSuffix} other)");
                sb.AppendLine("        {");
                sb.AppendLine($"            return MockOverrides.{Constants.RecordIEquatableEqualsOverrideName}?.Invoke(other) ?? throw new TDoubles.TDoublesException(\"Equals\", \"{Constants.IEquatableFullName}<{mockTargetType}>\");");
                sb.AppendLine("        }");
            }

            // Unified callback declaration
            // NOTE: DO NOT use `params` modifier for args
            //       It may cause unexpected result when only an argument that is object[]
            {
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// The unified callback invoked before each mock call.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"memberName\">The short name of original member without generic type parameters.</param>");
                sb.AppendLine($"        partial void {Constants.CallbackNameBefore}(string memberName);");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// The unified callback invoked before each mock call.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"memberName\">The short name of original member without generic type parameters.</param>");
                sb.AppendLine($"        partial void {Constants.CallbackNameBefore}(string memberName, object?[] args);");
            }

            // Close class
            sb.AppendLine("    }");

            // Close namespace
            if (!string.IsNullOrEmpty(BlueprintHelpers.GetMockNamespace(blueprint)))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates class declaration based on inheritance strategy.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the class declaration to.</param>
        /// <param name="bp">The MockClassBlueprint containing information for class generation.</param>
        /// <returns>The generated class declaration string.</returns>
        public string GenerateClassDeclaration(StringBuilder sb, MockClassBlueprint bp)
        {
            var classDeclaration = new StringBuilder();

            var mockDeclarationKind = bp.IsRecordStruct
                                        ? "record struct"
                                        : bp.IsRecord
                                            ? "record"
                                            : "class";

            if (bp.ExcludeMemberShortNames.Count > 0)
            {
                classDeclaration.AppendLine("    /// <summary>");
                classDeclaration.AppendLine($"    /// Excluded members: '{string.Join("', '", bp.ExcludeMemberShortNames)}'");
                classDeclaration.AppendLine("    /// </summary>");
            }

            classDeclaration.Append($"    partial {mockDeclarationKind} {BlueprintHelpers.GetMockClassName(bp)}");

            // Add generic type parameters if needed
            if (BlueprintHelpers.GetMockClassTypeParameters(bp.TypeMapping).Count > 0)
            {
                var typeParams = string.Join(", ", BlueprintHelpers.GetMockClassTypeParameters(bp.TypeMapping));
                classDeclaration.Append($"<{typeParams}>");
            }

            // Add inheritance and interfaces
            var inheritanceList = new List<string>();

            // Add base class if inheriting
            if (bp.InheritanceStrategy.ShouldInherit && bp.BaseTypeSymbol != null)
            {
                var baseClassName = GetBaseClassNameForDeclaration(bp);
                inheritanceList.Add(baseClassName);
            }

            // Add ALL required interfaces (per requirements - NO EXCEPTIONS)
            foreach (var interfaceBp in bp.InterfaceImplementations)
            {
                var interfaceName = GetInterfaceNameForDeclaration(interfaceBp, bp);
                inheritanceList.Add(interfaceName);
            }

            if (inheritanceList.Count > 0)
            {
                classDeclaration.AppendLine();
                classDeclaration.Append($"        : {string.Join(EnvironmentNewLine + "        , ", inheritanceList)}");
            }

            // Add generic type constraints
            foreach (var typeParam in bp.TypeMapping.MockClassTypeParameterSymbols)
            {
                var targetTypeParam = bp.TypeMapping.TypeParameterSymbolMap.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Value, typeParam)).Key;
                if (targetTypeParam != null)
                {
                    var whereClause = SymbolHelpers.GenerateWhereClause(targetTypeParam, typeParam.Name);
                    if (whereClause != null)
                    {
                        if (bp.TypeMapping.ShouldApplyMapping())
                        {
                            whereClause = bp.TypeMapping.ApplyTypeMappingToString(whereClause);
                        }

                        classDeclaration.AppendLine();
                        classDeclaration.Append("        ");
                        classDeclaration.Append(whereClause);
                    }
                }
            }

            sb.AppendLine(classDeclaration.ToString());
            sb.AppendLine("    {");

            return classDeclaration.ToString();
        }

        /// <summary>
        /// Gets the base class name for class declaration, applying generic mapping if needed.
        /// </summary>
        private string GetBaseClassNameForDeclaration(MockClassBlueprint bp)
        {
            if (bp.BaseTypeSymbol == null)
                return string.Empty;

            var baseTypeName = bp.BaseTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);

            // Apply generic mapping if available
            if (bp.TypeMapping.ShouldApplyMapping())
            {
                return bp.TypeMapping.ApplyTypeMappingToString(baseTypeName);
            }

            return baseTypeName;
        }

        /// <summary>
        /// Gets the interface name for class declaration, applying generic mapping if needed.
        /// </summary>
        private string GetInterfaceNameForDeclaration(InterfaceImplementationBlueprint interfaceBp, MockClassBlueprint classBp)
        {
            if (interfaceBp.InterfaceType == null)
                return string.Empty;

            var interfaceTypeName = interfaceBp.InterfaceType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);

            // Apply generic mapping if available
            if (classBp.TypeMapping.ShouldApplyMapping())
            {
                return classBp.TypeMapping.ApplyTypeMappingToString(interfaceTypeName);
            }

            return interfaceTypeName;
        }

        /// <summary>
        /// Generates constructor based on blueprint's generic type mapping.
        /// </summary>
        public string GenerateConstructor(StringBuilder sb, MockClassBlueprint bp)
        {
            var targetTypeName = GetConstructorParameterType(bp);

            sb.AppendLine($"        public {BlueprintHelpers.GetMockClassName(bp)}({targetTypeName}? target = default)");
            sb.AppendLine("        {");
            sb.AppendLine($"            {Constants.MockTargetInstanceFieldName} = target;");
            sb.AppendLine("            MockOverrides = new MockOverrideContainer();");
            sb.AppendLine("        }");
            sb.AppendLine();

            return targetTypeName;
        }

        /// <summary>
        /// Generates MockTarget property.
        /// </summary>
        public string GenerateMockTargetProperty(StringBuilder sb, MockClassBlueprint bp)
        {
            var targetTypeName = GetConstructorParameterType(bp);

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets the underlying target instance being mocked.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {targetTypeName}? MockTarget => {Constants.MockTargetInstanceFieldName};");
            sb.AppendLine();
            sb.AppendLine($"        private readonly {targetTypeName}? {Constants.MockTargetInstanceFieldName};");
            sb.AppendLine();

            return targetTypeName;
        }

        /// <summary>
        /// Generates MockOverrides property.
        /// </summary>
        public string GenerateMockOverridesProperty(StringBuilder sb)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets the container for method and property overrides.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public MockOverrideContainer MockOverrides { get; }");
            sb.AppendLine();

            return "MockOverrideContainer";
        }

        /// <summary>
        /// Generates member implementation from unified blueprint.
        /// </summary>
        public string GenerateUnifiedMember(StringBuilder sb, UnifiedMemberBlueprint memberBp, MockClassBlueprint classBp)
        {
            if (memberBp.IsExplicitInterfaceImplementation)
            {
                return GenerateExplicitInterfaceImplementation(sb, memberBp, classBp);
            }

            switch (memberBp.MemberType)
            {
                case MemberType.Method:
                    return GenerateMethod(sb, memberBp, classBp);
                case MemberType.Property:
                    return GenerateProperty(sb, memberBp, classBp);
                case MemberType.Indexer:
                    return GenerateIndexer(sb, memberBp, classBp);
                case MemberType.Event:
                    return GenerateEvent(sb, memberBp, classBp);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Generates explicit interface implementation using INTERFACE.member syntax.
        /// </summary>
        public string GenerateExplicitInterfaceImplementation(StringBuilder sb, UnifiedMemberBlueprint memberBp, MockClassBlueprint classBp)
        {
            if (!memberBp.IsExplicitInterfaceImplementation)
            {
                return string.Empty;
            }

            switch (memberBp.MemberType)
            {
                case MemberType.Method:
                    return GenerateExplicitInterfaceMethod(sb, memberBp, classBp);
                case MemberType.Property:
                    return GenerateExplicitInterfaceProperty(sb, memberBp, classBp);
                case MemberType.Indexer:
                    return GenerateExplicitInterfaceIndexer(sb, memberBp, classBp);
                case MemberType.Event:
                    return GenerateExplicitInterfaceEvent(sb, memberBp, classBp);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Generates MockOverrideContainer from blueprint.
        /// </summary>
        public string GenerateMockOverrideContainer(StringBuilder sb, MockClassBlueprint bp)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Container for method and property overrides.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public sealed class MockOverrideContainer");
            sb.AppendLine("        {");

            // Generate override properties for all members
            foreach (var member in bp.Members)
            {
                GenerateOverrideProperties(sb, member, bp);
            }

            // Record and record struct need to implement IEquatable<MOCK_TARGET_TYPE>
            if (bp.IsRecord || bp.IsRecordStruct)
            {
                var mockTargetType = bp.TargetTypeSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                var nullableSuffix = bp.IsRecordStruct ? string.Empty : "?";

                sb.AppendLine("            /// <summary>");
                sb.AppendLine("            /// Override for compiler-generated record method.");
                sb.AppendLine("            /// </summary>");
                sb.AppendLine($"            public {Constants.FuncFullName}<{mockTargetType}{nullableSuffix}, bool>? {Constants.RecordIEquatableEqualsOverrideName} {{ get; set; }}");
            }

            sb.AppendLine("        }");

            return "MockOverrideContainer";
        }

        /// <summary>
        /// Generates target casting based on member's containing type.
        /// </summary>
        public string GenerateTargetCasting(UnifiedMemberBlueprint memberBp, MockClassBlueprint classBp, string targetVariable = Constants.MockTargetInstanceFieldName)
        {
            if (classBp.IsRecordStruct)
            {
                targetVariable += ".Value";
            }

            if (memberBp.ContainingType == null)
                return targetVariable;

            // For explicit interface implementations, cast to the explicit interface type
            if (memberBp.IsExplicitInterfaceImplementation && memberBp.ExplicitInterfaceType != null)
            {
                var ifaceName = memberBp.ExplicitInterfaceType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                if (memberBp.GenericMapping.ShouldApplyMapping())
                {
                    ifaceName = memberBp.GenericMapping.ApplyTypeMappingToString(ifaceName);
                }

                return $"(({ifaceName}?){targetVariable})!";  // Append '!' to avoid warning
            }

            // Use unified blueprint for containing type casting
            var containingTypeName = BlueprintHelpers.ToContainingTypeString(memberBp);
            return $"(({containingTypeName}?){targetVariable})!";  // Append '!' to avoid warning
        }

        /// <summary>
        /// Generates return value handling based on blueprint.
        /// </summary>
        /// <param name="memberBp">The UnifiedMemberBlueprint for the member.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated return value handling code.</returns>
        public string GenerateReturnValueHandling(UnifiedMemberBlueprint memberBp, MockClassBlueprint classBp)
        {
            if (memberBp.IsVoid)
                return string.Empty; // Void methods don't need return value handling

            var fallback = memberBp.ReturnStrategy switch
            {
                ReturnValueStrategy.Default => "default",
                ReturnValueStrategy.NullableDefault => "default",
                ReturnValueStrategy.NewInstance => "new()",
                ReturnValueStrategy.ThrowException => GenerateTDoublesExceptionThrow(memberBp),
                _ => GenerateTDoublesExceptionThrow(memberBp)
            };

            // Do not alter fallback just for type parameters here; fail-first is preserved.

            // Generate delegation expression based on member type using symbol-based approach
            var delegationExpression = memberBp.MemberType switch
            {
                MemberType.Method => GenerateMethodDelegationExpression(memberBp, classBp, fallback),
                MemberType.Property => GeneratePropertyGetterDelegationExpression(memberBp, classBp, fallback),
                MemberType.Indexer => GenerateIndexerGetterDelegationExpression(memberBp, classBp, fallback),
                MemberType.Event => string.Empty, // Events do not return values
                _ => fallback
            };

            // Use unified blueprint for return type casting
            var returnType = BlueprintHelpers.ToReturnTypeString(memberBp);
            return $"({returnType})({delegationExpression})";
        }

        /// <summary>
        /// Generates method delegation expression using symbol-based approach.
        /// </summary>
        private string GenerateMethodDelegationExpression(UnifiedMemberBlueprint method, MockClassBlueprint classBp, string fallback)
        {
            var targetCasting = GenerateTargetCasting(method, classBp);
            var parameterList = string.Join(", ", method.Parameters.Select(p => BlueprintHelpers.ToParameterInvocationString(p)));
            var resolved = BlueprintHelpers.GetResolvedName(method, classBp);
            var targetCall = $"{targetCasting}.{BlueprintHelpers.GetOriginalNameWithTypeArgs(method)}({parameterList})";
            // return $"(MockOverrides.{resolved} != null ? MockOverrides.{resolved}.Invoke({parameterList}) : ({Constants.MockTargetInstanceFieldName} != null ? {targetCall} : {fallback}))";
            return $"(MockOverrides.{resolved} != null ? {Constants.TempReturnVariableName} : ({Constants.MockTargetInstanceFieldName} != null ? {targetCall} : {fallback}))";
        }

        /// <summary>
        /// Generates property getter delegation expression using symbol-based approach.
        /// </summary>
        private string GeneratePropertyGetterDelegationExpression(UnifiedMemberBlueprint property, MockClassBlueprint classBp, string fallback)
        {
            var targetCasting = GenerateTargetCasting(property, classBp);
            var resolved = BlueprintHelpers.GetResolvedName(property, classBp);
            var targetAccess = $"{targetCasting}.{BlueprintHelpers.GetOriginalName(property)}";
            return $"(MockOverrides.{resolved}{Constants.GetterSuffix} != null ? MockOverrides.{resolved}{Constants.GetterSuffix}.Invoke() : ({Constants.MockTargetInstanceFieldName} != null ? {targetAccess} : {fallback}))";
        }

        /// <summary>
        /// Generates indexer getter delegation expression using symbol-based approach.
        /// </summary>
        private string GenerateIndexerGetterDelegationExpression(UnifiedMemberBlueprint indexer, MockClassBlueprint classBp, string fallback)
        {
            var targetCasting = GenerateTargetCasting(indexer, classBp);
            var parameterList = string.Join(", ", indexer.Parameters.Select(p => BlueprintHelpers.ToParameterInvocationString(p)));
            var resolved = BlueprintHelpers.GetResolvedName(indexer, classBp);
            return $"(MockOverrides.{resolved}{Constants.GetterSuffix} != null ? MockOverrides.{resolved}{Constants.GetterSuffix}.Invoke({parameterList}) : ({Constants.MockTargetInstanceFieldName} != null ? {targetCasting}[{parameterList}] : {fallback}))";
        }

        // Private implementation methods

        /// <summary>
        /// Generates using directives from namespace symbols.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="namespaceSymbols">The namespace symbols to generate using directives for.</param>
        private void GenerateUsingDirectives(StringBuilder sb, IEnumerable<INamespaceSymbol> namespaceSymbols)
        {
            var usingDirectives = namespaceSymbols
                .Where(ns => ns != null && !ns.IsGlobalNamespace)
                .Select(ns => ns.ToDisplayString())
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Distinct()
                .OrderBy(ns => ns);

            foreach (var usingDirective in usingDirectives)
            {
                sb.AppendLine($"using {usingDirective};");
            }
            sb.AppendLine();
        }

        /// <summary>
        /// Gets the type name for the constructor parameter, applying generic mapping if needed.
        /// </summary>
        /// <param name="bp">The MockClassBlueprint.</param>
        /// <returns>The type name string for the constructor parameter.</returns>
        private string GetConstructorParameterType(MockClassBlueprint bp)
        {
            var targetTypeName = bp.TargetTypeSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat) ?? "object";

            // Apply generic mapping if available
            if (bp.TypeMapping.ShouldApplyMapping())
            {
                return bp.TypeMapping.ApplyTypeMappingToString(targetTypeName);
            }

            return targetTypeName;
        }

        /// <summary>
        /// Generates the code for a method implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the method code to.</param>
        /// <param name="method">The UnifiedMemberBlueprint for the method.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated method signature string.</returns>
        private string GenerateMethod(StringBuilder sb, UnifiedMemberBlueprint method, MockClassBlueprint classBp)
        {
            // Generate method signature using unified blueprint for all type conversions
            var signature = new StringBuilder();
            signature.Append($"        {BlueprintHelpers.GetAccessibility(method)}");

            var modifierString = GetModifierString(method.MemberModifier);
            if (!string.IsNullOrEmpty(modifierString))
            {
                signature.Append($" {modifierString}");
            }

            // Use unified blueprint for return type conversion
            var returnType = BlueprintHelpers.ToReturnTypeString(method);
            signature.Append($" {returnType} {BlueprintHelpers.GetOriginalName(method)}");

            // Add generic type parameters
            if (method.IsGeneric && BlueprintHelpers.GetGenericTypeParameters(method).Count > 0)
            {
                signature.Append($"<{string.Join(", ", BlueprintHelpers.GetGenericTypeParameters(method))}>");
            }

            // Add parameters using unified blueprint for all type conversions
            var parameters = method.Parameters.Select((_, i) => BlueprintHelpers.ToParameterDeclarationString(method, i));
            signature.Append($"({string.Join(", ", parameters)})");

            // Add generic constraints
            var genericConstraints = BlueprintHelpers.GetGenericConstraints(method);
            if (genericConstraints?.Count > 0)
            {
                foreach (var genericConstraint in genericConstraints)
                {
                    // C# doesn't allow using type constraint for override methods
                    if (method.MemberModifier == MemberModifier.Override)
                    {
                        // 'class' is allowed but 'class?' is not
                        if (!genericConstraint.EndsWith(" : class", StringComparison.Ordinal) &&
                            !genericConstraint.EndsWith(" : struct", StringComparison.Ordinal))
                        {
                            continue;
                        }
                    }

                    signature.AppendLine();
                    signature.Append("            ");
                    signature.Append(genericConstraint);
                }
            }

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");

            // Generate method body
            GenerateMethodBody(sb, method, classBp);

            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the code for a property implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the property code to.</param>
        /// <param name="property">The UnifiedMemberBlueprint for the property.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated property signature string.</returns>
        private string GenerateProperty(StringBuilder sb, UnifiedMemberBlueprint property, MockClassBlueprint classBp)
        {
            // Generate property signature using unified blueprint for type conversion
            var signature = new StringBuilder();
            signature.Append($"        {BlueprintHelpers.GetAccessibility(property)}");

            var modifierString = GetModifierString(property.MemberModifier);
            if (!string.IsNullOrEmpty(modifierString))
            {
                signature.Append($" {modifierString}");
            }

            // Use unified blueprint for return type conversion
            var returnType = BlueprintHelpers.ToReturnTypeString(property);
            signature.Append($" {returnType} {BlueprintHelpers.GetOriginalName(property)}");

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");

            // Generate getter
            if (property.HasGetter)
            {
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.GetterSuffix, property)}");
                sb.AppendLine();
                GeneratePropertyGetterBody(sb, property, classBp);
                sb.AppendLine("            }");
            }

            // Generate setter
            if (property.HasSetter)
            {
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.SetterSuffix, property)}");
                sb.AppendLine();
                GeneratePropertySetterBody(sb, property, classBp);
                sb.AppendLine("            }");
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the code for an indexer implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the indexer code to.</param>
        /// <param name="indexer">The UnifiedMemberBlueprint for the indexer.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated indexer signature string.</returns>
        private string GenerateIndexer(StringBuilder sb, UnifiedMemberBlueprint indexer, MockClassBlueprint classBp)
        {
            // Generate indexer signature using unified blueprint for type conversion
            var signature = new StringBuilder();
            signature.Append($"        {BlueprintHelpers.GetAccessibility(indexer)}");

            var modifierString = GetModifierString(indexer.MemberModifier);
            if (!string.IsNullOrEmpty(modifierString))
            {
                signature.Append($" {modifierString}");
            }

            // Use unified blueprint for return type conversion
            var returnType = BlueprintHelpers.ToReturnTypeString(indexer);
            signature.Append($" {returnType} this");

            // Add indexer parameters using unified blueprint for type conversion
            var parameters = indexer.Parameters.Select((_, i) => BlueprintHelpers.ToParameterDeclarationString(indexer, i));
            signature.Append($"[{string.Join(", ", parameters)}]");

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");

            // Generate getter
            if (indexer.HasGetter)
            {
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.GetterSuffix, indexer)}");
                sb.AppendLine();
                GenerateIndexerGetterBody(sb, indexer, classBp);
                sb.AppendLine("            }");
            }

            // Generate setter
            if (indexer.HasSetter)
            {
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.SetterSuffix, indexer)}");
                sb.AppendLine();
                GenerateIndexerSetterBody(sb, indexer, classBp);
                sb.AppendLine("            }");
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the code for an event implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the event code to.</param>
        /// <param name="eventBp">The UnifiedMemberBlueprint for the event.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated event signature string.</returns>
        private string GenerateEvent(StringBuilder sb, UnifiedMemberBlueprint eventBp, MockClassBlueprint classBp)
        {
            // Generate event signature
            var signature = new StringBuilder();
            signature.Append($"        {BlueprintHelpers.GetAccessibility(eventBp)}");

            var modifierString = GetModifierString(eventBp.MemberModifier);
            if (!string.IsNullOrEmpty(modifierString))
            {
                signature.Append($" {modifierString}");
            }

            var eventType = BlueprintHelpers.ToReturnTypeString(eventBp);
            var eventName = BlueprintHelpers.GetOriginalName(eventBp);
            var resolvedName = BlueprintHelpers.GetResolvedName(eventBp, classBp);
            var targetCasting = GenerateTargetCasting(eventBp, classBp);

            signature.Append($" event {eventType} {eventName}");

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");
            // add accessor
            sb.AppendLine("            add");
            sb.AppendLine("            {");
            // Unified callback
            sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.AdderSuffix, eventBp)}");
            sb.AppendLine();
            sb.AppendLine($"                if (MockOverrides.{resolvedName}{Constants.AdderSuffix} != null) MockOverrides.{resolvedName}{Constants.AdderSuffix}.Invoke(value);");
            sb.AppendLine($"                else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}.{eventName} += value;");
            sb.AppendLine($"                else {GenerateTDoublesExceptionThrow(eventBp)};");
            sb.AppendLine("            }");
            // remove accessor
            sb.AppendLine("            remove");
            sb.AppendLine("            {");
            // Unified callback
            sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.RemoverSuffix, eventBp)}");
            sb.AppendLine();
            sb.AppendLine($"                if (MockOverrides.{resolvedName}{Constants.RemoverSuffix} != null) MockOverrides.{resolvedName}{Constants.RemoverSuffix}.Invoke(value);");
            sb.AppendLine($"                else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}.{eventName} -= value;");
            sb.AppendLine($"                else {GenerateTDoublesExceptionThrow(eventBp)};");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the code for an explicit interface method implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the method code to.</param>
        /// <param name="method">The UnifiedMemberBlueprint for the method.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated method signature string.</returns>
        private string GenerateExplicitInterfaceMethod(StringBuilder sb, UnifiedMemberBlueprint method, MockClassBlueprint classBp)
        {
            // Generate explicit interface method signature
            var signature = new StringBuilder();
            signature.Append($"        {BlueprintHelpers.ToReturnTypeString(method)} {BlueprintHelpers.GetExplicitImplementationName(method)}");

            // Add generic type parameters
            if (method.IsGeneric && BlueprintHelpers.GetGenericTypeParameters(method).Count > 0)
            {
                signature.Append($"<{string.Join(", ", BlueprintHelpers.GetGenericTypeParameters(method))}>");
            }

            // Add parameters (explicit interface implementations use fully qualified parameter declarations)
            var parameters = method.Parameters.Select((_, i) => BlueprintHelpers.ToParameterDeclarationString(method, i));
            signature.Append($"({string.Join(", ", parameters)})");

            // Add generic constraints
            var genericConstraints = BlueprintHelpers.GetGenericConstraints(method);
            if (genericConstraints?.Count > 0)
            {
                foreach (var genericConstraint in genericConstraints)
                {
                    signature.AppendLine();
                    signature.Append("            ");
                    signature.Append(genericConstraint);
                }
            }

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");

            // Generate method body
            GenerateMethodBody(sb, method, classBp);

            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the code for an explicit interface property implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the property code to.</param>
        /// <param name="property">The UnifiedMemberBlueprint for the property.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated property signature string.</returns>
        private string GenerateExplicitInterfaceProperty(StringBuilder sb, UnifiedMemberBlueprint property, MockClassBlueprint classBp)
        {
            // Generate explicit interface property signature
            var signature = $"        {BlueprintHelpers.ToReturnTypeString(property)} {BlueprintHelpers.GetExplicitImplementationName(property)}";

            sb.AppendLine(signature);
            sb.AppendLine("        {");

            // Generate getter
            if (property.HasGetter)
            {
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.GetterSuffix, property)}");
                sb.AppendLine();
                GeneratePropertyGetterBody(sb, property, classBp);
                sb.AppendLine("            }");
            }

            // Generate setter
            if (property.HasSetter)
            {
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.SetterSuffix, property)}");
                sb.AppendLine();
                GeneratePropertySetterBody(sb, property, classBp);
                sb.AppendLine("            }");
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            return signature;
        }

        /// <summary>
        /// Generates the code for an explicit interface indexer implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the indexer code to.</param>
        /// <param name="indexer">The UnifiedMemberBlueprint for the indexer.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated indexer signature string.</returns>
        private string GenerateExplicitInterfaceIndexer(StringBuilder sb, UnifiedMemberBlueprint indexer, MockClassBlueprint classBp)
        {
            // Generate explicit interface indexer signature (INTERFACE.this[...])
            var signature = new StringBuilder();
            signature.Append($"        {BlueprintHelpers.ToReturnTypeString(indexer)} {BlueprintHelpers.GetExplicitImplementationName(indexer)}");

            // Add indexer parameters
            var parameters = indexer.Parameters.Select((_, i) => BlueprintHelpers.ToParameterDeclarationString(indexer, i));
            signature.Append($"[{string.Join(", ", parameters)}]");

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");

            // Generate getter
            if (indexer.HasGetter)
            {
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.GetterSuffix, indexer)}");
                sb.AppendLine();
                GenerateIndexerGetterBody(sb, indexer, classBp);
                sb.AppendLine("            }");
            }

            // Generate setter
            if (indexer.HasSetter)
            {
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                // Unified callback
                sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.SetterSuffix, indexer)}");
                sb.AppendLine();
                GenerateIndexerSetterBody(sb, indexer, classBp);
                sb.AppendLine("            }");
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the code for an explicit interface event implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the event code to.</param>
        /// <param name="eventBp">The UnifiedMemberBlueprint for the event.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <returns>The generated event signature string.</returns>
        private string GenerateExplicitInterfaceEvent(StringBuilder sb, UnifiedMemberBlueprint eventBp, MockClassBlueprint classBp)
        {
            // Generate explicit interface event signature
            var signature = new StringBuilder();
            signature.Append($"        event {BlueprintHelpers.ToReturnTypeString(eventBp)} {BlueprintHelpers.GetExplicitImplementationName(eventBp)}");

            var resolvedName = BlueprintHelpers.GetResolvedName(eventBp, classBp);
            var targetCasting = GenerateTargetCasting(eventBp, classBp);
            var eventName = BlueprintHelpers.GetOriginalName(eventBp);

            sb.AppendLine(signature.ToString());
            sb.AppendLine("        {");
            // add accessor
            sb.AppendLine("            add");
            sb.AppendLine("            {");
            // Unified callback
            sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.AdderSuffix, eventBp)}");
            sb.AppendLine();
            sb.AppendLine($"                if (MockOverrides.{resolvedName}{Constants.AdderSuffix} != null) MockOverrides.{resolvedName}{Constants.AdderSuffix}.Invoke(value);");
            sb.AppendLine($"                else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}.{eventName} += value;");
            sb.AppendLine($"                else {GenerateTDoublesExceptionThrow(eventBp)};");
            sb.AppendLine("            }");
            // remove accessor
            sb.AppendLine("            remove");
            sb.AppendLine("            {");
            // Unified callback
            sb.AppendLine($"                {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, Constants.RemoverSuffix, eventBp)}");
            sb.AppendLine();
            sb.AppendLine($"                if (MockOverrides.{resolvedName}{Constants.RemoverSuffix} != null) MockOverrides.{resolvedName}{Constants.RemoverSuffix}.Invoke(value);");
            sb.AppendLine($"                else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}.{eventName} -= value;");
            sb.AppendLine($"                else {GenerateTDoublesExceptionThrow(eventBp)};");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            return signature.ToString();
        }

        /// <summary>
        /// Generates the body of a method implementation.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the method body to.</param>
        /// <param name="method">The UnifiedMemberBlueprint for the method.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        private void GenerateMethodBody(StringBuilder sb, UnifiedMemberBlueprint method, MockClassBlueprint classBp)
        {
            var targetCasting = GenerateTargetCasting(method, classBp);
            var parameterList = string.Join(", ", method.Parameters.Select(p =>
            {
                return (p.IsRef || p.IsOut)
                    ? BlueprintHelpers.ToParameterInvocationString(p, Constants.TempVariablePrefix)
                    : BlueprintHelpers.ToParameterInvocationString(p);
            }));

            // Initialize out parameters
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var param = method.Parameters[i];
                if (param.IsOut)
                {
                    sb.AppendLine($"            {BlueprintHelpers.GetParameterName(param)} = default!;");
                }
            }

            // Unified callback
            sb.AppendLine($"            {GenerateMockCallbackInvocation(Constants.CallbackNameBefore, null, method)}");
            sb.AppendLine();

            // Declare temporary out/ref parameter variables
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var param = method.Parameters[i];
                if (param.IsOut)
                {
                    sb.AppendLine($"            {BlueprintHelpers.ToOverridePropertyParameterTypeString(method, i)} {Constants.TempVariablePrefix}{BlueprintHelpers.GetParameterName(param)};");
                }
                else if (param.IsRef)
                {
                    sb.AppendLine($"            {BlueprintHelpers.ToOverridePropertyParameterTypeString(method, i)} {Constants.TempVariablePrefix}{BlueprintHelpers.GetParameterName(param)} = {BlueprintHelpers.GetParameterName(param)};");
                }
            }

            // Invoke method with ref/out-aware parameter list
            var resolved = BlueprintHelpers.GetResolvedName(method, classBp);
            if (!method.IsVoid)
            {
                sb.AppendLine($"            {BlueprintHelpers.ToReturnTypeString(method)} {Constants.TempReturnVariableName} = default!;");
            }
            sb.AppendLine($"            if (MockOverrides.{resolved} != null) {{");
            {
                if (method.IsVoid)
                {
                    sb.AppendLine($"                MockOverrides.{resolved}.Invoke({parameterList});");
                }
                else
                {
                    sb.AppendLine($"                {Constants.TempReturnVariableName} = ({BlueprintHelpers.ToReturnTypeString(method)})MockOverrides.{resolved}.Invoke({parameterList});");
                }

                // Cast temp receivers
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var param = method.Parameters[i];
                    if (!param.IsRef && !param.IsOut) continue;
                    sb.AppendLine($"                {BlueprintHelpers.GetParameterName(param)} = ({BlueprintHelpers.ToParameterTypeString(method, i)}){Constants.TempVariablePrefix}{BlueprintHelpers.GetParameterName(param)};");
                }
            }
            sb.AppendLine($"            }}");

            sb.AppendLine();

            if (method.IsVoid)
            {
                // Void method delegation - use null-coalescing on the Action delegates, then invoke
                sb.AppendLine($"            if (MockOverrides.{resolved} != null) {{ }}");
                sb.AppendLine($"            else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}.{BlueprintHelpers.GetOriginalName(method)}({parameterList});");
                if (method.Parameters.Any(p => p.IsOut))
                {
                    sb.AppendLine($"            else {GenerateTDoublesExceptionThrow(method)};");
                }
            }
            else
            {
                // Non-void method delegation with return value handling
                // Delegation expression is now generated internally by GenerateReturnValueHandling
                var returnExpression = GenerateReturnValueHandling(method, classBp);
                sb.AppendLine($"            return {returnExpression};");
            }
        }

        /// <summary>
        /// Generates the body of a property getter.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the getter body to.</param>
        /// <param name="property">The UnifiedMemberBlueprint for the property.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        private void GeneratePropertyGetterBody(StringBuilder sb, UnifiedMemberBlueprint property, MockClassBlueprint classBp)
        {
            //var targetCasting = GenerateTargetCasting(property, classBp);

            // Delegation expression is now generated internally by GenerateReturnValueHandling
            var returnExpression = GenerateReturnValueHandling(property, classBp);
            sb.AppendLine($"                return {returnExpression};");
        }

        /// <summary>
        /// Generates the body of a property setter.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the setter body to.</param>
        /// <param name="property">The UnifiedMemberBlueprint for the property.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        private void GeneratePropertySetterBody(StringBuilder sb, UnifiedMemberBlueprint property, MockClassBlueprint classBp)
        {
            var targetCasting = GenerateTargetCasting(property, classBp);
            sb.AppendLine($"                if (MockOverrides.{BlueprintHelpers.GetResolvedName(property, classBp)}{Constants.SetterSuffix} != null) MockOverrides.{BlueprintHelpers.GetResolvedName(property, classBp)}{Constants.SetterSuffix}.Invoke(value);");
            sb.AppendLine($"                else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}.{BlueprintHelpers.GetOriginalName(property)} = value;");
            sb.AppendLine($"                else {GenerateTDoublesExceptionThrow(property)};");
        }

        /// <summary>
        /// Generates the body of an indexer getter.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the getter body to.</param>
        /// <param name="indexer">The UnifiedMemberBlueprint for the indexer.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        private void GenerateIndexerGetterBody(StringBuilder sb, UnifiedMemberBlueprint indexer, MockClassBlueprint classBp)
        {
            var targetCasting = GenerateTargetCasting(indexer, classBp);
            var parameterList = string.Join(", ", indexer.Parameters.Select(p => BlueprintHelpers.ToParameterInvocationString(p)));

            // Delegation expression is now generated internally by GenerateReturnValueHandling
            var returnExpression = GenerateReturnValueHandling(indexer, classBp);
            sb.AppendLine($"                return {returnExpression};");
        }

        /// <summary>
        /// Generates the body of an indexer setter.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the setter body to.</param>
        /// <param name="indexer">The UnifiedMemberBlueprint for the indexer.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        private void GenerateIndexerSetterBody(StringBuilder sb, UnifiedMemberBlueprint indexer, MockClassBlueprint classBp)
        {
            var targetCasting = GenerateTargetCasting(indexer, classBp);
            var parameterList = string.Join(", ", indexer.Parameters.Select(p => BlueprintHelpers.ToParameterInvocationString(p)));

            var resolved = BlueprintHelpers.GetResolvedName(indexer, classBp);
            sb.AppendLine($"                if (MockOverrides.{resolved}{Constants.SetterSuffix} != null) MockOverrides.{resolved}{Constants.SetterSuffix}.Invoke({parameterList}, value);");
            sb.AppendLine($"                else if ({Constants.MockTargetInstanceFieldName} != null) {targetCasting}[{parameterList}] = value;");
            sb.AppendLine($"                else {GenerateTDoublesExceptionThrow(indexer)};");
        }

        /// <summary>
        /// Generates override properties for a given member within the MockOverrideContainer.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the properties to.</param>
        /// <param name="member">The UnifiedMemberBlueprint for the member.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        internal void GenerateOverrideProperties(StringBuilder sb, UnifiedMemberBlueprint member, MockClassBlueprint classBp)
        {
            switch (member.MemberType)
            {
                case MemberType.Method:
                    GenerateMethodOverrideProperty(sb, member, classBp);
                    break;
                case MemberType.Property:
                    GeneratePropertyOverrideProperties(sb, member, classBp);
                    break;
                case MemberType.Indexer:
                    GenerateIndexerOverrideProperties(sb, member, classBp);
                    break;
                case MemberType.Event:
                    GenerateEventOverrideProperties(sb, member, classBp);
                    break;
            }
        }

        /// <summary>
        /// Generates the override property for a method.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the property to.</param>
        /// <param name="method">The UnifiedMemberBlueprint for the method.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        internal void GenerateMethodOverrideProperty(StringBuilder sb, UnifiedMemberBlueprint method, MockClassBlueprint classBp)
        {
            // Generate full signature for documentation
            var fullSignature = GenerateFullMethodSignature(method);
            var overridePropName = BlueprintHelpers.GetResolvedName(method, classBp);

            bool hasRefOutInOrParams = method.Parameters.Any(p => p.IsRef || p.IsOut || p.IsIn || p.IsParams);

            if (hasRefOutInOrParams)
            {
                var delegateName = GenerateDelegateDeclaration(sb, method, classBp, overridePropName);

                sb.AppendLine($"            /// <summary>");
                sb.AppendLine($"            /// Override for {method.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)}.{BlueprintHelpers.GetOriginalName(method)} method:<br/><c>+ {fullSignature}</c>");
                sb.AppendLine($"            /// </summary>");
                sb.AppendLine($"            public {delegateName}? {overridePropName} {{ get; set; }}");
                sb.AppendLine();
            }
            else
            {
                // TODO: update data model to support this specific case...?
                var modifier = InheritanceAnalyzer.IsSystemObjectMemberName(overridePropName)
                    ? GetModifierString(MemberModifier.New) + " "
                    : string.Empty;

                if (method.IsVoid)
                {
                    // Action for void methods - parameters use override-safe types
                    var actionType = method.Parameters.Count > 0
                        ? $"{Constants.ActionFullName}<{string.Join(", ", method.Parameters.Select((_, i) => BlueprintHelpers.ToOverridePropertyParameterTypeString(method, i)))}>"
                        : Constants.ActionFullName;

                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Override for {method.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)}.{BlueprintHelpers.GetOriginalName(method)} method:<br/><c>+ {fullSignature}</c>");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            public {modifier}{actionType}? {overridePropName} {{ get; set; }}");
                    sb.AppendLine();
                }
                else
                {
                    // Func for non-void methods - parameters use override-safe types
                    var parameterTypes = method.Parameters.Select((_, i) => BlueprintHelpers.ToOverridePropertyParameterTypeString(method, i));
                    var returnType = BlueprintHelpers.ToOverridePropertyReturnTypeString(method);
                    var allTypes = parameterTypes.Concat(new[] { returnType });
                    var funcType = $"{Constants.FuncFullName}<{string.Join(", ", allTypes)}>";

                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Override for {method.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)}.{BlueprintHelpers.GetOriginalName(method)} method:<br/><c>+ {fullSignature}</c>");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            public {modifier}{funcType}? {overridePropName} {{ get; set; }}");
                    sb.AppendLine();
                }
            }
        }

#pragma warning disable IDE0060
        /// <summary>
        /// Generates a delegate declaration for methods with ref/out/in/params parameters.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the delegate declaration to.</param>
        /// <param name="method">The UnifiedMemberBlueprint for the method.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        /// <param name="baseName">The base name for the delegate.</param>
        /// <returns>The name of the generated delegate.</returns>
        private string GenerateDelegateDeclaration(StringBuilder sb, UnifiedMemberBlueprint method, MockClassBlueprint classBp, string baseName)
        {
            var delegateName = $"{baseName}{Constants.DelegateSuffix}";
            var returnType = method.IsVoid ? "void" : BlueprintHelpers.ToOverridePropertyReturnTypeString(method);
            var parameters = string.Join(", ", method.Parameters.Select((p, i) => BlueprintHelpers.ToParameterDeclarationString(method, i, BlueprintHelpers.ToOverridePropertyParameterTypeString(method, i))));

            sb.AppendLine($"            public delegate {returnType} {delegateName}({parameters});");
            return delegateName;
        }
#pragma warning restore IDE0060


        /// <summary>
        /// Generates override properties for a property (getter and setter).
        /// </summary>
        /// <param name="sb">The StringBuilder to append the properties to.</param>
        /// <param name="property">The UnifiedMemberBlueprint for the property.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        internal void GeneratePropertyOverrideProperties(StringBuilder sb, UnifiedMemberBlueprint property, MockClassBlueprint classBp)
        {
            // Use unified blueprint for return type conversion
            var returnType = BlueprintHelpers.ToOverridePropertyReturnTypeString(property);

            // Generate getter override
            if (property.HasGetter)
            {
                var getterSignature = GenerateFullPropertyGetterSignature(property);
                sb.AppendLine($"            /// <summary>");
                sb.AppendLine($"            /// Override for {property.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)}.{BlueprintHelpers.GetOriginalName(property)} property getter:<br/><c>+ {getterSignature}</c>");
                sb.AppendLine($"            /// </summary>");
                sb.AppendLine($"            public {Constants.FuncFullName}<{returnType}>? {BlueprintHelpers.GetResolvedName(property, classBp)}{Constants.GetterSuffix} {{ get; set; }}");
                sb.AppendLine();
            }

            // Generate setter override
            if (property.HasSetter)
            {
                var setterSignature = GenerateFullPropertySetterSignature(property);
                sb.AppendLine($"            /// <summary>");
                sb.AppendLine($"            /// Override for {property.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)}.{BlueprintHelpers.GetOriginalName(property)} property setter:<br/><c>+ {setterSignature}</c>");
                sb.AppendLine($"            /// </summary>");
                sb.AppendLine($"            public {Constants.ActionFullName}<{returnType}>? {BlueprintHelpers.GetResolvedName(property, classBp)}{Constants.SetterSuffix} {{ get; set; }}");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Generates override properties for an indexer (getter and setter).
        /// </summary>
        /// <param name="sb">The StringBuilder to append the properties to.</param>
        /// <param name="indexer">The UnifiedMemberBlueprint for the indexer.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        internal void GenerateIndexerOverrideProperties(StringBuilder sb, UnifiedMemberBlueprint indexer, MockClassBlueprint classBp)
        {
            // Use unified blueprint for parameter and return type conversion
            var parameterTypes = indexer.Parameters.Select((_, i) => BlueprintHelpers.ToOverridePropertyParameterTypeString(indexer, i));
            var returnType = BlueprintHelpers.ToOverridePropertyReturnTypeString(indexer);

            // Generate getter override
            if (indexer.HasGetter)
            {
                var getterTypes = parameterTypes.Concat(new[] { returnType });
                var getterFuncType = $"{Constants.FuncFullName}<{string.Join(", ", getterTypes)}>";
                var getterSignature = GenerateFullIndexerGetterSignature(indexer);

                sb.AppendLine($"            /// <summary>");
                sb.AppendLine($"            /// Override for {indexer.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)} indexer getter:<br/><c>+ {getterSignature}</c>");
                sb.AppendLine($"            /// </summary>");
                sb.AppendLine($"            public {getterFuncType}? {BlueprintHelpers.GetResolvedName(indexer, classBp)}{Constants.GetterSuffix} {{ get; set; }}");
                sb.AppendLine();
            }

            // Generate setter override
            if (indexer.HasSetter)
            {
                var setterTypes = parameterTypes.Concat(new[] { returnType });
                var setterActionType = $"{Constants.ActionFullName}<{string.Join(", ", setterTypes)}>";
                var setterSignature = GenerateFullIndexerSetterSignature(indexer);

                sb.AppendLine($"            /// <summary>");
                sb.AppendLine($"            /// Override for {indexer.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)} indexer setter:<br/><c>+ {setterSignature}</c>");
                sb.AppendLine($"            /// </summary>");
                sb.AppendLine($"            public {setterActionType}? {BlueprintHelpers.GetResolvedName(indexer, classBp)}{Constants.SetterSuffix} {{ get; set; }}");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Generates override properties for an event (add and remove accessors).
        /// </summary>
        /// <param name="sb">The StringBuilder to append the properties to.</param>
        /// <param name="eventBp">The UnifiedMemberBlueprint for the event.</param>
        /// <param name="classBp">The MockClassBlueprint for the class.</param>
        internal void GenerateEventOverrideProperties(StringBuilder sb, UnifiedMemberBlueprint eventBp, MockClassBlueprint classBp)
        {
            var eventSignature = GenerateFullEventSignature(eventBp);
            var resolved = BlueprintHelpers.GetResolvedName(eventBp, classBp);
            var eventType = BlueprintHelpers.ToReturnTypeString(eventBp);
            sb.AppendLine($"            /// <summary>");
            sb.AppendLine($"            /// Overrides for {eventBp.ContainingType?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)}.{BlueprintHelpers.GetOriginalName(eventBp)} event add/remove:<br/><c>+ {eventSignature}</c>");
            sb.AppendLine($"            /// </summary>");
            sb.AppendLine($"            public {Constants.ActionFullName}<{eventType}>? {resolved}{Constants.AdderSuffix} {{ get; set; }}");
            sb.AppendLine($"            public {Constants.ActionFullName}<{eventType}>? {resolved}{Constants.RemoverSuffix} {{ get; set; }}");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates code to throw a TDoublesException for a given member.
        /// </summary>
        /// <param name="memberBp">The UnifiedMemberBlueprint for the member.</param>
        /// <returns>The string representing the exception throw statement.</returns>
        private string GenerateTDoublesExceptionThrow(UnifiedMemberBlueprint memberBp)
        {
            var memberName = BlueprintHelpers.GetOriginalName(memberBp);
            var typeName = BlueprintHelpers.GetContainingTypeName(memberBp);
            return $"throw new TDoubles.TDoublesException(\"{memberName}\", \"{typeName}\")";
        }

        /// <summary>
        /// Converts a MemberModifier enum to its string representation for code generation.
        /// </summary>
        /// <param name="modifier">The member modifier enum value.</param>
        /// <returns>The string representation of the modifier, or empty string for None.</returns>
        private string GetModifierString(MemberModifier modifier)
        {
            return modifier switch
            {
                MemberModifier.Override => "override",
                MemberModifier.New => "new",
                MemberModifier.Virtual => "virtual",
                MemberModifier.None => string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Generates full method signature for documentation.
        /// </summary>
        private string GenerateFullMethodSignature(UnifiedMemberBlueprint method)
        {
            var signature = new StringBuilder();

            // Add return type (for non-void methods) using unified blueprint
            if (!method.IsVoid)
            {
                signature.Append($"{BlueprintHelpers.ToReturnTypeDocumentationString(method)} ");
            }
            else
            {
                signature.Append("void ");
            }

            // Add method name
            signature.Append(BlueprintHelpers.GetOriginalName(method));

            // Add generic type parameters
            if (method.IsGeneric && BlueprintHelpers.GetGenericTypeParameters(method).Count > 0)
            {
                signature.Append($"&lt;{string.Join(", ", BlueprintHelpers.GetGenericTypeParameters(method))}&gt;");
            }

            // Add parameters using unified blueprint
            signature.Append('(');
            if (method.Parameters.Count > 0)
            {
                var parameterStrings = method.Parameters.Select((param, i) =>
                {
                    var paramStr = new StringBuilder();
                    if (!string.IsNullOrEmpty(BlueprintHelpers.GetParameterModifier(param)))
                    {
                        paramStr.Append($"{BlueprintHelpers.GetParameterModifier(param)} ");
                    }
                    paramStr.Append($"{BlueprintHelpers.ToParameterTypeDocumentationString(method, i)} {BlueprintHelpers.GetParameterName(param)}");
                    return paramStr.ToString();
                });
                signature.Append(string.Join(", ", parameterStrings));
            }
            signature.Append(')');

            // Add generic constraints
            var genericConstraints = BlueprintHelpers.GetGenericConstraints(method);
            if (genericConstraints?.Count > 0)
            {
                signature.Append($" {string.Join(" ", genericConstraints)}");
            }

            return signature.ToString();
        }

        /// <summary>
        /// Generates full property getter signature for documentation.
        /// </summary>
        private string GenerateFullPropertyGetterSignature(UnifiedMemberBlueprint property)
        {
            return $"{BlueprintHelpers.ToReturnTypeDocumentationString(property)} {BlueprintHelpers.GetOriginalName(property)} {{ get; }}";
        }

        /// <summary>
        /// Generates full property setter signature for documentation.
        /// </summary>
        private string GenerateFullPropertySetterSignature(UnifiedMemberBlueprint property)
        {
            return $"{BlueprintHelpers.ToReturnTypeDocumentationString(property)} {BlueprintHelpers.GetOriginalName(property)} {{ set; }}";
        }

        /// <summary>
        /// Generates full indexer getter signature for documentation.
        /// </summary>
        private string GenerateFullIndexerGetterSignature(UnifiedMemberBlueprint indexer)
        {
            var parameterStrings = indexer.Parameters.Select((param, i) =>
            {
                var paramStr = new StringBuilder();
                if (!string.IsNullOrEmpty(BlueprintHelpers.GetParameterModifier(param)))
                {
                    paramStr.Append($"{BlueprintHelpers.GetParameterModifier(param)} ");
                }
                paramStr.Append($"{BlueprintHelpers.ToIndexerParameterTypeDocumentationString(indexer, i)} {BlueprintHelpers.GetParameterName(param)}");
                return paramStr.ToString();
            });

            return $"{BlueprintHelpers.ToReturnTypeDocumentationString(indexer)} this[{string.Join(", ", parameterStrings)}] {{ get; }}";
        }

        /// <summary>
        /// Generates full indexer setter signature for documentation.
        /// </summary>
        private string GenerateFullIndexerSetterSignature(UnifiedMemberBlueprint indexer)
        {
            var parameterStrings = indexer.Parameters.Select((param, i) =>
            {
                var paramStr = new StringBuilder();
                if (!string.IsNullOrEmpty(BlueprintHelpers.GetParameterModifier(param)))
                {
                    paramStr.Append($"{BlueprintHelpers.GetParameterModifier(param)} ");
                }
                paramStr.Append($"{BlueprintHelpers.ToIndexerParameterTypeDocumentationString(indexer, i)} {BlueprintHelpers.GetParameterName(param)}");
                return paramStr.ToString();
            });

            return $"{BlueprintHelpers.ToReturnTypeDocumentationString(indexer)} this[{string.Join(", ", parameterStrings)}] {{ set; }}";
        }

        /// <summary>
        /// Generates full event signature for documentation.
        /// </summary>
        /// <param name="eventBp">The UnifiedMemberBlueprint for the event.</param>
        /// <returns>The full event signature string.</returns>
        private string GenerateFullEventSignature(UnifiedMemberBlueprint eventBp)
        {
            return $"event {ConvertToShortTypeName(eventBp.ReturnTypeSymbol)} {BlueprintHelpers.GetOriginalName(eventBp)}";
        }

        /// <summary>
        /// Converts type symbols to short C# keyword names.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to convert.</param>
        /// <returns>The short C# keyword equivalent type name.</returns>
        private string ConvertToShortTypeName(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol == null)
            {
                return "object";
            }

            // Use TypeConverter for consistent type conversion
            return SymbolHelpers.ToCSharpKeywordString(typeSymbol);
        }

        /// <summary>
        /// Converts generic types to short names.
        /// </summary>
        // REMOVED: This method violated symbol-based architecture by taking string parameters.
        // Use TypeConverter.ToCSharpKeywordString(ITypeSymbol) instead.

        /// <summary>
        /// Converts array types to short names.
        /// </summary>
        // REMOVED: This method violated symbol-based architecture by taking string parameters.
        // Use TypeConverter.ToCSharpKeywordString(ITypeSymbol) instead.

        /// <summary>
        /// Splits generic type arguments, handling nested generics.
        /// </summary>
        // REMOVED: This method violated symbol-based architecture by taking string parameters.
        // Use TypeConverter methods with ITypeSymbol instead.

        /// <summary>
        /// Generates the invocation code for the unified mock callback.
        /// </summary>
        /// <param name="callbackName">The name of the callback method.</param>
        /// <param name="memberSuffix">Optional suffix for the member name (e.g., "Getter", "Setter").</param>
        /// <param name="member">The UnifiedMemberBlueprint for the member.</param>
        /// <returns>The generated callback invocation code.</returns>
        private string GenerateMockCallbackInvocation(string callbackName, string? memberSuffix, UnifiedMemberBlueprint member)
        {
            // var typeName
            //     = SymbolHelpers.GetShortNameWithoutTypeArgs(member.OriginalSymbol?.ContainingType);
            // var fullMemberName
            //     = member.OriginalSymbol?.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)
            //     + '.'
            //     + member.OriginalSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)
            //     + memberSuffix;
            var memberName
                = BlueprintHelpers.GetOriginalName(member)
                + memberSuffix;

            var sb = new StringBuilder();

            sb.Append(callbackName);
            sb.Append("(\"");
            sb.Append(memberName);
            sb.Append("\"); ");

            sb.Append(callbackName);
            sb.Append("(\"");
            sb.Append(memberName);
            sb.Append("\", ");
            if (member.Parameters.Count == 0)
            {
                sb.Append("global::System.Array.Empty<object>()");
            }
            else
            {
                sb.Append("new object?[] { ");
                sb.Append(string.Join(", ", member.Parameters.Select(p => BlueprintHelpers.GetParameterName(p))));
                sb.Append(" }");
            }
            sb.Append(");");

            return sb.ToString();
        }
    }
}
