using System;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute
{
    public string Value { get; set; }
}

public interface IAttributedInterface
{
    string AttributedMethod();
    
    string AttributedProperty { get; set; }
}

[Mock(typeof(IAttributedInterface))]
public partial class MockAttributedInterface
{
}

public class Test15_AttributePreservation
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockAttributedInterface();
        exitCode += ValidationHelper.ValidateImplementation<IAttributedInterface, MockAttributedInterface>(mock);

        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.AttributedMethod(), "TDoubles.TDoublesException", "AttributedMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.AttributedProperty, "TDoubles.TDoublesException", "AttributedProperty");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.AttributedProperty = "test", "TDoubles.TDoublesException", "AttributedProperty");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        mock.MockOverrides.AttributedMethod = () => "overridden";
        mock.MockOverrides.AttributedProperty__get = () => "overridden";
        mock.MockOverrides.AttributedProperty__set = value => { };

        exitCode += ValidationHelper.ValidateCall(mock, m => m.AttributedMethod(), "overridden");
        exitCode += ValidationHelper.ValidateProperty(mock, m => m.AttributedProperty, (m, v) => m.AttributedProperty = v, setValue: "any", expectedAfter: "overridden");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
