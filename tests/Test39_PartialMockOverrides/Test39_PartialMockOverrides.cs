using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ICommonInterface<T>
{
    T TestMethod();
}

public interface IIntTest: ICommonInterface<int>
{
}

public interface IDoubleTest: ICommonInterface<double>
{
}

[Mock(typeof(IIntTest))]
public partial class MockIntTest
{
    public partial class MockOverrideContainer : ICommonInterfaceMock<int>
    {
    }
}


[Mock(typeof(IDoubleTest))]
public partial class MockDoubleTest
{
    public partial class MockOverrideContainer : ICommonInterfaceMock<double>
    {
    }
}

public interface ICommonInterfaceMock<T>
{
    Func<T> TestMethod { get; set; }
}

public class Test39_PartialMockOverrides
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mockInt = new MockIntTest();
        exitCode += ValidationHelper.ValidateImplementation<ICommonInterface<int>, MockIntTest>(mockInt);

        var mockDouble = new MockDoubleTest();
        exitCode += ValidationHelper.ValidateImplementation<ICommonInterface<double>, MockDoubleTest>(mockDouble);

        // Apply overrides and validate results
        SetupTestMethodOverride(mockInt.MockOverrides);
        SetupTestMethodOverride(mockDouble.MockOverrides);

        exitCode += ValidationHelper.ValidateCall(mockInt, m => m.TestMethod(), default(int));
        exitCode += ValidationHelper.ValidateCall(mockDouble, m => m.TestMethod(), default(double));
        return exitCode;
    }

    static void SetupTestMethodOverride<T>(ICommonInterfaceMock<T> mockOverrides)
    {
        // in real life we would have more complex logic here and maybe not only one method, but for test purposes we just want to validate that partial overrides work as expected
        mockOverrides.TestMethod = () => default(T);
    }
}
