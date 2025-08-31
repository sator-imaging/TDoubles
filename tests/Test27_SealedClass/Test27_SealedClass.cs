using System;
using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

public sealed class SealedClass : IDisposable
{
    public void Dispose() { }
}

[Mock(typeof(SealedClass))]
public partial class MockSealedClass
{
}

public class Test27_SealedClass
{
    static int Main()
    {
        int exitCode = 0;

        var mock = new MockSealedClass();
        exitCode += ValidationHelper.ValidateImplementation<IDisposable, MockSealedClass>(mock);

        // Pre-override existence checks
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => m.Dispose(), "invoke Dispose()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.ToString(), "invoke ToString()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.GetHashCode(), "invoke GetHashCode()");
        exitCode += ValidationHelper.ValidateMemberExists(mock, m => _ = m.Equals(new object()), "invoke Equals(object)");

        // Apply overrides and validate
        mock.MockOverrides.Dispose = () => { };
        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;

        exitCode += ValidationHelper.ValidateAction(() => mock.Dispose(), "call Dispose");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        return exitCode;
    }
}
