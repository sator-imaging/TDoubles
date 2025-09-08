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
partial class MockWithExcludeNamesStmg { public void __STMG_A() { } }

[Mock(typeof(PublicAndInternalMembers), new[] { "__FOO_A", "__FOO_B" }, IncludeInternals = false)]
partial class MockWithExcludeNamesFoo { public void __FOO_A() { } }

[Mock(typeof(PublicAndInternalMembers), new string[] { "__BAR_A" }, IncludeInternals = false)]
partial class MockWithExcludeNamesBar { public void __BAR_A() { } }

[Mock(typeof(PublicAndInternalMembers), new string[0], IncludeInternals = false)]
partial class MockWithExcludeNamesNothing { }


public class Empty { }

[Mock(typeof(Empty), "TestMethod", "ToString", "Equals", "GetHashCode")]
partial class MockEmpty
{
    public void TestMethod() { }
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

        var model = GeneratorValidationModel.Create();
        string source;

        exitCode += ValidationHelper.GetGeneratedSource(model, "MockWithExcludeNamesStmg", out source);
        exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: '__STMG_A', '__STMG_B', '__STMG_C'");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " __STMG_A(");

        exitCode += ValidationHelper.GetGeneratedSource(model, "MockWithExcludeNamesFoo", out source);
        exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: '__FOO_A', '__FOO_B'");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " __FOO_A(");

        exitCode += ValidationHelper.GetGeneratedSource(model, "MockWithExcludeNamesBar", out source);
        exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: '__BAR_A'");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " __BAR_A(");

        exitCode += ValidationHelper.GetGeneratedSource(model, "MockWithExcludeNamesNothing", out source);
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "/// Excluded members:");

        exitCode += ValidationHelper.GetGeneratedSource(model, "MockEmpty", out source);
        exitCode += ValidationHelper.ValidateContains(source, "/// Excluded members: 'TestMethod', 'ToString', 'Equals', 'GetHashCode'");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " ToString(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " Equals(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " GetHashCode(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " TestMethod(");

        if (exitCode != 0)
        {
            Console.WriteLine(source);
        }

        return exitCode;
    }
}
