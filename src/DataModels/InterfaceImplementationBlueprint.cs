using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TDoubles.DataModels
{
    public class InterfaceImplementationBlueprint
    {
        /// <summary>
        /// Gets or sets the interface type symbol.
        /// </summary>
        public INamedTypeSymbol? InterfaceType { get; set; }

        /// <summary>
        /// Gets or sets whether this interface requires explicit implementation.
        /// </summary>
        public bool RequiresExplicitImplementation { get; set; }

        ///// <summary>
        ///// Gets or sets the members that belong to this interface.
        ///// </summary>
        //public List<UnifiedMemberBlueprint> Members { get; set; } = new List<UnifiedMemberBlueprint>();
    }
}
