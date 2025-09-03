using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public interface IEmptyInterface
{
}

[Mock(typeof(IEmptyInterface))]
public partial class MockIEmptyInterface : IEmptyInterface
{
}

public class Test18_EmptyInterface
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockIEmptyInterface();
        exitCode += ValidationHelper.ValidateImplementation<IEmptyInterface, MockIEmptyInterface>(mock);

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
