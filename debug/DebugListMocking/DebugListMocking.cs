using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static NUnit.Framework.StringAssert;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

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

        [Test]
        public void GeneratedCodeDoesNotContainIndexerAccessors()
        {
            // Input code for the source generator
            var inputCode = """
using System.Collections.Generic;
using TDoubles;

namespace TestListMocking
{
    [TDoubles.Mock(typeof(List<>))]
    public partial class MockList<A> { } 
}
""";

            // Create a CSharpCompilation
            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { CSharpSyntaxTree.ParseText(inputCode) },
                GetMetadataReferences(), // Use the helper method to get all references
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Instantiate the source generator
            var generator = new TDoubles.TDoublesSourceGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);

            // Run the generator
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Check for diagnostics
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var diag in diagnostics)
                {
                    Console.WriteLine($"Diagnostic: {diag.Id} - {diag.GetMessage()}");
                }
                Assert.Fail("Source generator produced errors.");
            }

            // Get the generated source code
            var generatedSyntaxTrees = outputCompilation.SyntaxTrees.Where(st => !st.ToString().Contains("inputCode")).ToList();

            if (!generatedSyntaxTrees.Any())
            {
                Assert.Fail("No source code was generated.");
            }

            foreach (var generatedTree in generatedSyntaxTrees)
            {
                var generatedCode = generatedTree.ToString();

                //Console.WriteLine($"--- Generated Code ---\n{generatedCode}\n----------------------");

                if (generatedCode.Contains(".get_Item")) Console.Error.WriteLine("Found '.get_Item'");
                if (generatedCode.Contains(".set_Item")) Console.Error.WriteLine("Found '.set_Item'");

                if (generatedCode.Contains(".get_Item") ||
                    generatedCode.Contains(".set_Item"))
                {
                    Assert.Fail("Gnerated code contains '.get_Item' or '.set_Item'");
                }
            }
        }

        // [Test]
        public void VerifyTypeGraphIndexerExclusion()
        {
            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var listType = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            Assert.That(listType, Is.Not.Null);

            var iListType = compilation.GetTypeByMetadataName("System.Collections.IList");
            Assert.That(iListType, Is.Not.Null);

            var typeGraph = TDoubles.TypeHierarchyGraph.Build(listType!, includeInternals: false, compilation: compilation);

            Console.WriteLine("--- Resolved Members ---");
            foreach (var resolvedMember in typeGraph.ResolvedMembers.Values.OrderBy(x => x.Member.Name))
            {
                var member = resolvedMember.Member;
                var memberType = "";
                var isIndexer = "";
                var parameterCount = "";
                var isPropertyIndexer = "";
                var explicitInterfaceType = "";
                var AssociatedSymbol = "";
                var methodKind = "";

                if (member is IPropertySymbol prop)
                {
                    memberType = "Property";
                    isIndexer = $", IsIndexer: {prop.IsIndexer}";
                    parameterCount = $", ParameterCount: {prop.Parameters.Length}";
                    isPropertyIndexer = $", IsPropertyIndexer: {TDoubles.SymbolHelpers.IsPropertyIndexer(prop)}";
                    explicitInterfaceType = $", ExplicitInterfaceType: {TDoubles.SymbolHelpers.GetExplicitInterfaceType(prop)}";
                }
                else if (member is IMethodSymbol meth)
                {
                    memberType = "Method";
                    methodKind = $", MethodKind: {meth.MethodKind}";
                    methodKind = $", AssociatedSymbol: {meth.AssociatedSymbol ?? meth.ExplicitInterfaceImplementations.FirstOrDefault()?.AssociatedSymbol}";
                }
                else if (member is IEventSymbol ev)
                {
                    memberType = "Event";
                }

                Console.WriteLine($"Member: {member.Name}, Type: {memberType}{parameterCount}{isIndexer}{isPropertyIndexer}{methodKind}{explicitInterfaceType}, DeclaringType: {resolvedMember.DeclaringType.ToDisplayString()}");
            }
            Console.WriteLine("------------------------");

            // Verify that get_Item, set_Item are NOT present
            var iListMembers = iListType!.GetMembers();
            foreach (var member in iListMembers)
            {
                if (member.Name == "get_Item" || member.Name == "set_Item")
                {
                    Assert.That(typeGraph.ResolvedMembers.Values.Any(rm => SymbolEqualityComparer.Default.Equals(rm.Member, member)), Is.False, $"Member {member.Name} from IList should be excluded.");
                }
            }
        }
    
    [Test]
        public void PrintMemberKeysForListMocking()
        {
            var inputCode = """
using System.Collections.Generic;
using TDoubles;

namespace TestListMocking
{
    [TDoubles.Mock(typeof(List<>))]
    public partial class MockList<A> { } 
}
""";

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { CSharpSyntaxTree.ParseText(inputCode) },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var listType = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            Assert.That(listType, Is.Not.Null);

            var mockClassType = compilation.GetTypeByMetadataName("TestListMocking.MockList`1");
            Assert.That(mockClassType, Is.Not.Null);

            var builder = new TDoubles.MockBlueprintBuilder();
            var blueprint = builder.BuildBlueprint(listType!, mockClassType!, includeInternals: false, mode: TDoubles.GenericMockingMode.UnboundGeneric, compilation: compilation);

            Console.WriteLine("--- Generated Member Keys ---");
            foreach (var member in blueprint.Members.OrderBy(m => TDoubles.BlueprintHelpers.GetResolvedNameKey(m)))
            {
                var key = TDoubles.BlueprintHelpers.GetResolvedNameKey(member);
                var resolvedName = TDoubles.BlueprintHelpers.GetResolvedName(member, blueprint);
                Console.WriteLine($"Key: {key}, ResolvedName: {resolvedName}");
            }
            Console.WriteLine("-----------------------------");
        }
    }
}