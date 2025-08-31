using System.Collections.Generic;

namespace TDoubles.DataModels
{
    public class ConflictResolutionMap
    {
        /// <summary>
        /// Gets or sets the mapping from original names to resolved names.
        /// </summary>
        public Dictionary<UnifiedMemberBlueprint, string> ResolvedNames { get; set; } = new Dictionary<UnifiedMemberBlueprint, string>();

        /// <summary>
        /// Gets or sets the conflict groups.
        /// </summary>
        public Dictionary<string, List<string>> ConflictGroups { get; set; } = new Dictionary<string, List<string>>();

        ///// <summary>
        ///// Gets or sets the explicit interface member names.
        ///// </summary>
        //public List<string> ExplicitInterfaceMembers { get; set; } = new List<string>();
    }
}
