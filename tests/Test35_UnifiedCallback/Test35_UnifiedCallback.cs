using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;

public interface IUnifiedCallback
{
    // In generated mock, out parameter must be initialized before callback invocation
    string ComplexParameters(in string A, ref string B, out string C);
}

[Mock(typeof(IUnifiedCallback))]
public partial class MockUnifiedCallback
{
    public bool RaiseError;

    partial void OnWillMockCall(string memberName)
    {
        if (RaiseError)
        {
            throw new Exception($" 'STMG::{memberName}' ");
        }
    }
}

public class Test35_UnifiedCallback
{
    static int Main()
    {
        int exitCode = 0;

        // Validate interface implementation
        var mock = new MockUnifiedCallback();
        exitCode += ValidationHelper.ValidateImplementation<IUnifiedCallback, MockUnifiedCallback>(mock);

        // Pre-override existence checks
        string refString = "ref string";
        string outString = "out string";
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.ComplexParameters("", ref refString, out outString), "TDoubles.TDoublesException", "ComplexParameters");

        mock.MockOverrides.ComplexParameters = (in string X, ref string B, out string C) => { C = "overridden_complexParameters"; return C; };
        mock.MockOverrides.ToString = () => "overridden_toString";
        mock.MockOverrides.GetHashCode = () => 12345;
        mock.MockOverrides.Equals = obj => true;
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ComplexParameters("", ref refString, out outString), "overridden_complexParameters");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.ToString(), "overridden_toString");
        exitCode += ValidationHelper.ValidateMember(mock, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock, m => m.Equals(new object()), true);

        mock.RaiseError = true;
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.ComplexParameters("", ref refString, out outString), "System.Exception", "STMG::ComplexParameters");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.ToString(), "System.Exception", "STMG::ToString");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.GetHashCode(), "System.Exception", "STMG::GetHashCode");
        exitCode += ValidationHelper.ValidateThrows(mock, m => m.Equals(new object()), "System.Exception", "STMG::Equals");

        return exitCode;
    }
}
