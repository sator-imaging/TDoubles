using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface ITestInterface
{
    string this[int index] { get; set; }
    string this[string name] { get; }
    int this[double val] { set; }
}

[Mock(typeof(ITestInterface))]
public partial class MockITestInterface
{
}

public class Test34_MultipleIndexers
{
    static int Main()
    {
        int exitCode = 0;

        // Create mock and validate interface implementation
        var mock = new MockITestInterface();
        exitCode += ValidationHelper.ValidateImplementation<ITestInterface, MockITestInterface>(mock);

        // Validate default behavior for indexers (should throw until overridden)
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m[0], "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m["test"], "TDoubles.TDoublesException", "this");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m[0.0] = 0, "TDoubles.TDoublesException", "this");

        // Override and validate the first indexer (int)
        mock.MockOverrides.This_int__get = (index) => $"int_indexer_{index}";
        mock.MockOverrides.This_int__set = (index, value) => Console.WriteLine($"Set int_indexer_{index} to {value}");
        exitCode += ValidationHelper.ValidateCall(mock, m => m[1], "int_indexer_1");
        exitCode += ValidationHelper.ValidateAction(() => mock[2] = "new_value", "set int indexer"); // This will call the set accessor

        // Override and validate the second indexer (string)
        mock.MockOverrides.This_string__get = (name) => $"string_indexer_{name}";
        exitCode += ValidationHelper.ValidateCall(mock, m => m["key"], "string_indexer_key");

        // Override and validate the third indexer (double, bool)
        mock.MockOverrides.This_double__set = (val, set_value) => Console.WriteLine($"Set double_indexer_{val} to {set_value}");
        exitCode += ValidationHelper.ValidateAction(() => mock[3.0] = 100, "set double indexer"); // This will call the set accessor

        return exitCode;
    }
}