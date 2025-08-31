using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public class BaseClass
{
    public virtual string BaseMethod() => "base";
}

public class MiddleClass : BaseClass
{
    public virtual string MiddleMethod() => "middle";
}

public class TargetClass : MiddleClass
{
    public virtual string TargetMethod() => "target";
}

[Mock(typeof(TargetClass))]
public partial class MockTargetClass
{
}

public class Test10_InheritanceChain
{
    static int Main()
    {
        int exitCode = 0;

        // Validate inheritance chain and initial calls
        var mock = new MockTargetClass();
        exitCode += ValidationHelper.ValidateImplementation<TargetClass, MockTargetClass>(mock);
        exitCode += ValidationHelper.ValidateImplementation<MiddleClass, MockTargetClass>(mock);
        exitCode += ValidationHelper.ValidateImplementation<BaseClass, MockTargetClass>(mock);

        // Non-nullable reference returns throw pre-override
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.BaseMethod(), "TDoubles.TDoublesException", "BaseMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.MiddleMethod(), "TDoubles.TDoublesException", "MiddleMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.TargetMethod(), "TDoubles.TDoublesException", "TargetMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.BaseMethod = () => "overridden_base";
        mock.MockOverrides.MiddleMethod = () => "overridden_middle";
        mock.MockOverrides.TargetMethod = () => "overridden_target";

        exitCode += ValidationHelper.ValidateCall(mock, m => m.BaseMethod(), "overridden_base");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.MiddleMethod(), "overridden_middle");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.TargetMethod(), "overridden_target");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
