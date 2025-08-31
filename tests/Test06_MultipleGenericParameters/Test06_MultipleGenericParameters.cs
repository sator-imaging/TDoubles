using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestMultiGenericInterface<T1, T2, T3>
{
    T1 TestMethodA();
    T2 TestMethodB();
    T3 TestMethodC();
}

[Mock(typeof(ITestMultiGenericInterface<,,>))]
public partial class MockITestMultiGenericInterface<A, B, C>
{
}

public class Test06_MultipleGenericParameters
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockITestMultiGenericInterface<string, int, bool>();
        exitCode += ValidationHelper.ValidateImplementation<ITestMultiGenericInterface<string, int, bool>, MockITestMultiGenericInterface<string, int, bool>>(mock);

        // Pre-override existence checks
        // A is string (non-nullable reference) -> should throw
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethodA(), "TDoubles.TDoublesException", "TestMethodA");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethodB(), "TDoubles.TDoublesException", "TestMethodB");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethodC(), "TDoubles.TDoublesException", "TestMethodC");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.TestMethodA = () => "overridden_A";
        mock.MockOverrides.TestMethodB = () => 42;
        mock.MockOverrides.TestMethodC = () => true;

        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethodA(), "overridden_A");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethodB(), 42);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethodC(), true);

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
