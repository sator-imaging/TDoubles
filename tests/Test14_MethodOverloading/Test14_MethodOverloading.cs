using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;
using System.Collections.Generic;
using System.Linq;

public interface IOverloadedMethods
{
    string TestMethod();
    string TestMethod(int value);
    string TestMethod(string value);
    string TestMethod(int value1, string value2);

    T OverloadMethod<T>(IEnumerable<T> items);
    T OverloadMethod<T>(List<T> items);
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
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.OverloadMethod<int>(Enumerable.Empty<int>()), "TDoubles.TDoublesException", "OverloadMethod");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.OverloadMethod<int>(new List<int>()), "TDoubles.TDoublesException", "OverloadMethod");
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

        // OverloadMethod overrides: both use Func<object, object> due to method-level generic T
        mock.MockOverrides.OverloadMethod_IEnumerableT = new Func<object, object>(obj =>
        {
            var items = (IEnumerable<int>)obj;
            return items.Count();
        });

        mock.MockOverrides.OverloadMethod_ListT = new Func<object, object>(obj =>
        {
            var list = (List<int>)obj;
            return list.Count * 10;
        });

        exitCode += ValidationHelper.ValidateCall(mock, m => m.OverloadMethod<int>(new int[] { 1, 2, 3 }), 3);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.OverloadMethod<int>(new List<int> { 1, 2, 3 }), 30);

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
