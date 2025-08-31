using System;
using System.Reflection;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public class PublicAndInternalMembers
{
    public void PublicMethod() { }
    public bool PublicProperty => true;

    internal void InternalMethod() { }
    internal bool InternalProperty { get; set; } = false;
}

[Mock(typeof(PublicAndInternalMembers))]
partial class MockPublicMembersOnly { }

[Mock(typeof(PublicAndInternalMembers), IncludeInternals = true)]
partial class MockAllMembers { }


[Mock(typeof(PublicAndInternalMembers), "__STMG_A", "__STMG_B", "__STMG_C")]
partial class MockWithExcludeNamesStmg { }

[Mock(typeof(PublicAndInternalMembers), new[] { "__FOO_A", "__FOO_B" }, IncludeInternals = false)]
partial class MockWithExcludeNamesFoo { }

[Mock(typeof(PublicAndInternalMembers), new string[] { "__BAR_A" }, IncludeInternals = false)]
partial class MockWithExcludeNamesBar { }

[Mock(typeof(PublicAndInternalMembers), new string[0], IncludeInternals = false)]
partial class MockWithExcludeNamesNothing { }


public class Empty { }

[Mock(typeof(Empty), "ToString", "Equals", "GetHashCode")]
partial class MockEmpty
{
}

public class Test33_MockAttributeOptions
{
    public static int Main()
    {
        int exitCode = 0;

        // Test MockPublicMembersOnly
        var mockPublicMembersOnly = new MockPublicMembersOnly();
        exitCode += ValidationHelper.ValidateImplementation<PublicAndInternalMembers, MockPublicMembersOnly>(mockPublicMembersOnly);
        // Further checks for public members only
        exitCode += ValidationHelper.ValidateMemberExists(mockPublicMembersOnly, "PublicMethod", "PublicMethod not found in MockPublicMembersOnly");
        exitCode += ValidationHelper.ValidateMemberExists(mockPublicMembersOnly, "PublicProperty", "PublicProperty not found in MockPublicMembersOnly");
        exitCode += ValidationHelper.ValidateMemberDoesNotExist(mockPublicMembersOnly, "InternalMethod", "InternalMethod found in MockPublicMembersOnly");
        exitCode += ValidationHelper.ValidateMemberDoesNotExist(mockPublicMembersOnly, "InternalProperty", "InternalProperty found in MockPublicMembersOnly");

        // Test MockAllMembers
        var mockAllMembers = new MockAllMembers();
        exitCode += ValidationHelper.ValidateImplementation<PublicAndInternalMembers, MockAllMembers>(mockAllMembers);
        // Further checks for all members
        exitCode += ValidationHelper.ValidateMemberExists(mockAllMembers, "PublicMethod", "PublicMethod not found in MockAllMembers");
        exitCode += ValidationHelper.ValidateMemberExists(mockAllMembers, "PublicProperty", "PublicProperty not found in MockAllMembers");
        exitCode += ValidationHelper.ValidateMemberExists(mockAllMembers, "InternalMethod", "InternalMethod not found in MockAllMembers");
        exitCode += ValidationHelper.ValidateMemberExists(mockAllMembers, "InternalProperty", "InternalProperty not found in MockAllMembers");

        // Test MockWithExcludeNamesStmg
        var mockWithExcludeNamesStmg = new MockWithExcludeNamesStmg();
        exitCode += ValidationHelper.ValidateImplementation<PublicAndInternalMembers, MockWithExcludeNamesStmg>(mockWithExcludeNamesStmg);

        // Test MockWithExcludeNamesFoo
        var mockWithExcludeNamesFoo = new MockWithExcludeNamesFoo();
        exitCode += ValidationHelper.ValidateImplementation<PublicAndInternalMembers, MockWithExcludeNamesFoo>(mockWithExcludeNamesFoo);

        // Test MockWithExcludeNamesBar
        var mockWithExcludeNamesBar = new MockWithExcludeNamesBar();
        exitCode += ValidationHelper.ValidateImplementation<PublicAndInternalMembers, MockWithExcludeNamesBar>(mockWithExcludeNamesBar);

        // Test MockWithExcludeNamesNothing
        var mockWithExcludeNamesNothing = new MockWithExcludeNamesNothing();
        exitCode += ValidationHelper.ValidateImplementation<PublicAndInternalMembers, MockWithExcludeNamesNothing>(mockWithExcludeNamesNothing);

        // Test MockEmpty
        var mockEmpty = new MockEmpty();
        exitCode += ValidationHelper.ValidateImplementation<Empty, MockEmpty>(mockEmpty);
        // Further checks for excluded names (this will require more specific checks based on the actual generated names)
        exitCode += ValidationHelper.ValidateMemberDoesNotExist(mockEmpty, "ToString", "ToString should not be mocked in MockEmpty");
        exitCode += ValidationHelper.ValidateMemberDoesNotExist(mockEmpty, "Equals", "Equals should not be mocked in MockEmpty");
        exitCode += ValidationHelper.ValidateMemberDoesNotExist(mockEmpty, "GetHashCode", "GetHashCode should not be mocked in MockEmpty");

        var model = GeneratorValidationModel.Create("tests/Test33_MockAttributeOptions");
        var sources = ValidationHelper.GetGeneratedSources(model);
        foreach (var (hintName, source) in sources)
        {
            Console.WriteLine($"------- {hintName} -------");
            // Console.WriteLine(source);

            if (hintName.StartsWith("MockWithExcludeNamesStmg", StringComparison.Ordinal))
            {
                exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: '__STMG_A', '__STMG_B', '__STMG_C'");
            }
            if (hintName.StartsWith("MockWithExcludeNamesFoo", StringComparison.Ordinal))
            {
                exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: '__FOO_A', '__FOO_B'");
            }
            if (hintName.StartsWith("MockWithExcludeNamesBar", StringComparison.Ordinal))
            {
                exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: '__BAR_A'");
            }
            if (hintName.StartsWith("MockWithExcludeNamesNothing", StringComparison.Ordinal))
            {
                exitCode += ValidationHelper.ValidateDoesNotContain(source, "/// Excluded members:");
            }
            if (hintName.StartsWith("MockEmpty", StringComparison.Ordinal))
            {
                exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: 'ToString', 'Equals', 'GetHashCode'");

                exitCode += ValidationHelper.ValidateDoesNotContain(source, " ToString(");
                exitCode += ValidationHelper.ValidateDoesNotContain(source, " Equals(");
                exitCode += ValidationHelper.ValidateDoesNotContain(source, " GetHashCode(");
            }
        }

        return exitCode;
    }
}
