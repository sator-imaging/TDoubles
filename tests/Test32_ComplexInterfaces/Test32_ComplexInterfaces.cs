using System;
using System.Collections;
using System.Collections.Generic;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

[Mock(typeof(IDictionary<,>))] partial class MockIDictionary<A, B> { }
[Mock(typeof(IDictionary<int, string>))] partial class MockIDictionaryIntString { }
[Mock(typeof(IDictionary<string, long>))] partial class MockIDictionaryStringLong { }

[Mock(typeof(IList<>))] partial class MockIList<X> { }
[Mock(typeof(IList<int>))] partial class MockIListInt { }
[Mock(typeof(IList<string>))] partial class MockIListString { }

public class Test32_ComplexInterfaces
{
    public static int Main()
    {
        int exitCode = 0;

        // Test IDictionary<,>
        var mockIDictionary = new MockIDictionary<long, float>();
        exitCode += ValidationHelper.ValidateImplementation<IDictionary<long, float>, MockIDictionary<long, float>>(mockIDictionary);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<KeyValuePair<long, float>>, MockIDictionary<long, float>>(mockIDictionary);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<KeyValuePair<long, float>>, MockIDictionary<long, float>>(mockIDictionary);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockIDictionary<long, float>>(mockIDictionary);

        // Test IDictionary<int, string>
        var mockIDictionaryIntString = new MockIDictionaryIntString();
        exitCode += ValidationHelper.ValidateImplementation<IDictionary<int, string>, MockIDictionaryIntString>(mockIDictionaryIntString);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<KeyValuePair<int, string>>, MockIDictionaryIntString>(mockIDictionaryIntString);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<KeyValuePair<int, string>>, MockIDictionaryIntString>(mockIDictionaryIntString);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockIDictionaryIntString>(mockIDictionaryIntString);

        // Test IDictionary<string, long>
        var mockIDictionaryStringLong = new MockIDictionaryStringLong();
        exitCode += ValidationHelper.ValidateImplementation<IDictionary<string, long>, MockIDictionaryStringLong>(mockIDictionaryStringLong);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<KeyValuePair<string, long>>, MockIDictionaryStringLong>(mockIDictionaryStringLong);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<KeyValuePair<string, long>>, MockIDictionaryStringLong>(mockIDictionaryStringLong);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockIDictionaryStringLong>(mockIDictionaryStringLong);

        // Test IList<>
        var mockIList = new MockIList<float>();
        exitCode += ValidationHelper.ValidateImplementation<IList<float>, MockIList<float>>(mockIList);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<float>, MockIList<float>>(mockIList);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<float>, MockIList<float>>(mockIList);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockIList<float>>(mockIList);

        // Test IList<int>
        var mockIListInt = new MockIListInt();
        exitCode += ValidationHelper.ValidateImplementation<IList<int>, MockIListInt>(mockIListInt);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<int>, MockIListInt>(mockIListInt);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<int>, MockIListInt>(mockIListInt);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockIListInt>(mockIListInt);

        // Test IList<string>
        var mockIListString = new MockIListString();
        exitCode += ValidationHelper.ValidateImplementation<IList<string>, MockIListString>(mockIListString);
        exitCode += ValidationHelper.ValidateImplementation<ICollection<string>, MockIListString>(mockIListString);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable<string>, MockIListString>(mockIListString);
        exitCode += ValidationHelper.ValidateImplementation<IEnumerable, MockIListString>(mockIListString);

        return exitCode;
    }
}
