using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable IDE0079
#nullable enable
#pragma warning disable CA1822
#pragma warning disable IDE1006

namespace TDoubles.Sandbox
{
    class NoVirtualMethod()
    {
        public void Foo() { }
        public void Bar() { }
    }
    record NoVirtualMethodRecord
    {
        public void Foo() { }
        public void Bar() { }
    }
    record Record() { }
    record struct RecordStruct() { }

    // error and warning location tests
    [Mock(typeof(IList))] class _ErrorTestNoPartial { }
    [Mock(typeof(IList<>))] partial class _ErrorTestNoTypeArg { }
    [Mock(typeof(IList<>))] partial class _ErrorTestNoTypeArgCount<A, B> { }
    [Mock(typeof(_ErrorCircular))] partial class _ErrorCircular { }
    [Mock(typeof(StringSplitOptions))] partial class _ErrorUnsupportedType { }
    [Mock(typeof(Record))] partial class _ErrorRecord { }
    [Mock(typeof(RecordStruct))] partial class _ErrorRecordStructAsClass { }
    [Mock(typeof(RecordStruct))] partial record _ErrorRecordStructAsRecord { }
    [Mock(typeof(NoVirtualMethod))] partial class MockNoVirtualMethod { }
    [Mock(typeof(NoVirtualMethodRecord))] partial record MockNoVirtualMethodRecord { }

    [Mock(typeof(Dictionary<,>))] internal partial class MockDictionary<A, B> { }
    [Mock(typeof(IDictionary<,>))] internal partial class MockIDictionary<A, B> { }
    [Mock(typeof(List<>))] internal partial class MockList<A> { }
    [Mock(typeof(List<int>))] internal partial class MockListInt { }
    [Mock(typeof(IList<>))] internal partial class MockIList<A> { }
    [Mock(typeof(IList<int>))] internal partial class MockIListInt { }


    [Mock(typeof(MockUnifiedCallback), "", "", "", IncludeInternals = true)]
    partial class MockAttributeTest { }

    public interface IUnifiedCallback
    {
        (string x, int y) ComplexParameters(in string A, ref string B, out string C);
    }

    [Mock(typeof(IUnifiedCallback), "ToString_XXX")]
    public partial class MockUnifiedCallback
    {
        public override string? ToString() => string.Empty;

        partial void OnWillMockCall(string memberName)
        {
            throw new NotImplementedException();
        }
        partial void OnWillMockCall(string memberName, object?[] args)
        {
            throw new NotImplementedException();
        }
    }


    public record struct EmptyRecordStruct { }

    [Mock(typeof(EmptyRecordStruct))]
    readonly partial record struct MockEmptyRecordStruct
    {
    }


    interface IService
    {
        int GetUserId();
        A GetValue<A>();
        void DoSomething();
    }
    interface IService<T>
    {
        int GetUserId();
        T GetValue();
        void DoSomething();
    }

    [Mock(typeof(IService))] partial class MockIService { }
    [Mock(typeof(IService<>))] partial class MockIService<A> { }
    [Mock(typeof(IService<int>))] partial class MockIServiceInt { }
}
