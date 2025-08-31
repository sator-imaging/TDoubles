using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestGenericInterface<T>
{
    T TestMethod();
}

[Mock(typeof(ITestGenericInterface<>))]
public partial class MockITestGenericInterface<A>
{
}

public class Test05_SingleGenericInterface
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockITestGenericInterface<string>();
        exitCode += ValidationHelper.ValidateImplementation<ITestGenericInterface<string>, MockITestGenericInterface<string>>(mock);

        // Pre-override: non-nullable reference return should throw
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.TestMethod = () => "overridden_generic_value";
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "overridden_generic_value");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
