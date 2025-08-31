using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public abstract class EmptyAbstractClass
{
}

[Mock(typeof(EmptyAbstractClass))]
public partial class MockEmptyAbstractClass
{
}

public class Test19_EmptyAbstractClass
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockEmptyAbstractClass();
        exitCode += ValidationHelper.ValidateImplementation<EmptyAbstractClass, MockEmptyAbstractClass>(mock);

        // Only object members existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
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
