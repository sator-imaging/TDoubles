using System;
using System.Reflection;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public interface IMock
{
    void IMockMethod();
}

public interface IOther
{
    void IOtherMethod();
    void ExplicitInterfaceImpl();
}

[Mock(typeof(IMock))]
public partial class MockDeclaringMemberExclusion : IMock, IOther
{
    public void IMockMethod() { }

    public void IOtherMethod() { }
    void IOther.ExplicitInterfaceImpl() { }
    public void ExplicitInterfaceImpl() { }

    public void ThisMethodMustNotBeMocked() { }
    public long ThisPropertyMustNotBeMocked { get; set; }
    public string this[int index] { get => string.Empty; set { } }

    partial void OnWillMockCall(string memberName)
    {
        throw new NotImplementedException();
    }
}

public class Test36_MockDeclaringMemberExclusion
{
    public static int Main()
    {
        int exitCode = 0;

        // Test MockDeclaringMemberExclusion
        var mock = new MockDeclaringMemberExclusion();
        exitCode += ValidationHelper.ValidateImplementation<IMock, MockDeclaringMemberExclusion>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IOther, MockDeclaringMemberExclusion>(mock);
        // Further checks for member existence
        exitCode += ValidationHelper.ValidateMemberExists(mock, "IMockMethod", "IMockMethod not found in MockDeclaringMemberExclusion");
        exitCode += ValidationHelper.ValidateMemberExists(mock, "IOtherMethod", "IOtherMethod not found in MockDeclaringMemberExclusion");

        // exitCode += ValidationHelper.ValidateMemberDoesNotExist(mock, "ThisMethodMustNotBeMocked", "ThisMethodMustNotBeMocked should not be mocked in MockDeclaringMemberExclusion");
        // exitCode += ValidationHelper.ValidateMemberDoesNotExist(mock, "ThisPropertyMustNotBeMocked", "ThisPropertyMustNotBeMocked should not be mocked in MockDeclaringMemberExclusion");
        // exitCode += ValidationHelper.ValidateMemberDoesNotExist(mock, "Item", "Item(indexer) should not be mocked in MockDeclaringMemberExclusion");

        var model = GeneratorValidationModel.Create();
        exitCode += ValidationHelper.GetGeneratedSource(model, "MockDeclaringMemberExclusion", out var source);
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " IMockMethod(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " IOtherMethod(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " IOther.ExplicitInterfaceImpl(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " ExplicitInterfaceImpl(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " ThisMethodMustNotBeMocked(");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " ThisPropertyMustNotBeMocked");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " this");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, " new void OnWillMockCall(");

        return exitCode;
    }
}
