using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestPropertyInterface
{
    string TestProperty { get; set; }
}

[Mock(typeof(ITestPropertyInterface))]
public partial class MockITestPropertyInterface
{
}

public class Test03_PropertyMocking
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockITestPropertyInterface();
        exitCode += ValidationHelper.ValidateImplementation<ITestPropertyInterface, MockITestPropertyInterface>(mock);

        // Validate property and object member existence before any overrides
        // Getter returns non-nullable reference -> should throw before override
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestProperty, "TDoubles.TDoublesException", "TestProperty");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.TestProperty = "test_value", "TDoubles.TDoublesException", "TestProperty");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate behavior
        mock.MockOverrides.TestProperty__get = () => "overridden__get";
        mock.MockOverrides.TestProperty__set = value => { };
        exitCode += ValidationHelper.ValidateProperty(
            mock,
            m => m.TestProperty,
            (m, v) => m.TestProperty = v,
            setValue: "any",
            expectedAfter: "overridden__get"
        );

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
