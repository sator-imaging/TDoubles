using System.Collections.Generic;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;
using System.IO;

public class CustomClass
{
    public string Value { get; set; }
}

public interface IComplexParameters
{
    string ArrayMethod(string[] array);
    T GenericMethod<T>(List<T> list);
    void CustomMethod(CustomClass custom);
}

[Mock(typeof(IComplexParameters))]
public partial class MockComplexParameters
{
}

public class Test12_ComplexParameterTypes
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockComplexParameters();
        exitCode += ValidationHelper.ValidateImplementation<IComplexParameters, MockComplexParameters>(mock);

        // Pre-override existence checks
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.ArrayMethod(new string[] { "test" }), "TDoubles.TDoublesException", "ArrayMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.GenericMethod<int>(new List<int> { 1, 2, 3 }), "TDoubles.TDoublesException", "GenericMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.CustomMethod(new CustomClass { Value = "test" }), "invoke CustomMethod(CustomClass)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate results
        mock.MockOverrides.ArrayMethod = array => "overridden_array";
        // Generic method uses method-level type arg; override property uses object signature
        mock.MockOverrides.GenericMethod = new Func<object, object>(obj =>
        {
            var list = (List<int>)obj;
            return list.Count > 0 ? list[0] : default(int);
        });
        mock.MockOverrides.CustomMethod = custom => { };

        exitCode += ValidationHelper.ValidateCall(mock, m => m.ArrayMethod(new string[] { "test" }), "overridden_array");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.GenericMethod<int>(new List<int> { 1, 2, 3 }), 1);
        exitCode += ValidationHelper.ValidateAction(() => mock.CustomMethod(new CustomClass { Value = "x" }), "call CustomMethod");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
