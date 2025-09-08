using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TDoubles.Tests.ComprehensiveValidation
{
    public sealed class GeneratorValidationModel
    {
        public string ProjectDirectory { get; }
        public string GeneratorAssemblyPath { get; }
        public string GeneratorTypeName { get; }

        public GeneratorValidationModel(string projectDirectory, string generatorAssemblyPath, string generatorTypeName)
        {
            ProjectDirectory = projectDirectory ?? throw new ArgumentNullException(nameof(projectDirectory));
            GeneratorAssemblyPath = generatorAssemblyPath ?? throw new ArgumentNullException(nameof(generatorAssemblyPath));
            GeneratorTypeName = generatorTypeName ?? throw new ArgumentNullException(nameof(generatorTypeName));
        }

        public static GeneratorValidationModel Create([CallerFilePath] string? callerFilePath = null)
        {
            string projectDirectory = "tests/" + Path.GetFileNameWithoutExtension(callerFilePath);
            return CreateCore(projectDirectory);
        }

        internal static GeneratorValidationModel CreateCore(string projectDirectory)
        {
            string generatorAssemblyPath
#if DEBUG
            = "src/bin/Debug/netstandard2.0/SatorImaging.TDoubles.dll"
#else
            = "src/bin/Release/netstandard2.0/SatorImaging.TDoubles.dll"
#endif
            ;

            return new GeneratorValidationModel(projectDirectory, generatorAssemblyPath, "TDoubles.TDoublesSourceGenerator");
        }
    }

    public static class ValidationHelper
    {
        private static void LogPass(string message)
        {
            Console.WriteLine($"[PASS] {message}");
        }

        private static void LogFail(string message)
        {
            Console.WriteLine($"[FAIL] {message}");
        }

        public static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ValidationHelper <path_to_project_directory>");
                return 1;
            }

            string inputPath = args[0];
            string projectDirectory;

            if (Path.GetExtension(inputPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                projectDirectory = Path.GetDirectoryName(inputPath) ?? throw new InvalidOperationException($"Could not determine directory for .csproj file: {inputPath}");
            }
            else
            {
                projectDirectory = inputPath;
            }

            if (!Directory.Exists(projectDirectory))
            {
                Console.WriteLine($"Error: Project directory not found at '{projectDirectory}'");
                return 1;
            }

            var model = GeneratorValidationModel.CreateCore(projectDirectory);

            if (!File.Exists(model.GeneratorAssemblyPath))
            {
                Console.WriteLine($"Error: Generator assembly not found at '{model.GeneratorAssemblyPath}'");
                Console.WriteLine("Please ensure TDoubles project is built.");
                return 1;
            }

            int validationResult;
            try
            {
                validationResult = ValidateGeneratedSources(model);
                if (validationResult == 0)
                {
                    Console.WriteLine("Validation completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during validation: {ex.Message}");
                validationResult = 1;
            }
            return validationResult;
        }

        // Validates that a given implementation instance conforms to the specified contract type (interface or abstract class).
        public static int ValidateImplementation<TContract, TImplementation>(TImplementation implementation)
        {
            try
            {
                if (implementation is null)
                {
                    LogFail("ValidateImplementation: implementation is null");
                    return 1;
                }

                var implementationType = implementation.GetType();
                var contractType = typeof(TContract);

                if (!contractType.IsAssignableFrom(implementationType))
                {
                    LogFail($"ValidateImplementation: '{implementationType.FullName}' does not implement/derive from '{contractType.FullName}'");
                    return 1;
                }

                LogPass($"ValidateImplementation: '{implementationType.FullName}' implements/derives from '{contractType.FullName}'");
                return 0;
            }
            catch (Exception ex)
            {
                LogFail($"ValidateImplementation threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a given implementation instance does NOT conform to the specified contract type.
        public static int ValidateNotImplementation<TContract, TImplementation>(TImplementation implementation)
        {
            try
            {
                if (implementation is null)
                {
                    LogFail("ValidateNotImplementation: implementation is null");
                    return 1;
                }

                var implementationType = implementation.GetType();
                var contractType = typeof(TContract);

                if (!contractType.IsAssignableFrom(implementationType))
                {
                    LogPass($"ValidateNotImplementation: '{implementationType.FullName}' does not implement/derive from '{contractType.FullName}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateNotImplementation: '{implementationType.FullName}' unexpectedly implements/derives from '{contractType.FullName}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateNotImplementation threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a given implementation instance does NOT implement the specified interface.
        public static int ValidateDoesNotImplementInterface<TInterface, TImplementation>(TImplementation implementation)
        {
            try
            {
                if (implementation is null)
                {
                    LogFail("ValidateDoesNotImplementInterface: implementation is null");
                    return 1;
                }

                var implementationType = implementation.GetType();
                var interfaceType = typeof(TInterface);

                if (!interfaceType.IsInterface)
                {
                    LogFail($"ValidateDoesNotImplementInterface: '{interfaceType.FullName}' is not an interface.");
                    return 1;
                }

                if (implementationType.GetInterfaces().Contains(interfaceType))
                {
                    LogFail($"ValidateDoesNotImplementInterface: '{implementationType.FullName}' unexpectedly implements '{interfaceType.FullName}'.");
                    return 1;
                }
                else
                {
                    LogPass($"ValidateDoesNotImplementInterface: '{implementationType.FullName}' does not implement '{interfaceType.FullName}'.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateDoesNotImplementInterface threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates a method or function call on a model returns expected value.
        public static int ValidateCall<TModel, TResult>(TModel model, Func<TModel, TResult> call, TResult expected, IEqualityComparer<TResult>? comparer = null)
        {
            try
            {
                if (call is null)
                {
                    LogFail("ValidateCall: call delegate is null");
                    return 1;
                }

                var actual = call(model);
                var eq = comparer ?? EqualityComparer<TResult>.Default;
                if (eq.Equals(actual, expected))
                {
                    LogPass($"ValidateCall: actual '{actual}' equals expected '{expected}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateCall: actual '{actual}' does not equal expected '{expected}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateCall threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates property get/set flow using the full model instance.
        public static int ValidateProperty<TModel, T>(TModel model, Func<TModel, T> getter, Action<TModel, T> setter, T setValue, T expectedAfter, IEqualityComparer<T>? comparer = null)
        {
            try
            {
                if (getter is null)
                {
                    LogFail("ValidateProperty: getter is null");
                    return 1;
                }
                if (setter is null)
                {
                    LogFail("ValidateProperty: setter is null");
                    return 1;
                }

                setter(model, setValue);
                var after = getter(model);
                var eq = comparer ?? EqualityComparer<T>.Default;
                if (eq.Equals(after, expectedAfter))
                {
                    LogPass($"ValidateProperty: value after set '{after}' equals expected '{expectedAfter}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateProperty: value after set '{after}' does not equal expected '{expectedAfter}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateProperty threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates indexer get/set flow using the full model instance.
        public static int ValidateIndexer<TModel, TIndex, TValue>(TModel model, Func<TModel, TIndex, TValue> getter, Action<TModel, TIndex, TValue> setter, TIndex index, TValue setValue, TValue expectedAfter, IEqualityComparer<TValue>? comparer = null)
        {
            try
            {
                if (getter is null)
                {
                    LogFail("ValidateIndexer: getter is null");
                    return 1;
                }
                if (setter is null)
                {
                    LogFail("ValidateIndexer: setter is null");
                    return 1;
                }

                setter(model, index, setValue);
                var after = getter(model, index);
                var eq = comparer ?? EqualityComparer<TValue>.Default;
                if (eq.Equals(after, expectedAfter))
                {
                    LogPass($"ValidateIndexer: value after set '{after}' equals expected '{expectedAfter}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateIndexer: value after set '{after}' does not equal expected '{expectedAfter}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateIndexer threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates arbitrary member value (e.g., ToString/GetHashCode/Equals-self) using the full model instance.
        public static int ValidateMember<TModel, TResult>(TModel model, Func<TModel, TResult> member, TResult expected, IEqualityComparer<TResult>? comparer = null)
        {
            try
            {
                if (member is null)
                {
                    LogFail("ValidateMember: member delegate is null");
                    return 1;
                }

                var actual = member(model);
                var eq = comparer ?? EqualityComparer<TResult>.Default;
                if (eq.Equals(actual, expected))
                {
                    LogPass($"ValidateMember: actual '{actual}' equals expected '{expected}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateMember: actual '{actual}' does not equal expected '{expected}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateMember threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a member can be invoked without throwing (existence/accessible behavior).
        public static int ValidateMemberExists(Action action, string description)
        {
            try
            {
                if (action is null)
                {
                    LogFail($"ValidateMemberExists: {description}: action is null");
                    return 1;
                }
                action();
                LogPass($"ValidateMemberExists: {description}");
                return 0;
            }
            catch (Exception ex)
            {
                LogFail($"ValidateMemberExists: {description}: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Overload that uses the full model and an action that accepts the model.
        public static int ValidateMemberExists<TModel>(TModel model, Action<TModel> action, string description)
        {
            try
            {
                if (action is null)
                {
                    LogFail($"ValidateMemberExists: {description}: action is null");
                    return 1;
                }
                action(model);
                LogPass($"ValidateMemberExists: {description}");
                return 0;
            }
            catch (Exception ex)
            {
                LogFail($"ValidateMemberExists: {description}: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a void method or property/indexer setter doesn't throw.
        public static int ValidateAction(Action action, string description)
        {
            try
            {
                if (action is null)
                {
                    LogFail($"ValidateAction: {description}: action is null");
                    return 1;
                }
                action();
                LogPass($"ValidateAction: {description}");
                return 0;
            }
            catch (Exception ex)
            {
                LogFail($"ValidateAction: {description}: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        private const BindingFlags FIND_ALL_MEMBERS
            = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.GetProperty
            | BindingFlags.SetProperty
            | BindingFlags.DeclaredOnly
            ;

        // Validates that a member (method or property) exists on the given object using reflection.
        public static int ValidateMemberExists(object instance, string memberName, string description, BindingFlags bindingFlags = FIND_ALL_MEMBERS)
        {
            try
            {
                if (instance is null)
                {
                    LogFail($"ValidateMemberExists: {description}: instance is null");
                    return 1;
                }

                var type = instance.GetType();
                var memberInfo = type.GetMember(memberName, bindingFlags);

                if (memberInfo != null && memberInfo.Length > 0)
                {
                    LogPass($"ValidateMemberExists: {description}: Member '{memberName}' found on type '{type.Name}' as expected.");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateMemberExists: {description}: Member '{memberName}' not found on type '{type.Name}'.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateMemberExists: {description}: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a member (method or property) does not exist on the given object.
        public static int ValidateMemberDoesNotExist(object instance, string memberName, string description, BindingFlags bindingFlags = FIND_ALL_MEMBERS)
        {
            try
            {
                if (instance is null)
                {
                    LogFail($"ValidateMemberDoesNotExist: {description}: instance is null");
                    return 1;
                }

                var type = instance.GetType();
                var memberInfo = type.GetMember(memberName, bindingFlags);

                if (memberInfo != null && memberInfo.Length > 0)
                {
                    LogFail($"ValidateMemberDoesNotExist: {description}: Member '{memberName}' unexpectedly found on type '{type.Name}'.");
                    return 1;
                }
                else
                {
                    LogPass($"ValidateMemberDoesNotExist: {description}: Member '{memberName}' not found on type '{type.Name}' as expected.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateMemberDoesNotExist: {description}: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that invoking action throws an exception matching the expected type name and optional message content.
        // expectedExceptionFullName: full type name, e.g., "TDoubles.TDoublesException".
        public static int ValidateThrows<TModel>(TModel model, Action<TModel> action, string expectedExceptionFullName, string? messageContains = null)
        {
            try
            {
                if (action is null)
                {
                    LogFail("ValidateThrows: action is null");
                    return 1;
                }

                action(model);
                LogFail($"ValidateThrows: expected '{expectedExceptionFullName}' but no exception was thrown");
                return 1;
            }
            catch (Exception ex)
            {
                var actualType = ex.GetType();
                var actualFullName = actualType.FullName ?? actualType.Name;
                var typeMatches = string.Equals(actualFullName, expectedExceptionFullName, StringComparison.Ordinal);
                var messageMatches = messageContains == null || (ex.Message?.Contains($" '{messageContains}' ", StringComparison.Ordinal) == true);

                if (typeMatches && messageMatches)
                {
                    LogPass($"ValidateThrows: threw '{actualFullName}' as expected");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateThrows: threw '{actualFullName}' with message '{ex.Message}', expected '{expectedExceptionFullName}'{(messageContains is not null ? $" containing '{messageContains}'" : string.Empty)}");
                    return 1;
                }
            }
        }

        // Validates that invoking action does not throw.
        public static int ValidateNotThrows<TModel>(TModel model, Action<TModel> action)
        {
            try
            {
                if (action is null)
                {
                    LogFail("ValidateNotThrows: action is null");
                    return 1;
                }
                action(model);
                LogPass("ValidateNotThrows: no exception thrown");
                return 0;
            }
            catch (Exception ex)
            {
                LogFail($"ValidateNotThrows: unexpected {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that the target type does not inherit from any class (other than object) or implement any interfaces.
        public static int ValidateNoInheritanceOrImplementation<TTarget>()
        {
            try
            {
                var targetType = typeof(TTarget);

                if (targetType.BaseType != null &&
                    targetType.BaseType != typeof(object) &&
                    targetType.BaseType != typeof(ValueType))
                {
                    LogFail($"ValidateNoInheritanceOrImplementation: '{targetType.FullName}' inherits from '{targetType.BaseType.FullName}'");
                    return 1;
                }

                if (targetType.GetInterfaces().Any())
                {
                    LogFail($"ValidateNoInheritanceOrImplementation: '{targetType.FullName}' implements interfaces");
                    return 1;
                }

                LogPass($"ValidateNoInheritanceOrImplementation: '{targetType.FullName}' does not inherit or implement any class/interface");
                return 0;
            }
            catch (Exception ex)
            {
                LogFail($"ValidateNoInheritanceOrImplementation threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a string contains a required substring.
        public static int ValidateContains(string source, string expectedSubstring)
        {
            try
            {
                if (source.Contains(expectedSubstring, StringComparison.Ordinal))
                {
                    LogPass($"ValidateContains: 'source' contains '{expectedSubstring}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateContains: 'source' does not contain '{expectedSubstring}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateContains threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Validates that a string does NOT contain an unexpected substring.
        public static int ValidateDoesNotContain(string source, string unexpectedSubstring)
        {
            try
            {
                if (!source.Contains(unexpectedSubstring, StringComparison.Ordinal))
                {
                    LogPass($"ValidateDoesNotContain: 'source' does not contain '{unexpectedSubstring}'");
                    return 0;
                }
                else
                {
                    LogFail($"ValidateDoesNotContain: 'source' unexpectedly contains '{unexpectedSubstring}'");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogFail($"ValidateDoesNotContain threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        // Runs the source generator against a project, compiles the updated compilation,
        // and if there are any errors, prints all generated sources and exits(1).
        public static int ValidateGeneratedSources(GeneratorValidationModel model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            try
            {
                var compilation = CreateCompilationFromProject(model);
                var generator = LoadGenerator(model);

                GeneratorDriver driver = CSharpGeneratorDriver.Create(new IIncrementalGenerator[] { generator });
                driver = driver.RunGenerators(compilation);

                driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var generatorDiagnostics);

                var diagnostics = updatedCompilation.GetDiagnostics();
                var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

                if (hasErrors)
                {
                    PrintGeneratedSources(driver, diagnostics, true);
                    return 1;
                }

                LogPass("ValidateGeneratedSources: compilation succeeded with generated sources");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] ValidateGeneratedSources threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Runs the source generator against a project and returns a dictionary of generated source files.
        /// The key is the file name (HintName) and the value is the generated source code.
        /// </summary>
        /// <param name="model">The generator validation model.</param>
        /// <returns>A dictionary where the key is the file name and the value is the generated source code.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the generator assembly or type cannot be loaded.</exception>
        private static Dictionary<string, string> GetGeneratedSources(GeneratorValidationModel model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            var compilation = CreateCompilationFromProject(model);
            var generator = LoadGenerator(model);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new IIncrementalGenerator[] { generator });
            driver = driver.RunGenerators(compilation);

            var runResult = driver.GetRunResult();
            var generatedSources = new Dictionary<string, string>();

            foreach (var result in runResult.Results)
            {
                foreach (var source in result.GeneratedSources)
                {
                    generatedSources[source.HintName] = source.SourceText.ToString();
                }
            }

            return generatedSources;
        }

        /// <summary>
        /// Runs the source generator against a project and returns the content of a single generated source file
        /// whose HintName starts with the specified text.
        /// </summary>
        /// <param name="model">The generator validation model.</param>
        /// <param name="hintNameStart">The starting text of the HintName to search for.</param>
        /// <param name="generatedSource">The content of the matching generated source file, if found.</param>
        /// <returns>0 if a single matching source is found, 1 otherwise.</returns>
        public static int GetGeneratedSource(GeneratorValidationModel model, string hintNameStart, out string generatedSource)
        {
            generatedSource = string.Empty;
            try
            {
                var generatedSources = GetGeneratedSources(model);
                var matchingSources = generatedSources.Where(s => s.Key.StartsWith(hintNameStart, StringComparison.Ordinal)).ToList();

                if (matchingSources.Count > 1)
                {
                    LogFail($"GetGeneratedSource: Multiple generated sources found starting with '{hintNameStart}'. Matching sources: {string.Join(", ", matchingSources.Select(s => s.Key))}");
                    return 1;
                }

                if (matchingSources.Count == 1)
                {
                    generatedSource = matchingSources.Single().Value;
                    LogPass($"GetGeneratedSource: Found single generated source starting with '{hintNameStart}'.");
                    return 0;
                }

                LogFail($"GetGeneratedSource: No generated source found starting with '{hintNameStart}'. Available sources: {string.Join(", ", generatedSources.Keys)}");
                return 1;
            }
            catch (Exception ex)
            {
                LogFail($"GetGeneratedSource threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        private static IIncrementalGenerator LoadGenerator(GeneratorValidationModel model)
        {
            var assembly = Assembly.LoadFrom(model.GeneratorAssemblyPath);
            var type = assembly.GetType(model.GeneratorTypeName, throwOnError: true)!;

            if (!typeof(IIncrementalGenerator).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type '{model.GeneratorTypeName}' does not implement IIncrementalGenerator.");
            }

            return (IIncrementalGenerator)Activator.CreateInstance(type)!;
        }

        private static CSharpCompilation CreateCompilationFromProject(GeneratorValidationModel model)
        {
            var csFiles = Directory.GetFiles(model.ProjectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                            !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .ToArray();

            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            var syntaxTrees = csFiles.Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), parseOptions, path: p)).ToList();

            // Use trusted platform assemblies for references to match the runtime
            var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
            var references = tpa.Split(Path.PathSeparator)
                .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .Select(p => MetadataReference.CreateFromFile(p))
                .Cast<MetadataReference>()
                .ToList();

            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable,
                optimizationLevel: OptimizationLevel.Debug);

            var compilation = CSharpCompilation.Create(
                assemblyName: Path.GetFileName(model.ProjectDirectory) ?? "TestProject",
                syntaxTrees: syntaxTrees,
                references: references,
                options: compilationOptions);

            return compilation;
        }

        private static void PrintGeneratedSources(GeneratorDriver driver, IEnumerable<Diagnostic> allDiagnostics, bool printErrorsOnly)
        {
            var runResult = driver.GetRunResult();

            // Pre-process diagnostics for efficient lookup
            var relevantDiagnostics = printErrorsOnly
                ? allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList()
                : allDiagnostics.ToList();

            var diagnosticsLookup = relevantDiagnostics
                .Where(d => d.Location.IsInSource && d.Location.SourceTree != null)
                .ToLookup(d => d.Location.SourceTree.FilePath);

            foreach (var result in runResult.Results)
            {
                foreach (var source in result.GeneratedSources)
                {
                    var sourceDiagnostics = diagnosticsLookup[source.SyntaxTree.FilePath].ToList();
                    if (!sourceDiagnostics.Any())
                    {
                        continue;
                    }

                    Console.WriteLine("\n// ---- Generated Source: " + source.HintName + " ----\n");
                    // Add line numbers to the generated source code
                    var lines = source.SourceText.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    int digit = 1 + (int)Math.Log10(lines.Length);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        Console.WriteLine($"{(i + 1).ToString().PadLeft(digit)} |  {lines[i]}");
                    }
                    Console.WriteLine("\n// ---- End Generated Source ----\n");

                    // Print diagnostics related to this source
                    foreach (var diag in sourceDiagnostics)
                    {
                        Console.WriteLine(diag.ToString());
                    }
                }
            }
        }
    }
}
