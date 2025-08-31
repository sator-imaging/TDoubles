using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IRefOutParameters
{
    void RefMethod(ref string value);
    void OutMethod(out int value);
    void InMethod(in double value);
    void ParamsMethod(params string[] args);
}

[Mock(typeof(IRefOutParameters))]
public partial class MockRefOutParameters
{
}

public class Test13_RefOutParameterHandling
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockRefOutParameters();
        exitCode += ValidationHelper.ValidateImplementation<IRefOutParameters, MockRefOutParameters>(mock);

        // Pre-override existence checks
        string refValue = "initial";
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.RefMethod(ref refValue), "invoke RefMethod(ref string)");
        int outValue;
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.OutMethod(out outValue), "TDoubles.TDoublesException", "OutMethod");
        double inValue = 3.14;
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.InMethod(in inValue), "invoke InMethod(in double)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.ParamsMethod("a", "b"), "invoke ParamsMethod(params string[])");

        // Apply overrides and validate mutated values
        mock.MockOverrides.RefMethod = (ref string value) => { value = "overridden_ref"; };
        mock.MockOverrides.OutMethod = (out int value) => { value = 42; };
        mock.MockOverrides.InMethod = (in double value) => { };
        mock.MockOverrides.ParamsMethod = (args) => { Console.WriteLine($"ParamsMethod called with {args.Length} arguments."); };

        string refValue2 = "initial";
        exitCode += ValidationHelper.ValidateAction(() => mock.RefMethod(ref refValue2), "call RefMethod");
        exitCode += ValidationHelper.ValidateMember(refValue2, v => v, "overridden_ref");

        int outValue2;
        exitCode += ValidationHelper.ValidateAction(() => mock.OutMethod(out outValue2), "call OutMethod");
        outValue2 = 0;
        mock.OutMethod(out outValue2);
        exitCode += ValidationHelper.ValidateMember(outValue2, v => v, 42);

        double inValue2 = 3.14;
        exitCode += ValidationHelper.ValidateAction(() => mock.InMethod(in inValue2), "call InMethod");

        exitCode += ValidationHelper.ValidateAction(() => mock.ParamsMethod("arg1", "arg2", "arg3"), "call ParamsMethod");
        // TODO: Add validation for ParamsMethod override

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
