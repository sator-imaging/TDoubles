using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public interface IPublicInterface
{
    string PublicMethod();
}

internal interface IInternalInterface
{
    string InternalMethod();
}

[Mock(typeof(IPublicInterface))]
public partial class MockPublicInterface
{
}

[Mock(typeof(IInternalInterface))]
internal partial class MockInternalInterface
{
}

public class Test17_AccessibilityLevelHandling
{
    static int Main()
    {
        int exitCode = 0;

        var mockPublic = new MockPublicInterface();
        var mockInternal = new MockInternalInterface();

        exitCode += ValidationHelper.ValidateImplementation<IPublicInterface, MockPublicInterface>(mockPublic);
        exitCode += ValidationHelper.ValidateNotImplementation<IInternalInterface, MockInternalInterface>(mockInternal);

        exitCode += ValidationHelper.ValidateThrows(mockPublic, m => _ = m.PublicMethod(), "TDoubles.TDoublesException", "PublicMethod");
        exitCode += ValidationHelper.ValidateMemberExists(mockPublic, m => _ = m.ToString(), "invoke ToString() on public mock");
        exitCode += ValidationHelper.ValidateMemberExists(mockPublic, m => _ = m.GetHashCode(), "invoke GetHashCode() on public mock");
        exitCode += ValidationHelper.ValidateMemberExists(mockPublic, m => _ = m.Equals(new object()), "invoke Equals(object) on public mock");
        exitCode += ValidationHelper.ValidateMemberDoesNotExist(mockInternal, nameof(IInternalInterface.InternalMethod), "InternalMethod should not be mocked in MockInternalInterface");
        exitCode += ValidationHelper.ValidateMemberExists(mockInternal, m => _ = m.ToString(), "invoke ToString() on internal mock");
        exitCode += ValidationHelper.ValidateMemberExists(mockInternal, m => _ = m.GetHashCode(), "invoke GetHashCode() on internal mock");
        exitCode += ValidationHelper.ValidateMemberExists(mockInternal, m => _ = m.Equals(new object()), "invoke Equals(object) on internal mock");

        mockPublic.MockOverrides.PublicMethod = () => "overridden_public";
        // mockInternal.MockOverrides.InternalMethod = () => "overridden_internal";

        exitCode += ValidationHelper.ValidateCall(mockPublic, m => m.PublicMethod(), "overridden_public");
        // exitCode += ValidationHelper.ValidateCall(mockInternal, m => m.InternalMethod(), "overridden_internal");

        mockPublic.MockOverrides.ToString = () => "overridden_toString_public";
        mockPublic.MockOverrides.GetHashCode = () => 12345;
        mockPublic.MockOverrides.Equals = obj => true;
        mockInternal.MockOverrides.ToString = () => "overridden_toString_internal";
        mockInternal.MockOverrides.GetHashCode = () => 67890;
        mockInternal.MockOverrides.Equals = obj => false;

        exitCode += ValidationHelper.ValidateMember(mockPublic, m => m.ToString(), "overridden_toString_public");
        exitCode += ValidationHelper.ValidateMember(mockPublic, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mockPublic, m => m.Equals(new object()), true);
        exitCode += ValidationHelper.ValidateMember(mockInternal, m => m.ToString(), "overridden_toString_internal");
        exitCode += ValidationHelper.ValidateMember(mockInternal, m => m.GetHashCode(), 67890);
        exitCode += ValidationHelper.ValidateMember(mockInternal, m => m.Equals(new object()), false);

        return exitCode;
    }
}
