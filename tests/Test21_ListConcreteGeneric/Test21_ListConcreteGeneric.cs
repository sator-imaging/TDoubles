using System.Collections;
using System.Collections.Generic;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

[Mock(typeof(List<string>))]
public partial class MockListString
{
}

public class Test21_ListConcreteGeneric
{
    static int Main()
    {
        int exitCode = 0;
        var mock = new MockListString();
        exitCode += ValidationHelper.ValidateImplementation<List<string>, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<string>, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<string>, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IList, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IList<string>, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyCollection<string>, MockListString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyList<string>, MockListString>(mock);

        // Pre-override existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Add("42"), "invoke Add(item)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Remove("42"), "invoke Remove(item)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Clear(), "invoke Clear()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Contains("42"), "invoke Contains(item)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Count, "get Count");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m[0], "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m[0] = "42", "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate return-bearing members
        mock.MockOverrides.Add = item => { };
        mock.MockOverrides.Remove = item => true;
        mock.MockOverrides.Clear = () => { };
        mock.MockOverrides.Contains = item => true;
        mock.MockOverrides.Count__get = () => 42;
        mock.MockOverrides.This__get = index => "999";
        mock.MockOverrides.This__set = (index, value) => { };

        exitCode += ValidationHelper.ValidateAction(() => mock.Add("1"), "call Add");
        exitCode += ValidationHelper.ValidateAction(() => mock.Clear(), "call Clear");
        exitCode += ValidationHelper.ValidateAction(() => mock[1] = "2", "set indexer");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Remove("1"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Contains("2"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Count, 42);
        exitCode += ValidationHelper.ValidateCall(mock, m => m[0], "999");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);
        return exitCode;
    }
}
