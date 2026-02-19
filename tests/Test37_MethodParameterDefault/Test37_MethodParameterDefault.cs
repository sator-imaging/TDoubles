using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IOverloadedMethods
{
    string TestMethod(bool value = true);
    object? RefTypeTestMethod(object? value = null);
    int StructTestMethod(int value = default);
    int StructTestMethodNonDefault(int value = 42);
}

[Mock(typeof(IOverloadedMethods))]
public partial class MockOverloadedMethods
{
}

public class Test37_MethodParameterDefault
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockOverloadedMethods();
        exitCode += ValidationHelper.ValidateImplementation<IOverloadedMethods, MockOverloadedMethods>(mock);

        // Pre-override existence checks and object members
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(false), "TDoubles.TDoublesException", "TestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, mock => mock.RefTypeTestMethod(), "invoke RefTypeTestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, mock => mock.StructTestMethod(), "invoke StructTestMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, mock => mock.StructTestMethodNonDefault(), "invoke StructTestMethodNonDefault");

        // Apply overrides and validate results
        mock.MockOverrides.TestMethod = value => value.ToString();
        mock.MockOverrides.RefTypeTestMethod = value => value;
        mock.MockOverrides.StructTestMethod = value => value;
        mock.MockOverrides.StructTestMethodNonDefault = value => value * 2;

        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "True");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(false), "False");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(true), "True");

        var testObj = new object();
        exitCode += ValidationHelper.ValidateCall(mock, m => m.RefTypeTestMethod(), null);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.RefTypeTestMethod(testObj), testObj);

        exitCode += ValidationHelper.ValidateCall(mock, m => m.StructTestMethod(), 0);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.StructTestMethod(1), 1);

        exitCode += ValidationHelper.ValidateCall(mock, m => m.StructTestMethodNonDefault(), 84);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.StructTestMethodNonDefault(310), 620);

        return exitCode;
    }
}
