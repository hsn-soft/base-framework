using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace HsnSoft.Base.Ddd.Domain.Tests;

public class RepositoryResultsTests
{
    private sealed class Thing : Entity<Guid>
    {
        public Thing()
        {
        }

        public Thing(Guid id)
            : base(id)
        {
        }
    }

    private sealed class Numbered(int id) : Entity<int>(id);

    private sealed class Named(string id) : Entity<string>(id);

    // ---- the exception ----

    [Fact]
    public void NotFound_Is404_WithItsKeyItsCodeAndItsRecords()
    {
        RepositoryException exception = RepositoryException.NotFound("Order", [7, "x", null, 7]);

        Assert.Equal((404, RepositoryFailure.NotFound, "Order"), (exception.HttpStatusCode, exception.Failure, exception.EntityName));
        Assert.Equal(("Repository:NotFound", "entity_not_found"), (exception.MessageKey, exception.ErrorCode));
        Assert.Equal(["Order"], exception.MessageArgs);
        Assert.Equal(["7", "x"], exception.Ids);   // each once, a null is no record
        Assert.Equal(("Order", "7, x"), (exception.Data["entity"], exception.Data["ids"]));
        Assert.Contains("Order was not found", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Conflict_Is409_WithItsKeyItsCodeAndItsRecords()
    {
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        RepositoryException exception = RepositoryException.Conflict("Order", [id]);

        Assert.Equal((409, RepositoryFailure.ConcurrencyConflict), (exception.HttpStatusCode, exception.Failure));
        Assert.Equal(("Concurrency:Conflict", "concurrency_conflict"), (exception.MessageKey, exception.ErrorCode));
        Assert.Equal(id.ToString(), Assert.Single(exception.Ids));
        Assert.Contains("changed or removed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_Is409_WithItsKeyItsCodeAndTheConstraint()
    {
        InvalidOperationException inner = new("the store said so");

        RepositoryException exception = RepositoryException.Duplicate("User", "ux_user_email", inner);

        Assert.Equal((409, RepositoryFailure.DuplicateKey, "User"), (exception.HttpStatusCode, exception.Failure, exception.EntityName));
        Assert.Equal(("Repository:Duplicate", "duplicate_key"), (exception.MessageKey, exception.ErrorCode));
        Assert.Equal("ux_user_email", exception.Constraint);
        Assert.Equal("ux_user_email", exception.Data["constraint"]);
        Assert.Same(inner, exception.InnerException);
        Assert.Empty(exception.Ids);
        Assert.Contains("unique value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_WithoutAConstraint_SaysOnlyTheEntity()
    {
        RepositoryException exception = RepositoryException.Duplicate("User", " ");

        Assert.Null(exception.Constraint);
        Assert.False(exception.Data.Contains("constraint"));
        Assert.Null(exception.InnerException);
        Assert.Throws<ArgumentException>(() => RepositoryException.Duplicate(" "));
    }

    [Fact]
    public void AFailureThatNamesNoRecord_HasNoIds_AndNoIdsData()
    {
        RepositoryException exception = RepositoryException.Conflict("Order");

        Assert.Empty(exception.Ids);
        Assert.False(exception.Data.Contains("ids"));
        Assert.Equal("Order", exception.Data["entity"]);
        Assert.Empty(RepositoryException.NotFound("Order", []).Ids);
    }

    [Fact]
    public void TheException_IsABaseHttpException_SoTheHostingLayersAnswerIt()
    {
        BaseHttpException exception = RepositoryException.NotFound("Order");

        Assert.IsAssignableFrom<ILocalizableError>(exception);
        Assert.True(KnownExceptions.IsKnown(exception));
    }

    [Fact]
    public void TheFactories_RejectABlankEntityName()
    {
        Assert.Throws<ArgumentException>(() => RepositoryException.NotFound(" "));
        Assert.Throws<ArgumentException>(() => RepositoryException.Conflict(""));
        Assert.Throws<ArgumentNullException>(() => RepositoryException.Conflict(null!));
    }

    // ---- what a write says ----

    [Fact]
    public void AWriteResult_SaysHowManyWereNotModified()
    {
        WriteResult result = new(5, 2);

        Assert.Equal((5, 2, 3), (result.Requested, result.Modified, result.NotModified));
        Assert.Equal(new WriteResult(5, 2), result);
        Assert.Equal(0, new WriteResult(0, 0).NotModified);
    }

    [Fact]
    public void TheOutcomes_AreNumbered_SoTheyAreStable()
    {
        Assert.Equal(1, (int)WriteOutcome.Modified);
        Assert.Equal(2, (int)WriteOutcome.NotModified);
        Assert.Equal(1, (int)RepositoryFailure.NotFound);
        Assert.Equal(2, (int)RepositoryFailure.ConcurrencyConflict);
        Assert.Equal(3, (int)RepositoryFailure.DuplicateKey);
    }

    // ---- the keys of an entity that has none ----

    [Fact]
    public void AnEmptyGuidKey_IsGiven_AVersion7Guid()
    {
        Thing thing = new();

        EntityKeys.EnsureKey(thing);

        Assert.NotEqual(Guid.Empty, thing.Id);
        Assert.Equal(7, thing.Id.Version);
    }

    [Fact]
    public void AKeyThatWasGiven_IsNeverChanged()
    {
        Guid chosen = Guid.NewGuid();
        Thing thing = new(chosen);

        EntityKeys.EnsureKey(thing);

        Assert.Equal(chosen, thing.Id);
    }

    [Fact]
    public void AKeyThatIsNotAGuid_IsLeftToTheDatabase()
    {
        Numbered number = new(0);
        Named name = new(string.Empty);

        EntityKeys.EnsureKey(number);
        EntityKeys.EnsureKey(name);

        Assert.Equal(0, number.Id);
        Assert.Equal(string.Empty, name.Id);
        Assert.Throws<ArgumentNullException>(() => EntityKeys.EnsureKey<Guid>(null!));
    }

    [Fact]
    public void TheKeys_AreOrderedByTheTimeTheyWereMade_ToTheMillisecond()
    {
        List<Thing> things = [.. Enumerable.Range(0, 20).Select(_ => new Thing())];
        things.ForEach(EntityKeys.EnsureKey);

        // the first 48 bits of a version 7 Guid are the time in milliseconds: they never go back
        List<string> times = [.. things.Select(thing => thing.Id.ToString()[..13])];
        Assert.Equal(times.Order(StringComparer.Ordinal), times);
    }

    [Fact]
    public void TheKeyAssignment_IsOnlyForAnEmptyKey_AndOnlyThroughTheInterface()
    {
        Thing thing = new();
        IKeyAssignable<Guid> assignable = thing;
        Guid first = Guid.NewGuid();

        Assert.True(assignable.AssignKeyIfEmpty(first));
        Assert.False(assignable.AssignKeyIfEmpty(Guid.NewGuid()));
        Assert.Equal(first, thing.Id);
    }
}
