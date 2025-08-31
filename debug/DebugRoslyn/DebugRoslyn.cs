using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Debug
{
    public class TestListMocking
    {
        private static IEnumerable<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            // Add the TDoubles assembly explicitly if it's not already included
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var tdoublesAssemblyPath = Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "src", "bin", "Debug", "netstandard2.0", "TDoubles.dll");
            if (!references.Any(r => r.Display == tdoublesAssemblyPath))
            {
                references.Add(MetadataReference.CreateFromFile(tdoublesAssemblyPath));
            }

            return references;
        }

        // [Test]
        public void AnalyzeExplicitInterfaceImplementationsUsingRoslyn()
        {
            var inputCode = """
                namespace N
                {
                    namespace M
                    {
                        interface A<T> : B<T, T> { } 
                        interface B<U, V> { void X(); }
                        
                        class C<W> : A<W>
                        {
                            void A<W>.X() { }
                            void B<W, W>.X() { }
                            void Foo() { }
                        }
                    }
                }
                """;

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { CSharpSyntaxTree.ParseText(inputCode) },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var classCSymbol = compilation.GetTypeByMetadataName("N.M.C`1");
            Assert.That(classCSymbol, Is.Not.Null);

            IMethodSymbol axMethod = null;
            IMethodSymbol bxMethod = null;

            Console.WriteLine($"Analyzing members of class {classCSymbol.Name}:");

            foreach (var member in classCSymbol.GetMembers())
            {
                if (member is IMethodSymbol methodSymbol)
                {
                    Console.WriteLine($"  Method Name: {methodSymbol.Name} ({methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    Console.WriteLine($"  Method Kind: {methodSymbol.MethodKind}");
                    Console.WriteLine($"  IsExplicitInterfaceImplementation: {methodSymbol.ExplicitInterfaceImplementations.Any()}");

                    if (methodSymbol.ExplicitInterfaceImplementations.Any())
                    {
                        Console.WriteLine($"    Explicitly implements:");
                        foreach (var explicitImpl in methodSymbol.ExplicitInterfaceImplementations)
                        {
                            Console.WriteLine($"      - Interface: {explicitImpl.ContainingType.Name}, Method: {explicitImpl.Name}");
                        }
                    }

                    if (methodSymbol.Name == "A.X")
                    {
                        axMethod = methodSymbol;
                    }
                    else if (methodSymbol.Name == "B.X")
                    {
                        bxMethod = methodSymbol;
                    }
                }
            }

            Assert.That(axMethod, Is.Not.Null, "A.X method not found.");
            Assert.That(bxMethod, Is.Not.Null, "B.X method not found.");

            // For A.X, it explicitly implements B.X through A
            Assert.That(axMethod.ExplicitInterfaceImplementations.Any(impl => 
                (impl.ContainingType.Name == "A" || impl.ContainingType.Name == "B") && impl.Name == "X"), 
                Is.True, "A.X is not recognized as explicit implementation of A.X or B.X.");

            // For B.X, it explicitly implements B.X
            Assert.That(bxMethod.ExplicitInterfaceImplementations.Any(impl => 
                impl.ContainingType.Name == "B" && impl.Name == "X"), 
                Is.True, "B.X is not recognized as explicit implementation of B.X.");
        }

        // [Test]
        public void AnalyzeInterfaceMembersUsingRoslyn()
        {
            var inputCode = @"namespace N
{
    interface IMyInterface
    {
        string MyMethod(int param1, bool? param2);
        int MyProperty { get; set; }
        event Action MyEvent;
    }

    interface IGenericInterface<T>
    {
        T GetValue(T input);
    }
}";

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { CSharpSyntaxTree.ParseText(inputCode) },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            ITypeSymbol dictionarySymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
            Assert.That(dictionarySymbol, Is.Not.Null);

            var allMembers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            dictionarySymbol = GetTypeForMemberExtraction(dictionarySymbol);
            ITypeSymbol GetTypeForMemberExtraction(ITypeSymbol type)
            {
                // If this is an unbound generic type (has type parameters but no type arguments),
                // we need to construct it with placeholder type arguments to extract members
                if (type is INamedTypeSymbol namedType && 
                    namedType.IsGenericType && 
                    namedType.IsUnboundGenericType)
                {
                    // Preserve generic type parameters by using the original definition,
                    // so member signatures retain type parameter symbols (e.g., A) instead of placeholders.
                    return namedType.OriginalDefinition;
                }
                
                // For non-generic types or already constructed generic types, return as-is
                return type;
            }

            // Add members from the interface itself
            foreach (var member in (dictionarySymbol).GetMembers())
            {
                allMembers.Add(member);
            }

            // Add members from directly implemented interfaces
            foreach (var directInterface in dictionarySymbol.Interfaces)
            {
                foreach (var member in (directInterface).GetMembers())
                {
                    allMembers.Add(member);
                }
            }

            // Add members from all interfaces in the hierarchy
            foreach (var allInterface in dictionarySymbol.AllInterfaces)
            {
                foreach (var member in (allInterface).GetMembers())
                {
                    allMembers.Add(member);
                }
            }

            Console.WriteLine($"Analyzing members of interface {dictionarySymbol.Name}:");

            foreach (var member in allMembers.OrderBy(x => x.ContainingType.Name).ThenBy(x => x.Name))
            {
                Console.Write($"  Member: {member.ContainingType.Name}.{member.Name}, Kind: {member.Kind}");

                if (member is IMethodSymbol methodSymbol)
                {
                    Console.Write($"    Method Return Type: {methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    Console.Write($"    Method Return Nullability: {methodSymbol.ReturnType.NullableAnnotation}");

                    if (methodSymbol.Parameters.Any())
                    {
                        Console.Write($"    Parameters:");
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            Console.Write($"      - Name: {parameter.Name}, Type: {parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, Nullability: {parameter.Type.NullableAnnotation}");
                        }
                    }
                    else
                    {
                        Console.Write($"    No parameters.");
                    }
                }
                else if (member is IPropertySymbol propertySymbol)
                {
                    Console.Write($"    Property Type: {propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    Console.Write($"    Property Nullability: {propertySymbol.Type.NullableAnnotation}");
                }
                else if (member is IEventSymbol eventSymbol)
                {
                    Console.Write($"    Event Type: {eventSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    Console.Write($"    Event Nullability: {eventSymbol.Type.NullableAnnotation}");
                }

                Console.WriteLine();
            }

            var genericInterfaceSymbol = compilation.GetTypeByMetadataName("N.IGenericInterface`1");
            Assert.That(genericInterfaceSymbol, Is.Not.Null);

            Console.WriteLine($"\nAnalyzing members of generic interface {genericInterfaceSymbol.Name}:");

            foreach (var member in genericInterfaceSymbol.GetMembers())
            {
                Console.WriteLine($"  Member: {member.Name}, Kind: {member.Kind}");

                if (member is IMethodSymbol methodSymbol)
                {
                    Console.WriteLine($"    Method Return Type: {methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    Console.WriteLine($"    Method Return Nullability: {methodSymbol.ReturnType.NullableAnnotation}");

                    if (methodSymbol.Parameters.Any())
                    {
                        Console.WriteLine($"    Parameters:");
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            Console.WriteLine($"      - Name: {parameter.Name}, Type: {parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, Nullability: {parameter.Type.NullableAnnotation}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"    No parameters.");
                    }
                }
            }
        }

        [Test]
        public void AnalyzeMethodLevelGenerics()
        {
            var inputCode = @"using TDoubles;

namespace MyTestNamespace
{
    public interface IMyGenericMethodInterface<A>
    {
        A ClassTypeArgMustBePreserved<A>();
        void ClassTypeArgMustBePreserved<A>(out A output);
        TResult GetDefault<TResult>();
        void Process<TInput>(TInput input);
    }

    [Mock(typeof(IMyGenericMethodInterface<>))]
    public partial class MyMock : IMyGenericMethodInterface<A>
    {
        // Mock implementation will be generated here
    }
}";

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { CSharpSyntaxTree.ParseText(inputCode) },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var targetInterfaceSymbol = compilation.GetTypeByMetadataName("MyTestNamespace.IMyGenericMethodInterface`1");
            var mockClassSymbol = compilation.GetTypeByMetadataName("MyTestNamespace.MyMock");

            Assert.That(targetInterfaceSymbol, Is.Not.Null);
            Assert.That(mockClassSymbol, Is.Not.Null);

            var blueprintBuilder = new TDoubles.MockBlueprintBuilder();
            var blueprint = blueprintBuilder.BuildBlueprint(
                targetType: targetInterfaceSymbol,
                mockClass: mockClassSymbol,
                includeInternals: false,
                mode: TDoubles.GenericMockingMode.NonGeneric, // This might need to be adjusted based on the actual scenario
                compilation: compilation
            );

            Assert.That(blueprint, Is.Not.Null);

            Console.WriteLine($"\nAnalyzing members from MockClassBlueprint for {blueprint.TargetTypeSymbol.Name}:");

            foreach (var memberBlueprint in blueprint.Members)
            {
                if (memberBlueprint.OriginalSymbol is IMethodSymbol methodSymbol)
                {
                    Console.WriteLine($"\n--- Analyzing Method: {methodSymbol.Name} ---");
                    // Console.WriteLine($"  IsGenericMethod: {methodSymbol.IsGenericMethod}");
                    // Console.WriteLine($"  Method Type Parameters: {string.Join(", ", methodSymbol.TypeParameters.Select(tp => tp.Name))}");

                    // Generate and print the method source code
                    var codeGenerator = new TDoubles.BlueprintDrivenCodeGenerator();
                    var generatedMethodCode = codeGenerator.GenerateUnifiedMember(new StringBuilder(), memberBlueprint, blueprint);
                    Console.WriteLine($"\nGenerated Method Code:\n{generatedMethodCode}");

                    // Emit mock override declaration in debug logging
                    var overrideContainerBuilder = new StringBuilder();
                    codeGenerator.GenerateMethodOverrideProperty(overrideContainerBuilder, memberBlueprint, blueprint);
                    Console.WriteLine($"\nMock Override Container Declaration:\n{overrideContainerBuilder.ToString()}");
                }
            }
        }

        [Test]
        public void AnalyzeGenericMethodConstraints()
        {
            var inputCode = @"using TDoubles;
using System.Collections.Generic;

namespace MyTestNamespace
{
    public interface IService<TKey, TValue>
        where TKey : class, new()
        where TValue : struct
    {
        void Process<T>(T item) where T : class, new();
        T GetDefault<T>() where T : struct;
        void Log<T>(T message) where T : IEnumerable<string>;
    }

    [Mock(typeof(IService<,>))]
    public partial class MockService<TKey, TValue> : IService<TKey, TValue>
        where TKey : class, new()
        where TValue : struct
    {
        // Mock implementation will be generated here
    }
}";

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { CSharpSyntaxTree.ParseText(inputCode) },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var targetInterfaceSymbol = compilation.GetTypeByMetadataName("MyTestNamespace.IService`2");
            var mockClassSymbol = compilation.GetTypeByMetadataName("MyTestNamespace.MockService`2");

            Assert.That(targetInterfaceSymbol, Is.Not.Null);
            Assert.That(mockClassSymbol, Is.Not.Null);

            var blueprintBuilder = new TDoubles.MockBlueprintBuilder();
            var blueprint = blueprintBuilder.BuildBlueprint(
                targetType: targetInterfaceSymbol,
                mockClass: mockClassSymbol,
                includeInternals: false,
                mode: TDoubles.GenericMockingMode.UnboundGeneric,
                compilation: compilation
            );

            Assert.That(blueprint, Is.Not.Null);

            Console.WriteLine($"\nAnalyzing members from MockClassBlueprint for {blueprint.TargetTypeSymbol.Name}:");

            Console.WriteLine($"\n--- Analyzing Class-Level Generics ---");
            foreach (var typeParam in blueprint.TypeMapping.MockClassTypeParameterSymbols)
            {
                Console.WriteLine($"  Class Type Parameter: {typeParam.Name}");
                var targetTypeParam = blueprint.TypeMapping.TypeParameterSymbolMap.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Value, typeParam)).Key;
                if (targetTypeParam != null)
                {
                    Console.WriteLine($"    Target Type Parameter: {targetTypeParam.Name}");
                    Console.WriteLine($"    HasReferenceTypeConstraint: {targetTypeParam.HasReferenceTypeConstraint}");
                    Console.WriteLine($"    HasValueTypeConstraint: {targetTypeParam.HasValueTypeConstraint}");
                    Console.WriteLine($"    HasConstructorConstraint: {targetTypeParam.HasConstructorConstraint}");
                    Console.WriteLine($"    HasUnmanagedTypeConstraint: {targetTypeParam.HasUnmanagedTypeConstraint}");
                    Console.WriteLine($"    ConstraintTypes Count: {targetTypeParam.ConstraintTypes.Length}");
                    foreach (var constraintType in targetTypeParam.ConstraintTypes)
                    {
                        Console.WriteLine($"      Constraint Type: {constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                    }
                }
            }

            foreach (var memberBlueprint in blueprint.Members)
                {
                    if (memberBlueprint.OriginalSymbol is IMethodSymbol methodSymbol)
                    {
                        Console.WriteLine($"\n--- Analyzing Method: {methodSymbol.Name} ---");
                        Console.WriteLine($"  IsGenericMethod: {methodSymbol.IsGenericMethod}");
                        Console.WriteLine($"  Method Type Parameters: {string.Join(", ", methodSymbol.TypeParameters.Select(tp => tp.Name))}");

                        Console.WriteLine($"  GenericConstraintSymbols Count: {memberBlueprint.GenericConstraintSymbols?.Count ?? 0}");
                        if (memberBlueprint.GenericConstraintSymbols != null)
                        {
                            foreach (var constraint in memberBlueprint.GenericConstraintSymbols)
                            {
                                Console.WriteLine($"    Constraint: {constraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                            }
                        }

                        var codeGenerator = new TDoubles.BlueprintDrivenCodeGenerator();
                        var generatedMethodCode = codeGenerator.GenerateUnifiedMember(new StringBuilder(), memberBlueprint, blueprint);
                        Console.WriteLine($"\nGenerated Method Code:\n{generatedMethodCode}");
                    }
                }
        }
    }
}