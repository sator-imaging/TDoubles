using TDoubles;
using TDoubles.Tests.ComprehensiveValidation;
using System;
using System.Collections.Generic;

// Content of 30_GenericMethodConstraints.cs
public abstract class GenericMethodConstraints
{
    public abstract A Struct_Abstract<A>() where A : struct;
    public abstract B Class_Abstract<B>() where B : class;
    public abstract C ClassNullable_Abstract<C>() where C : class?;
    public abstract D NotNull_Abstract<D>() where D : notnull;
    public abstract E Unmanaged_Abstract<E>() where E : unmanaged;
    public abstract F New_Abstract<F>() where F : new();
    public abstract G Foo_Abstract<G>() where G : Foo;
    public abstract H FooNullable_Abstract<H>() where H : Foo?;
    public abstract I IBar_Abstract<I>() where I : IBar;
    public abstract J IBarNullable_Abstract<J>() where J : IBar?;
    public abstract (K t, L u) TypeArgMapping_Abstract<K, L>() where K : L;
    // public abstract (M t, N? u) TypeArgMappingNullable_Abstract<M, N>() where M : N?;
    public abstract O Complex_Abstract<O>() where O : Baz, IBar?, IQux?, IEnumerable<O>, new();

    public AA Struct<AA>() where AA : struct { return default!; }
    public BB Class<BB>() where BB : class { return default!; }
    public CC ClassNullable<CC>() where CC : class? { return default!; }
    public DD NotNull<DD>() where DD : notnull { return default!; }
    public EE Unmanaged<EE>() where EE : unmanaged { return default!; }
    public FF New<FF>() where FF : new() { return default!; }
    public GG Foo<GG>() where GG : Foo { return default!; }
    public HH FooNullable<HH>() where HH : Foo? { return default!; }
    public II IBar<II>() where II : IBar { return default!; }
    public JJ IBarNullable<JJ>() where JJ : IBar? { return default!; }
    public (KK t, LL u) TypeArgMapping<KK, LL>() where KK : LL { return default!; }
    public (MM t, NN? u) TypeArgMappingNullable<MM, NN>() where MM : NN? { return default!; }
    public OO Complex<OO>() where OO : Baz, IBar?, IQux?, IEnumerable<OO>, new() { return default!; }
}

public class Foo { }
public interface IBar { }
public record Baz { }
public interface IQux { }

[Mock(typeof(GenericMethodConstraints))]
public partial class MockGenericMethodConstraints
{
}

public class Test30_GenericMethodConstraints
{
    static int Main()
    {
        int exitCode = 0;

        // Create mock and validate abstract class implementation
        var mock = new MockGenericMethodConstraints();
        exitCode += ValidationHelper.ValidateImplementation<GenericMethodConstraints, MockGenericMethodConstraints>(mock);

        var model = GeneratorValidationModel.Create();
        exitCode += ValidationHelper.GetGeneratedSource(model, "MockGenericMethodConstraints", out var source);

        // abstract methods
        exitCode += ValidationHelper.ValidateContains(source, "    where A : struct");
        exitCode += ValidationHelper.ValidateContains(source, "    where B : class");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where C : class?");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where D : notnull");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where E : unmanaged");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where F : new()");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where G : global::Foo");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where H : global::Foo?");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where I : global::IBar");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where J : global::IBar?");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where K : L");
        // exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where M : N?");
        exitCode += ValidationHelper.ValidateDoesNotContain(source, "    where O : global::Baz, global::IBar?, global::IQux?, global::System.Collections.Generic.IEnumerable<O>, new()");

        // usual methods
        exitCode += ValidationHelper.ValidateContains(source, "    where AA : struct");
        exitCode += ValidationHelper.ValidateContains(source, "    where BB : class");
        exitCode += ValidationHelper.ValidateContains(source, "    where CC : class?");
        exitCode += ValidationHelper.ValidateContains(source, "    where DD : notnull");
        exitCode += ValidationHelper.ValidateContains(source, "    where EE : unmanaged");
        exitCode += ValidationHelper.ValidateContains(source, "    where FF : new()");
        exitCode += ValidationHelper.ValidateContains(source, "    where GG : global::Foo");
        exitCode += ValidationHelper.ValidateContains(source, "    where HH : global::Foo?");
        exitCode += ValidationHelper.ValidateContains(source, "    where II : global::IBar");
        exitCode += ValidationHelper.ValidateContains(source, "    where JJ : global::IBar?");
        exitCode += ValidationHelper.ValidateContains(source, "    where KK : LL");
        // exitCode += ValidationHelper.ValidateContains(source, "    where MM : NN?");
        exitCode += ValidationHelper.ValidateContains(source, "    where OO : global::Baz, global::IBar?, global::IQux?, global::System.Collections.Generic.IEnumerable<OO>, new()");

        return exitCode;
    }
}
