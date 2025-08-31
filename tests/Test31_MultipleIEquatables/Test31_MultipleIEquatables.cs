using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;
using System.Collections.Generic;

// Content of 31_MultipleIEquatables.cs
public class MultipleIEquatables : IEquatable<int>, IEquatable<float>, IEquatable<char>, IEquatable<string>
{
    public bool Equals(int other) => true;
    public bool Equals(float other) => true;

    bool IEquatable<char>.Equals(char other) => true;
    bool IEquatable<string>.Equals(string other) => true;
}

[Mock(typeof(MultipleIEquatables))]
partial class MockMultipleIEquatables
{
}

public class Test31_MultipleIEquatables
{
    static int Main()
    {
        int exitCode = 0;

        // Create mock and validate interface implementation
        var mock = new MockMultipleIEquatables();
        exitCode += ValidationHelper.ValidateImplementation<MultipleIEquatables, MockMultipleIEquatables>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEquatable<int>, MockMultipleIEquatables>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEquatable<float>, MockMultipleIEquatables>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEquatable<char>, MockMultipleIEquatables>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEquatable<string>, MockMultipleIEquatables>(mock);

        // Validate that Equals methods can be overridden
        mock.MockOverrides.Equals_int = (val) => val == 10;
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Equals(10), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Equals(5), false);

        mock.MockOverrides.Equals_float = (val) => val == 10.5f;
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Equals(10.5f), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => m.Equals(5.0f), false);

        // Explicit interface implementations
        mock.MockOverrides.IEquatableT1_Equals_char = (val) => val == 'a';
        exitCode += ValidationHelper.ValidateCall(mock, m => ((IEquatable<char>)m).Equals('a'), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => ((IEquatable<char>)m).Equals('b'), false);

        mock.MockOverrides.IEquatableT1_Equals_string = (val) => val == "test";
        exitCode += ValidationHelper.ValidateCall(mock, m => ((IEquatable<string>)m).Equals("test"), true);
        exitCode += ValidationHelper.ValidateCall(mock, m => ((IEquatable<string>)m).Equals("other"), false);

        return exitCode;
    }
}
