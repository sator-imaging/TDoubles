using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using TDoubles.DataModels;

namespace TDoubles
{
    /// <summary>
    /// Helper class for generating mock-generator utility types (MockAttribute and TDoublesException).
    /// These utility types are generated in the target assembly with the correct TDoubles namespace.
    /// </summary>
    internal static class GeneratorUtilities
    {
        /// <summary>
        /// Generates the MockAttribute class in the target assembly with the TDoubles namespace.
        /// </summary>
        /// <param name="context">The generator execution context for adding the generated source.</param>
        public static void GenerateMockAttribute(IncrementalGeneratorPostInitializationContext context)
        {
            var attributeSource = """
using System;

namespace TDoubles
{
    /// <summary>
    /// Attribute used to mark partial classes for mock generation.
    /// The source generator will create a mock implementation that delegates to the target type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    internal class MockAttribute : Attribute
    {
        /// <summary>
        /// Gets the target type to be mocked.
        /// </summary>
        public Type TargetType { get; }
        
        /// <summary>
        /// Gets or sets whether to include internal members in the mock.
        /// Default is false (public members only).
        /// </summary>
        public bool IncludeInternals { get; set; } = false;

        /// <summary>
        /// Gets or sets the short names of members to exclude from the generated mock.
        /// </summary>
        public string[] ExcludeMemberShortNames { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Initializes a new instance of the MockAttribute with the specified target type.
        /// </summary>
        /// <param name="targetType">The type to be mocked. Cannot be null.</param>
        /// <param name="excludeMemberShortNames">Optional: Short names of members to exclude from the generated mock.</param>
        /// <exception cref="ArgumentNullException">Thrown when targetType is null.</exception>
        public MockAttribute(Type targetType, params string[] excludeMemberShortNames)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            ExcludeMemberShortNames = excludeMemberShortNames ?? Array.Empty<string>();
        }
    }
}
""";

            context.AddSource(Constants.SystemTypeHintNamePrefix + "MockAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
        }

        /// <summary>
        /// Generates the TDoublesException class in the target assembly with the TDoubles namespace.
        /// </summary>
        /// <param name="context">The generator execution context for adding the generated source.</param>
        public static void GenerateTDoublesException(IncrementalGeneratorPostInitializationContext context)
        {
            var exceptionSource = """
using System;

namespace TDoubles
{
    /// <summary>
    /// Exception thrown when a mock member has no override and returns a non-nullable reference type.
    /// This exception provides clear information about which member lacks a mock implementation.
    /// </summary>
    internal class TDoublesException : Exception
    {
        /// <summary>
        /// Gets the name of the member that lacks a mock implementation.
        /// </summary>
        public string MemberName { get; }
        
        /// <summary>
        /// Gets the name of the type that contains the member.
        /// </summary>
        public string TypeName { get; }
        
        /// <summary>
        /// Initializes a new instance of the TDoublesException with the specified member and type names.
        /// </summary>
        /// <param name="memberName">The name of the member that lacks a mock implementation. Cannot be null.</param>
        /// <param name="typeName">The name of the type that contains the member. Cannot be null.</param>
        public TDoublesException(string memberName, string typeName) 
            : base($"No mock override provided for member '{memberName}' in type '{typeName}' that returns a non-nullable reference type.")
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }
        
        /// <summary>
        /// Initializes a new instance of the TDoublesException with the specified member name, type name, and custom message.
        /// </summary>
        /// <param name="memberName">The name of the member that lacks a mock implementation. Cannot be null.</param>
        /// <param name="typeName">The name of the type that contains the member. Cannot be null.</param>
        /// <param name="message">The custom error message.</param>
        public TDoublesException(string memberName, string typeName, string message) 
            : base(message)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }
    }
}
""";

            context.AddSource(Constants.SystemTypeHintNamePrefix + "TDoublesException.g.cs", SourceText.From(exceptionSource, Encoding.UTF8));
        }
    }
}
