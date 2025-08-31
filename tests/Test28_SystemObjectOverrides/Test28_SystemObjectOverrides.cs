using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public class SystemObjectOverrides
{
    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
}

[Mock(typeof(SystemObjectOverrides))]
public partial class MockSystemObjectOverrides { }

public class Test28_SystemObjectOverrides
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockSystemObjectOverrides();
        exitCode += ValidationHelper.ValidateImplementation<SystemObjectOverrides, MockSystemObjectOverrides>(mock);

        // Only object members existence checks (overridden ToString doesn't return nullable string so that it throws)
        exitCode += ValidationHelper.ValidateThrows(mock, m => _ = m.ToString(), "TDoubles.TDoublesException", "ToString");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
