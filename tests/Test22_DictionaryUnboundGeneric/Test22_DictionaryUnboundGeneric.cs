using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

[Mock(typeof(Dictionary<,>))]
public partial class MockDictionary<A, B>
{
}

public class Test22_DictionaryUnboundGeneric
{
    static int Main()
    {
        int exitCode = 0;
        var mock = new MockDictionary<string, int>();
        exitCode += ValidationHelper.ValidateImplementation<Dictionary<string, int>, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<KeyValuePair<string, int>>, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<KeyValuePair<string, int>>, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IDictionary<string, int>, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyCollection<KeyValuePair<string, int>>, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyDictionary<string, int>, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IDictionary, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IDeserializationCallback, MockDictionary<string, int>>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ISerializable, MockDictionary<string, int>>(mock);

        // Pre-override existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Add("key", 42), "invoke Add(key,value)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Remove("key"), "invoke Remove(key)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Clear(), "invoke Clear()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ContainsKey("key"), "invoke ContainsKey(key)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ContainsValue(42), "invoke ContainsValue(value)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Count, "get Count");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m["key"], "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m["key"] = 42, "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.Add = (key, value) => { };
        mock.MockOverrides.Remove_TKey = key => true;
        mock.MockOverrides.Clear = () => { };
        mock.MockOverrides.ContainsKey = key => true;
        mock.MockOverrides.ContainsValue = value => true;
        mock.MockOverrides.Count__get = () => 42;
        mock.MockOverrides.This__get = key => 999;
        mock.MockOverrides.This__set = (key, value) => { };

        exitCode += ValidationHelper.ValidateAction(() => mock.Add("x", 1), "call Add");
        exitCode += ValidationHelper.ValidateAction(() => mock.Clear(), "call Clear");
        exitCode += ValidationHelper.ValidateAction(() => mock["y"] = 2, "set indexer");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Remove("x"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.ContainsKey("z"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.ContainsValue(2), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Count, 42);
        exitCode += ValidationHelper.ValidateCall(mock, m => m["key"], 999);

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
