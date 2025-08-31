using System;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public interface ITestEventInterface
{
    event Action TestEvent;
}

[Mock(typeof(ITestEventInterface))]
public partial class MockITestEventInterface
{
}

public class Test04_EventMocking
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockITestEventInterface();
        exitCode += ValidationHelper.ValidateImplementation<ITestEventInterface, MockITestEventInterface>(mock);

        // Pre-override event add/remove and object members existence
        Action testHandler = () => { };
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.TestEvent += testHandler, "TDoubles.TDoublesException", "TestEvent");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.TestEvent -= testHandler, "TDoubles.TDoublesException", "TestEvent");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and exercise event add/remove
        mock.MockOverrides.TestEvent__add = handler => { };
        mock.MockOverrides.TestEvent__remove = handler => { };
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.TestEvent += testHandler, "add TestEvent (overridden)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.TestEvent -= testHandler, "remove TestEvent (overridden)");

        // Override and validate object members
        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
