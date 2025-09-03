namespace TDoubles
{
    /// <summary>
    /// Defines constant values used throughout the TDoubles project.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Suffix used for generated getter override properties.
        /// </summary>
        public const string GetterSuffix = "__get";
        /// <summary>
        /// Suffix used for generated setter override properties.
        /// </summary>
        public const string SetterSuffix = "__set";
        /// <summary>
        /// Suffix used for generated event adder override properties.
        /// </summary>
        public const string AdderSuffix = "__add";
        /// <summary>
        /// Suffix used for generated event remover override properties.
        /// </summary>
        public const string RemoverSuffix = "__remove";
        /// <summary>
        /// The name used for indexer properties in generated code.
        /// </summary>
        public const string IndexerName = "This";
        /// <summary>
        /// Suffix used for generated delegate types.
        /// </summary>
        public const string DelegateSuffix = "__d";
        /// <summary>
        /// The name of the unified callback method invoked before a mock call.
        /// </summary>
        public const string CallbackNameBefore = "OnWillMockCall";
        /// <summary>
        /// The name of the unified callback method invoked after a mock call.
        /// </summary>
        public const string CallbackNameAfter = "OnDidMockCall";
        /// <summary>
        /// Prefix for temporary variables generated in the mock code.
        /// </summary>
        public const string TempVariablePrefix = "__STMG_2025_08_25__";
        /// <summary>
        /// Name for the temporary return value variable.
        /// </summary>
        public const string TempReturnVariableName = TempVariablePrefix /*+ "RETVAL"*/;  // No suffix is most safe for avoiding naming conflict
        /// <summary>
        /// The internal name used by Roslyn for indexer properties.
        /// </summary>
        public const string IndexerInternalName = "Item";
        /// <summary>
        /// Represents the global namespace prefix.
        /// </summary>
        public const string GlobalNamespace = "global::";
        /// <summary>
        /// Fully qualified name for System.Action.
        /// </summary>
        public const string ActionFullName = GlobalNamespace + "System.Action";
        /// <summary>
        /// Fully qualified name for System.Func.
        /// </summary>
        public const string FuncFullName = GlobalNamespace + "System.Func";
        /// <summary>
        /// Fully qualified name for System.IEquatable.
        /// </summary>
        public const string IEquatableFullName = GlobalNamespace + "System.IEquatable";
        /// <summary>
        /// Fully qualified name for System.ValueType.
        /// </summary>
        public const string ValueTypeFullName = GlobalNamespace + "System.ValueType";
        /// <summary>
        /// The name of the generated override for IEquatable.Equals in record types.
        /// </summary>
        public const string RecordIEquatableEqualsOverrideName = "MockTargetRecord_Equals";
        /// <summary>
        /// The name of the field holding the mock target instance.
        /// </summary>
        public const string MockTargetInstanceFieldName = "_target";
        /// <summary>
        /// A string used to indicate an unexpected or error state in generated code.
        /// </summary>
        public const string ErrorString = "MUST_NOT_BE_REACHED";
        /// <summary>
        /// Prefix for hint names of generated system types.
        /// </summary>
        public const string SystemTypeHintNamePrefix = "_ " + nameof(TDoubles) + " _ ";  // Spaces to avoid naming conflict
        /// <summary>
        /// The diagnostic category for TDoubles-related diagnostics.
        /// </summary>
        public const string DiagnosticCategory = nameof(TDoubles);
    }
}
