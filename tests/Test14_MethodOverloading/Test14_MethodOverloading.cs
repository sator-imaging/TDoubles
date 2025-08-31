using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IOverloadedMethods
{
    string TestMethod();
    string TestMethod(int value);
    string TestMethod(string value);
    string TestMethod(int value1, string value2);
}

[Mock(typeof(IOverloadedMethods))]
public partial class MockOverloadedMethods
{
}

public class Test14_MethodOverloading
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockOverloadedMethods();
        exitCode += ValidationHelper.ValidateImplementation<IOverloadedMethods, MockOverloadedMethods>(mock);

        // Pre-override existence checks and object members
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(42), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod("test"), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(42, "test"), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate results
        mock.MockOverrides.TestMethod = () => "overridden_no_params";
        mock.MockOverrides.TestMethod_int = value => "overridden_int";
        mock.MockOverrides.TestMethod_string = value => "overridden_string";
        mock.MockOverrides.TestMethod_int_string = (value1, value2) => "overridden_int_string";

        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "overridden_no_params");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(42), "overridden_int");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod("test"), "overridden_string");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(42, "test"), "overridden_int_string");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
