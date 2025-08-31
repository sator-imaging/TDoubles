using System;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public record EmptyRecord
{
}

[Mock(typeof(EmptyRecord))]
public partial record MockEmptyRecord
{
}

public class Test25_EmptyRecord
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockEmptyRecord();
        exitCode += ValidationHelper.ValidateImplementation<EmptyRecord, MockEmptyRecord>(mock);
        exitCode += ValidationHelper.ValidateImplementation<IEquatable<EmptyRecord>, MockEmptyRecord>(mock);

        // Only object members existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        // exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        // mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        // exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
