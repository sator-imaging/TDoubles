using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    public class InheritanceStrategy
    {
        /// <summary>
        /// Gets or sets whether the mock should inherit from the target type.
        /// </summary>
        public bool ShouldInherit { get; set; }

        /// <summary>
        /// Gets or sets whether the target has overridable members.
        /// </summary>
        public bool HasOverridableMembers { get; set; }

        /// <summary>
        /// Gets or sets the list of overridable member symbols.
        /// </summary>
        public List<ISymbol> OverridableSymbols { get; set; } = new List<ISymbol>();
    }
}
