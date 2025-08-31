using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    public class ParameterBlueprint
    {
        /// <summary>
        /// Gets or sets the parameter symbol for Roslyn-based operations.
        /// This is the source of truth for all parameter information.
        /// </summary>
        public IParameterSymbol ParameterSymbol { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether the parameter is ref.
        /// </summary>
        public bool IsRef { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter is out.
        /// </summary>
        public bool IsOut { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter is in.
        /// </summary>
        public bool IsIn { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter is params.
        /// </summary>
        public bool IsParams { get; set; }
    }
}
