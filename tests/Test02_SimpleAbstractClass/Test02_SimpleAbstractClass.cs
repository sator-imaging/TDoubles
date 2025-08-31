using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public abstract class TestAbstractClass
{
    public abstract string TestMethod();
}

[Mock(typeof(TestAbstractClass))]
public partial class MockTestAbstractClass
{
}

public class Test02_SimpleAbstractClass
{
    static int Main()
    {
        int exitCode = 0;

        // Validate inheritance and initial member availability
        var mock = new MockTestAbstractClass();
        exitCode += ValidationHelper.ValidateImplementation<TestAbstractClass, MockTestAbstractClass>(mock);

        // Validate behavior before any overrides: non-nullable reference return should throw
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate results
        mock.MockOverrides.TestMethod = () => "overridden_value";
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "overridden_value");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
