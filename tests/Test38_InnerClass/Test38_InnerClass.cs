using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IInnerClass
{
    string TestMethod();
}
public partial class OuterClass
{
    [Mock(typeof(IInnerClass))]
    private partial class MockOverloadedMethods
    {
    }

    public int Validate()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockOverloadedMethods();
        exitCode += ValidationHelper.ValidateImplementation<IInnerClass, MockOverloadedMethods>(mock);

        // Pre-override existence checks and object members
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TestMethod(), "TDoubles.TDoublesException", "TestMethod");

        // Apply overrides and validate results
        mock.MockOverrides.TestMethod = () => "true";

        exitCode += ValidationHelper.ValidateCall(mock, m => m.TestMethod(), "true");
        return exitCode;
    }
}

public class Test38_InnerClass
{
    static int Main()
    {
        return new OuterClass().Validate();
    }
}
