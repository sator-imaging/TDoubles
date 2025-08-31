using System;
using System.Collections.Generic;
using System.Reflection;
using TDoubles.Tests.ComprehensiveValidation;

namespace Tests.ValidationHelperTest;

public static class Program
{
    public static void Main()
    {
        RunAll();
        Console.WriteLine("ValidationHelperTest: All checks passed.");
    }

    private static void RunAll()
    {
        TestValidateImplementation_Succeeds();
        TestValidateImplementation_Fails();

        TestValidateCall_Succeeds();
        TestValidateCall_Fails();
        TestValidateCall_NullDelegate_Throws();

        TestValidateProperty_Succeeds();
        TestValidateProperty_Fails();
        TestValidateProperty_NullGetter_Throws();
        TestValidateProperty_NullSetter_Throws();

        TestValidateIndexer_Succeeds();
        TestValidateIndexer_Fails();
        TestValidateIndexer_NullGetter_Throws();
        TestValidateIndexer_NullSetter_Throws();

        TestValidateMember_Succeeds();
        TestValidateMember_Fails();
        TestValidateMember_NullDelegate_Throws();

        TestValidateAction_Succeeds();
        TestValidateAction_Fails();

        TestValidateNotImplementation_Succeeds();
        TestValidateNotImplementation_Fails();

        TestValidateDoesNotImplementInterface_Succeeds();
        TestValidateDoesNotImplementInterface_Fails();

        TestValidateMemberExists_Action_Succeeds();
        TestValidateMemberExists_Action_Fails_NullAction();
        TestValidateMemberExists_TModel_Action_Succeeds();
        TestValidateMemberExists_TModel_Action_Fails_NullAction();
        TestValidateMemberExists_Reflection_Succeeds();
        TestValidateMemberExists_Reflection_Fails_MemberNotFound();
        TestValidateMemberExists_Reflection_Fails_NullInstance();

        TestValidateMemberDoesNotExist_Succeeds();
        TestValidateMemberDoesNotExist_Fails();
        TestValidateMemberDoesNotExist_Fails_NullInstance();

        TestValidateThrows_Succeeds();
        TestValidateThrows_Fails_NoThrow();
        TestValidateThrows_Fails_WrongType();
        TestValidateThrows_Fails_WrongMessage();
        TestValidateThrows_Fails_NullAction();

        TestValidateNotThrows_Succeeds();
        TestValidateNotThrows_Fails();
        TestValidateNotThrows_Fails_NullAction();

        TestValidateNoInheritanceOrImplementation_Succeeds();
        TestValidateNoInheritanceOrImplementation_Fails_Inherits();
        TestValidateNoInheritanceOrImplementation_Fails_ImplementsInterface();

        TestValidateContains_Succeeds();
        TestValidateContains_Fails();

        TestValidateDoesNotContain_Succeeds();
        TestValidateDoesNotContain_Fails();
    }

    // --- Implementation validation ---
    private static void TestValidateImplementation_Succeeds()
    {
        IContract instance = new GoodImpl();
        ExpectSuccess(() => ValidationHelper.ValidateImplementation<IContract, GoodImpl>((GoodImpl)instance), nameof(TestValidateImplementation_Succeeds));
    }

    private static void TestValidateImplementation_Fails()
    {
        var bad = new BadImpl();
        ExpectFailure(() =>
            ValidationHelper.ValidateImplementation<IContract, BadImpl>(bad)
        , nameof(TestValidateImplementation_Fails));
    }

    // --- ValidateCall ---
    private static void TestValidateCall_Succeeds()
    {
        var model = new Model { Name = "alpha", Counter = 5 };
        ExpectSuccess(() => ValidationHelper.ValidateCall(model, m => m.Counter + 1, 6), nameof(TestValidateCall_Succeeds));
    }

    private static void TestValidateCall_Fails()
    {
        var model = new Model { Name = "alpha", Counter = 5 };
        ExpectFailure(() =>
            ValidationHelper.ValidateCall(model, m => m.Counter + 1, 7)
        , nameof(TestValidateCall_Fails));
    }

