using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;

namespace TestNamespace1
{
    public interface INamespaceInterface1
    {
        string Method1();
    }
}

namespace TestNamespace2
{
    namespace Nested
    {
        public interface INamespaceInterface2
        {
            string Method2();
        }
    }
}

[Mock(typeof(TestNamespace1.INamespaceInterface1))]
public partial class MockNamespaceInterface1
{
}

[Mock(typeof(TestNamespace2.Nested.INamespaceInterface2))]
public partial class MockNamespaceInterface2
{
}

public class Test16_NamespaceHandling
{
    static int Main()
    {
        int exitCode = 0;

        var mock1 = new MockNamespaceInterface1();
        var mock2 = new MockNamespaceInterface2();
        
        exitCode += ValidationHelper.ValidateImplementation<TestNamespace1.INamespaceInterface1, MockNamespaceInterface1>(mock1);
        exitCode += ValidationHelper.ValidateImplementation<TestNamespace2.Nested.INamespaceInterface2, MockNamespaceInterface2>(mock2);

        exitCode += ValidationHelper.ValidateThrows(mock1, m => _ = m.Method1(), "TDoubles.TDoublesException", "Method1");
        exitCode += ValidationHelper.ValidateThrows(mock2, m => _ = m.Method2(), "TDoubles.TDoublesException", "Method2");
        exitCode += ValidationHelper.ValidateMemberExists(mock1, m => _ = m.ToString(), "invoke ToString() on mock1");
        exitCode += ValidationHelper.ValidateMemberExists(mock1, m => _ = m.GetHashCode(), "invoke GetHashCode() on mock1");
        exitCode += ValidationHelper.ValidateMemberExists(mock1, m => _ = m.Equals(new object()), "invoke Equals(object) on mock1");
        exitCode += ValidationHelper.ValidateMemberExists(mock2, m => _ = m.ToString(), "invoke ToString() on mock2");
        exitCode += ValidationHelper.ValidateMemberExists(mock2, m => _ = m.GetHashCode(), "invoke GetHashCode() on mock2");
        exitCode += ValidationHelper.ValidateMemberExists(mock2, m => _ = m.Equals(new object()), "invoke Equals(object) on mock2");

        mock1.MockOverrides.Method1 = () => "overridden1";
        mock2.MockOverrides.Method2 = () => "overridden2";

        exitCode += ValidationHelper.ValidateCall(mock1, m => m.Method1(), "overridden1");
        exitCode += ValidationHelper.ValidateCall(mock2, m => m.Method2(), "overridden2");

        mock1.MockOverrides.ToString = () => "overridden_toString1";
        mock1.MockOverrides.GetHashCode = () => 12345;
        mock1.MockOverrides.Equals = obj => true;
        mock2.MockOverrides.ToString = () => "overridden_toString2";
        mock2.MockOverrides.GetHashCode = () => 67890;
        mock2.MockOverrides.Equals = obj => false;

        exitCode += ValidationHelper.ValidateMember(mock1, m => m.ToString(), "overridden_toString1");
        exitCode += ValidationHelper.ValidateMember(mock1, m => m.GetHashCode(), 12345);
        exitCode += ValidationHelper.ValidateMember(mock1, m => m.Equals(new object()), true);
        exitCode += ValidationHelper.ValidateMember(mock2, m => m.ToString(), "overridden_toString2");
        exitCode += ValidationHelper.ValidateMember(mock2, m => m.GetHashCode(), 67890);
        exitCode += ValidationHelper.ValidateMember(mock2, m => m.Equals(new object()), false);

        return exitCode;
    }
}
