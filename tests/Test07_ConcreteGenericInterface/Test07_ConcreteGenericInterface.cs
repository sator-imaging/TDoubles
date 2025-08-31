using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestGenericInterface<T>
{
    T TestMethod();
}

// Concrete generic interface test: close a generic interface with a concrete type argument (int)
[Mock(typeof(ITestGenericInterface<int>))]
public partial class MockITestGenericInterfaceInt
{
}

public class Test07_ConcreteGenericInterface
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation with concrete generic argument
        var mock = new MockITestGenericInterfaceInt();
        exitCode += ValidationHelper.ValidateImplementation<ITestGenericInterface<int>, MockITestGenericInterfaceInt>(mock);

        // Pre-override existence checks: int return should be callable (default)
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.TestMethod(), "invoke TestMethod()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.TestMethod = () => 42;
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), 42);

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
