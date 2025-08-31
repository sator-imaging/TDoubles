using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestInterface1
{
    string Method1();
}

public interface ITestInterface2
{
    int Method2();
}

// Combine both interfaces into a single target for mocking
public interface ICombinedInterfaces : ITestInterface1, ITestInterface2 { }

[Mock(typeof(ICombinedInterfaces))]
public partial class MockMultipleInterfaces
{
}

public class Test09_MultipleInterfaceImplementation
{
    static int Main()
    {
        int exitCode = 0;

        // Validate both interface implementations via combined interface
        var mock = new MockMultipleInterfaces();
        exitCode += ValidationHelper.ValidateImplementation<ICombinedInterfaces, MockMultipleInterfaces>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ITestInterface1, MockMultipleInterfaces>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ITestInterface2, MockMultipleInterfaces>(mock);

        // Pre-override existence checks and object members
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.Method1(), "TDoubles.TDoublesException", "Method1");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Method2(), "invoke Method2()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.Method1 = () => "overridden_method1";
        mock.MockOverrides.Method2 = () => 42;
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Method1(), "overridden_method1");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Method2(), 42);

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
