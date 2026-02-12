using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IOverloadedMethods
{
    string TestMethod(bool value = true);
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
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(false), "TDoubles.TDoublesException", "TestMethod");

        // Apply overrides and validate results
        mock.MockOverrides.TestMethod = value => value.ToString();

        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "true");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(false), "false");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(true), "true");

        return exitCode;
    }
}
