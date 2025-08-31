using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IExplicitInterface
{
    string ExplicitMethod();
}

// Concrete class with explicit interface implementation
public class ExplicitClass : IExplicitInterface
{
    string IExplicitInterface.ExplicitMethod() => "base";
}

[Mock(typeof(ExplicitClass))]
public partial class MockExplicitInterface
{
}

public class Test11_ExplicitInterfaceImplementation
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface and class implementation
        var mock = new MockExplicitInterface();
        exitCode += ValidationHelper.ValidateImplementation<IExplicitInterface, MockExplicitInterface>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ExplicitClass, MockExplicitInterface>(mock);

        // Pre-override: explicit interface member must be called via interface cast; non-nullable return -> fail-first
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = ((IExplicitInterface)m).ExplicitMethod(), "TDoubles.TDoublesException", "ExplicitMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply override and validate via interface cast
        mock.MockOverrides.IExplicitInterface_ExplicitMethod = () => "overridden_explicit";
        exitCode += ValidationHelper.ValidateCall(mock, m => ((IExplicitInterface)m).ExplicitMethod(), "overridden_explicit");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