    private static void TestValidateCall_NullDelegate_Throws()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateCall<Model, int>(model, null!, 0)
        , nameof(TestValidateCall_NullDelegate_Throws));
    }

    // --- ValidateProperty ---
    private static void TestValidateProperty_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateProperty(
            model,
            m => m.Counter,
            (m, v) => m.Counter = v,
            setValue: 42,
            expectedAfter: 42
        ), nameof(TestValidateProperty_Succeeds));
    }

    private static void TestValidateProperty_Fails()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateProperty(
                model,
                m => m.Counter,
                (m, v) => m.Counter = v,
                setValue: 10,
                expectedAfter: 11
            )
        , nameof(TestValidateProperty_Fails));
    }

    private static void TestValidateProperty_NullGetter_Throws()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateProperty<int, int>(
                model: 0, // placeholder, type param TModel is int for this null test
                getter: null!,
                setter: (_, __) => { },
                setValue: 0,
                expectedAfter: 0
            )
        , nameof(TestValidateProperty_NullGetter_Throws));
    }

    private static void TestValidateProperty_NullSetter_Throws()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateProperty(
                model,
                getter: m => m.Counter,
                setter: null!,
                setValue: 0,
                expectedAfter: 0
            )
        , nameof(TestValidateProperty_NullSetter_Throws));
    }

    // --- ValidateIndexer ---
    private static void TestValidateIndexer_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateIndexer(
            model,
            (m, i) => m[i],
            (m, i, v) => m[i] = v,
            index: 3,
            setValue: "val",
            expectedAfter: "val"
        ), nameof(TestValidateIndexer_Succeeds));
    }

    private static void TestValidateIndexer_Fails()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateIndexer(
                model,
                (m, i) => m[i],
                (m, i, v) => m[i] = v,
                index: 1,
                setValue: "x",
                expectedAfter: "y"
            )
        , nameof(TestValidateIndexer_Fails));
    }

    private static void TestValidateIndexer_NullGetter_Throws()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateIndexer(
                model,
                getter: null!,
                setter: (m, i, v) => m[i] = v,
                index: 0,
                setValue: "a",
                expectedAfter: "a"
            )
        , nameof(TestValidateIndexer_NullGetter_Throws));
    }

    private static void TestValidateIndexer_NullSetter_Throws()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateIndexer(
                model,
                getter: (m, i) => m[i],
                setter: null!,
                index: 0,
                setValue: "a",
                expectedAfter: "a"
            )
        , nameof(TestValidateIndexer_NullSetter_Throws));
    }

    // --- ValidateMember ---
    private static void TestValidateMember_Succeeds()
    {
        var model = new Model { Name = "x", Counter = 2 };
        ExpectSuccess(() => ValidationHelper.ValidateMember(model, m => m.ToString(), "Model:x:2"), nameof(TestValidateMember_Succeeds));
    }

    private static void TestValidateMember_Fails()
    {
        var model = new Model { Name = "x", Counter = 2 };
        ExpectFailure(() =>
            ValidationHelper.ValidateMember(model, m => m.Counter, 3)
        , nameof(TestValidateMember_Fails));
    }

    private static void TestValidateMember_NullDelegate_Throws()
    {
        var model = new Model();
        ExpectFailure(() =>
            ValidationHelper.ValidateMember<Model, int>(model, null!, 0)
        , nameof(TestValidateMember_NullDelegate_Throws));
    }

    // --- ValidateAction ---
    private static void TestValidateAction_Succeeds()
    {
        ExpectSuccess(() => ValidationHelper.ValidateAction(() => { /* This action should not throw */ }, nameof(TestValidateAction_Succeeds)), nameof(TestValidateAction_Succeeds));
    }

    private static void TestValidateAction_Fails()
    {
        ExpectFailure(() =>
            ValidationHelper.ValidateAction(() => { throw new InvalidOperationException("Test exception"); }, nameof(TestValidateAction_Fails))
        , nameof(TestValidateAction_Fails));
    }

    // --- ValidateNotImplementation ---
    private static void TestValidateNotImplementation_Succeeds()
    {
        var bad = new BadImpl();
        ExpectSuccess(() => ValidationHelper.ValidateNotImplementation<IContract, BadImpl>(bad), nameof(TestValidateNotImplementation_Succeeds));
    }

    private static void TestValidateNotImplementation_Fails()
    {
        IContract instance = new GoodImpl();
        ExpectFailure(() => ValidationHelper.ValidateNotImplementation<IContract, GoodImpl>((GoodImpl)instance), nameof(TestValidateNotImplementation_Fails));
    }

    // --- ValidateDoesNotImplementInterface ---
    private static void TestValidateDoesNotImplementInterface_Succeeds()
    {
        var bad = new BadImpl();
        ExpectSuccess(() => ValidationHelper.ValidateDoesNotImplementInterface<IContract, BadImpl>(bad), nameof(TestValidateDoesNotImplementInterface_Succeeds));
    }

    private static void TestValidateDoesNotImplementInterface_Fails()
    {
        IContract instance = new GoodImpl();
        ExpectFailure(() => ValidationHelper.ValidateDoesNotImplementInterface<IContract, GoodImpl>((GoodImpl)instance), nameof(TestValidateDoesNotImplementInterface_Fails));
    }

    // --- ValidateMemberExists ---
    private static void TestValidateMemberExists_Action_Succeeds()
    {
        ExpectSuccess(() => ValidationHelper.ValidateMemberExists(() => { /* do nothing */ }, nameof(TestValidateMemberExists_Action_Succeeds)), nameof(TestValidateMemberExists_Action_Succeeds));
    }

    private static void TestValidateMemberExists_Action_Fails_NullAction()
    {
        ExpectFailure(() => ValidationHelper.ValidateMemberExists(null!, nameof(TestValidateMemberExists_Action_Fails_NullAction)), nameof(TestValidateMemberExists_Action_Fails_NullAction));
    }

    private static void TestValidateMemberExists_TModel_Action_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateMemberExists(model, m => { /* do nothing */ }, nameof(TestValidateMemberExists_TModel_Action_Succeeds)), nameof(TestValidateMemberExists_TModel_Action_Succeeds));
    }

    private static void TestValidateMemberExists_TModel_Action_Fails_NullAction()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateMemberExists(model, null!, nameof(TestValidateMemberExists_TModel_Action_Fails_NullAction)), nameof(TestValidateMemberExists_TModel_Action_Fails_NullAction));
    }

    private static void TestValidateMemberExists_Reflection_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateMemberExists(model, "Counter", nameof(TestValidateMemberExists_Reflection_Succeeds), BindingFlags.Public | BindingFlags.Instance), nameof(TestValidateMemberExists_Reflection_Succeeds));
    }

    private static void TestValidateMemberExists_Reflection_Fails_MemberNotFound()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateMemberExists(model, "NonExistentMember", nameof(TestValidateMemberExists_Reflection_Fails_MemberNotFound), BindingFlags.Public | BindingFlags.Instance), nameof(TestValidateMemberExists_Reflection_Fails_MemberNotFound));
    }

    private static void TestValidateMemberExists_Reflection_Fails_NullInstance()
    {
        ExpectFailure(() => ValidationHelper.ValidateMemberExists(null!, "Counter", nameof(TestValidateMemberExists_Reflection_Fails_NullInstance), BindingFlags.Public | BindingFlags.Instance), nameof(TestValidateMemberExists_Reflection_Fails_NullInstance));
    }

    // --- ValidateMemberDoesNotExist ---
    private static void TestValidateMemberDoesNotExist_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateMemberDoesNotExist(model, "NonExistentMember", nameof(TestValidateMemberDoesNotExist_Succeeds), BindingFlags.Public | BindingFlags.Instance), nameof(TestValidateMemberDoesNotExist_Succeeds));
    }

    private static void TestValidateMemberDoesNotExist_Fails()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateMemberDoesNotExist(model, "Counter", nameof(TestValidateMemberDoesNotExist_Fails), BindingFlags.Public | BindingFlags.Instance), nameof(TestValidateMemberDoesNotExist_Fails));
    }

    private static void TestValidateMemberDoesNotExist_Fails_NullInstance()
    {
        ExpectFailure(() => ValidationHelper.ValidateMemberDoesNotExist(null!, "Counter", nameof(TestValidateMemberDoesNotExist_Fails_NullInstance), BindingFlags.Public | BindingFlags.Instance), nameof(TestValidateMemberDoesNotExist_Fails_NullInstance));
    }

    // --- ValidateThrows ---
    private static void TestValidateThrows_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateThrows(model, m => { throw new InvalidOperationException("Test"); }, typeof(InvalidOperationException).FullName!), nameof(TestValidateThrows_Succeeds));
    }

    private static void TestValidateThrows_Fails_NoThrow()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateThrows(model, m => { /* do nothing */ }, typeof(InvalidOperationException).FullName!), nameof(TestValidateThrows_Fails_NoThrow));
    }

    private static void TestValidateThrows_Fails_WrongType()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateThrows(model, m => { throw new ArgumentException("Test"); }, typeof(InvalidOperationException).FullName!), nameof(TestValidateThrows_Fails_WrongType));
    }

    private static void TestValidateThrows_Fails_WrongMessage()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateThrows(model, m => { throw new InvalidOperationException("Wrong message"); }, typeof(InvalidOperationException).FullName!, "Expected message"), nameof(TestValidateThrows_Fails_WrongMessage));
    }

    private static void TestValidateThrows_Fails_NullAction()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateThrows(model, null!, typeof(InvalidOperationException).FullName!), nameof(TestValidateThrows_Fails_NullAction));
    }

    // --- ValidateNotThrows ---
    private static void TestValidateNotThrows_Succeeds()
    {
        var model = new Model();
        ExpectSuccess(() => ValidationHelper.ValidateNotThrows(model, m => { /* do nothing */ }), nameof(TestValidateNotThrows_Succeeds));
    }

    private static void TestValidateNotThrows_Fails()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateNotThrows(model, m => { throw new InvalidOperationException("Test"); }), nameof(TestValidateNotThrows_Fails));
    }

    private static void TestValidateNotThrows_Fails_NullAction()
    {
        var model = new Model();
        ExpectFailure(() => ValidationHelper.ValidateNotThrows(model, null!), nameof(TestValidateNotThrows_Fails_NullAction));
    }

    // --- ValidateNoInheritanceOrImplementation ---
    private static void TestValidateNoInheritanceOrImplementation_Succeeds()
    {
        ExpectSuccess(() => ValidationHelper.ValidateNoInheritanceOrImplementation<BadImpl>(), nameof(TestValidateNoInheritanceOrImplementation_Succeeds));
    }

    private static void TestValidateNoInheritanceOrImplementation_Fails_Inherits()
    {
        ExpectFailure(() => ValidationHelper.ValidateNoInheritanceOrImplementation<DerivedClass>(), nameof(TestValidateNoInheritanceOrImplementation_Fails_Inherits));
    }

    private static void TestValidateNoInheritanceOrImplementation_Fails_ImplementsInterface()
    {
        ExpectFailure(() => ValidationHelper.ValidateNoInheritanceOrImplementation<GoodImpl>(), nameof(TestValidateNoInheritanceOrImplementation_Fails_ImplementsInterface));
    }

    // --- ValidateContains ---
    private static void TestValidateContains_Succeeds()
    {
        ExpectSuccess(() => ValidationHelper.ValidateContains("hello world", "world"), nameof(TestValidateContains_Succeeds));
    }

    private static void TestValidateContains_Fails()
    {
        ExpectFailure(() => ValidationHelper.ValidateContains("hello world", "foo"), nameof(TestValidateContains_Fails));
    }

    // --- ValidateDoesNotContain ---
    private static void TestValidateDoesNotContain_Succeeds()
    {
        ExpectSuccess(() => ValidationHelper.ValidateDoesNotContain("hello world", "foo"), nameof(TestValidateDoesNotContain_Succeeds));
    }

    private static void TestValidateDoesNotContain_Fails()
    {
        ExpectFailure(() => ValidationHelper.ValidateDoesNotContain("hello world", "world"), nameof(TestValidateDoesNotContain_Fails));
    }

    private static void ExpectSuccess(Func<int> action, string name)
    {
        if (action() != 0)
        {
            throw new Exception($"{name}: Expected success (0), but action returned non-zero.");
        }
    }

    private static void ExpectFailure(Func<int> action, string name)
    {
        if (action() == 0)
        {
            throw new Exception($"{name}: Expected failure (non-zero), but action returned 0.");
        }
    }

    private static void ExpectException<TException>(Action action, string name) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new Exception($"{name}: Expected {typeof(TException).Name}, got {ex.GetType().Name}.");
        }

        throw new Exception($"{name}: Expected {typeof(TException).Name}, but no exception was thrown.");
    }

    public interface IContract { }
    public sealed class GoodImpl : IContract { }
    public sealed class BadImpl { }

    public sealed class Model
    {
        public string Name { get; set; } = string.Empty;
        public int Counter { get; set; }

        private readonly Dictionary<int, string> _map = new();

        public string this[int index]
        {
            get => _map.TryGetValue(index, out var v) ? v : string.Empty;
            set => _map[index] = value;
        }

        public override string ToString() => $"Model:{Name}:{Counter}";
    }

    public class BaseClass { }
    public class DerivedClass : BaseClass { }
}