using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

[Mock(typeof(Dictionary<long, string>))]
public partial class MockDictionaryLongString
{
}

public class Test23_DictionaryConcreteGeneric
{
    static int Main()
    {
        int exitCode = 0;
        var mock = new MockDictionaryLongString();
        exitCode += ValidationHelper.ValidateImplementation<Dictionary<long, string>, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<KeyValuePair<long, string>>, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<KeyValuePair<long, string>>, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IDictionary<long, string>, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyCollection<KeyValuePair<long, string>>, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IReadOnlyDictionary<long, string>, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ICollection, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IDictionary, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IDeserializationCallback, MockDictionaryLongString>(mock);
        exitCode += ValidationHelper.ValidateImplementation<ISerializable, MockDictionaryLongString>(mock);

        // Pre-override existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Add(123L, "3.14"), "invoke Add(key,value)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Remove(123L), "invoke Remove(key)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Clear(), "invoke Clear()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ContainsKey(123L), "invoke ContainsKey(key)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ContainsValue("3.14"), "invoke ContainsValue(value)");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Count, "get Count");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m[123L], "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m[123L] = "3.14", "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.Add = (key, value) => { };
        mock.MockOverrides.Remove_long = key => true;
        mock.MockOverrides.Clear = () => { };
        mock.MockOverrides.ContainsKey = key => true;
        mock.MockOverrides.ContainsValue = value => true;
        mock.MockOverrides.Count__get = () => 42;
        mock.MockOverrides.This__get = key => "999.999";
        mock.MockOverrides.This__set = (key, value) => { };

        exitCode += ValidationHelper.ValidateAction(() => mock.Add(1L, "1.0"), "call Add");
        exitCode += ValidationHelper.ValidateAction(() => mock.Clear(), "call Clear");
        exitCode += ValidationHelper.ValidateAction(() => mock[2L] = "2.0", "set indexer");
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Remove(1L), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.ContainsKey(3L), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.ContainsValue("2.0"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Count, 42);
        exitCode += ValidationHelper.ValidateCall(mock, m => m[123L], "999.999");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
