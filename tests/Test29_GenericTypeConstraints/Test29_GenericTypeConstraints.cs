using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;
using System.Collections.Generic;

public abstract class GenericTypeConstraints<A, B, C, D, E, F, G, H, I, J, K, L, M, N>
    where A : struct
    where B : class
    where C : class?
    where D : notnull
    where E : unmanaged
    where F : new()
    where G : Foo
    where H : Foo?
    where I : IBar
    where J : IBar?
    where K : L?
    where M : N
    where N : Baz, IBar?, IQux?, IEnumerable<A>, new()
{
}

public class Foo { }
public interface IBar { }
public record Baz { }
public interface IQux { }

[Mock(typeof(GenericTypeConstraints<,,,,,,,,,,,,,>))]
public partial class MockGenericTypeConstraints<X1, X2, X3, X4, X5, X6, X7, X8, X9, Y1, Y2, Y3, Y4, Y5>
{
}

public class Test29_GenericTypeConstraints
{
    static int Main()
    {
        int exitCode = 0;

        var model = GeneratorValidationModel.Create();

        exitCode += ValidationHelper.GetGeneratedSource(model, "MockGenericTypeConstraints", out var source);
        exitCode += ValidationHelper.ValidateContains(source, "where X1 : struct");
        exitCode += ValidationHelper.ValidateContains(source, "where X2 : class");
        exitCode += ValidationHelper.ValidateContains(source, "where X3 : class?");
        exitCode += ValidationHelper.ValidateContains(source, "where X4 : notnull");
        exitCode += ValidationHelper.ValidateContains(source, "where X5 : unmanaged");
        exitCode += ValidationHelper.ValidateContains(source, "where X6 : new()");
        exitCode += ValidationHelper.ValidateContains(source, "where X7 : global::Foo");
        exitCode += ValidationHelper.ValidateContains(source, "where X8 : global::Foo?");
        exitCode += ValidationHelper.ValidateContains(source, "where X9 : global::IBar");
        exitCode += ValidationHelper.ValidateContains(source, "where Y1 : global::IBar?");
        exitCode += ValidationHelper.ValidateContains(source, "where Y2 : Y3?");
        exitCode += ValidationHelper.ValidateContains(source, "where Y4 : Y5");
        exitCode += ValidationHelper.ValidateContains(source, "where Y5 : global::Baz, global::IBar?, global::IQux?, global::System.Collections.Generic.IEnumerable<X1>, new()");

        return exitCode;
    }
}
