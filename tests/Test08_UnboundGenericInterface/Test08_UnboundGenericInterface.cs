using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestUnboundGenericInterface<T>
{
    T TestMethod();
}

[Mock(typeof(ITestUnboundGenericInterface<>))]
public partial class MockITestUnboundGenericInterface<A>
{
}

public class Test08_UnboundGenericInterface
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockITestUnboundGenericInterface<double>();
        exitCode += ValidationHelper.ValidateImplementation<ITestUnboundGenericInterface<double>, MockITestUnboundGenericInterface<double>>(mock);

        // Pre-override: unbound generic without constraints -> fail-first
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.TestMethod = () => 3.14;
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), 3.14);

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
