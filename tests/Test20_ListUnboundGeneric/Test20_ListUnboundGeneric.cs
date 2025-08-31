using System.Collections;
using System.Collections.Generic;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

[Mock(typeof(List<>))]
public partial class MockList<A>
{
}

public class Test20_ListUnboundGeneric
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockList<string>();
        exitCode += ValidationHelper.ValidateImplementation<List<string>, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<string>, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<string>, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IList, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IList<string>, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyCollection<string>, MockList<string>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyList<string>, MockList<string>>(mock);

        // Pre-override existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Add("test"), "invoke Add(item)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Remove("test"), "invoke Remove(item)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Clear(), "invoke Clear()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Contains("test"), "invoke Contains(item)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Count, "get Count");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m[0], "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m[0] = "test", "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate return-bearing members
        mock.MockOverrides.Add = item => { };
        mock.MockOverrides.Remove = item => true;
        mock.MockOverrides.Clear = () => { };
        mock.MockOverrides.Contains = item => true;
        mock.MockOverrides.Count__get = () => 42;
        mock.MockOverrides.This__get = index => "overridden";
        mock.MockOverrides.This__set = (index, value) => { };

        exitCode += ValidationHelper.ValidateAction(() => mock.Add("x"), "call Add");
        exitCode += ValidationHelper.ValidateAction(() => mock.Clear(), "call Clear");
        exitCode += ValidationHelper.ValidateAction(() => mock[1] = "y", "set indexer");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Remove("x"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Contains("any"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Count, 42);
        exitCode += ValidationHelper.ValidateCall(mock, m => m[0], "overridden");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
