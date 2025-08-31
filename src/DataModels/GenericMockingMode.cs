namespace TDoubles.DataModels
{
    /// <summary>
    /// Defines the different modes for handling generic types in mock generation.
    /// </summary>
    public enum GenericMockingMode
    {
        /// <summary>
        /// Target type is not generic.
        /// Example: typeof(NonGenericType)
        /// </summary>
        NonGeneric,

        /// <summary>
        /// Mock attribute uses unbound generic syntax with omitted type arguments.
        /// Requires matching type argument count between mock class and target type.
        /// Example: typeof(IDictionary&lt;,&gt;) - mock class must have 2 type parameters
        /// </summary>
        UnboundGeneric,

        /// <summary>
        /// Mock attribute uses closed constructed generic syntax with specific type arguments.
        /// Ignores mock class type arguments and works with closed constructed target type.
        /// Example: typeof(IDictionary&lt;int, string&gt;) - mock class type args are ignored
        /// </summary>
        ClosedGeneric
    }
}
