using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestInterface
{
    string TestMethod();
}

[Mock(typeof(ITestInterface))]
public partial class MockITestInterface
{
}

public class Test01_SimpleInterfaceMethod
{
    static int Main()
    {
        int exitCode = 0;

        // Create mock and validate interface implementation
        var mock = new MockITestInterface();
        exitCode += ValidationHelper.ValidateImplementation<ITestInterface, MockITestInterface>(mock);

        // First, validate member behavior before any overrides.
        // Non-nullable reference return should throw until an override is provided.
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Now override method via MockOverrides and validate the call result
        mock.MockOverrides.TestMethod = () => "overridden_value";
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "overridden_value");

        // Override object members and validate each result
        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
