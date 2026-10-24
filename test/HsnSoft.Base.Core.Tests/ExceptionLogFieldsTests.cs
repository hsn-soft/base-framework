using System.Runtime.CompilerServices;
using HsnSoft.Base.Logging;

namespace HsnSoft.Base.Core.Tests;

public class ExceptionLogFieldsTests
{
    private static int CurrentLine([CallerLineNumber] int line = 0) => line;

    private static (Exception Exception, int Line) Thrown(Exception exception)
    {
        int line = CurrentLine() + 3;
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return (caught, line);
        }
    }

    [Fact]
    public void Identifier_SaysWhereTheExceptionWasThrown_AsSolutionAssemblyFileAndLine()
    {
        (Exception exception, int line) = Thrown(new InvalidOperationException("boom"));

        string identifier = ExceptionLogFields.Identifier(exception, "HsnSoft");

        Assert.Equal($"Solution:HsnSoft|Assembly:HsnSoft.Base.Core.Tests|File:ExceptionLogFieldsTests.cs|Line:{line}", identifier);
    }

    [Fact]
    public void Identifier_LooksAtTheCauseFirst_NotAtTheWrapper()
    {
        (Exception cause, int causeLine) = Thrown(new KeyNotFoundException("missing"));
        (Exception wrapper, _) = Thrown(new InvalidOperationException("wrapped", cause));

        Assert.EndsWith($"|File:ExceptionLogFieldsTests.cs|Line:{causeLine}", ExceptionLogFields.Identifier(wrapper, "HsnSoft"), StringComparison.Ordinal);
    }

    // the library (xunit) throws, from sources it carries in its symbols, and the application called it
    private static void FailInsideALibrary() => Assert.Equal(1, 2);

    [Fact]
    public void Identifier_PrefersTheCodeOfTheApplication_ToALibraryThatHasItsSourcesInItsSymbols()
    {
        // xunit's assertions carry their source files: a failure thrown through them has frames with files in two solutions
        Exception exception = Assert.ThrowsAny<Exception>(FailInsideALibrary);

        string ours = ExceptionLogFields.Identifier(exception, "HsnSoft");
        string library = ExceptionLogFields.Identifier(exception, "xunit");

        Assert.Contains("|Assembly:HsnSoft.Base.Core.Tests|File:ExceptionLogFieldsTests.cs|", ours, StringComparison.Ordinal);
        Assert.Contains("|Assembly:xunit.assert|", library, StringComparison.Ordinal);
        // when nothing of the wanted solution has sources, any code with sources is the answer
        Assert.Equal(library, ExceptionLogFields.Identifier(exception, "nobody"));
    }

    [Fact]
    public void Identifier_TellsTheMethod_WhenNoFrameHasASourceFile_AndTheTypeWhenItWasNeverThrown()
    {
        // code made at run time, run on a pool thread, has no source file anywhere in its stack
        Action throwing = System.Linq.Expressions.Expression.Lambda<Action>(System.Linq.Expressions.Expression.Throw(System.Linq.Expressions.Expression.Constant(new InvalidOperationException("dynamic")))).Compile();
        Task task = Task.Run(throwing);
        Exception dynamic = Assert.Throws<AggregateException>(() => task.Wait()).InnerException!;

        string fromDynamic = ExceptionLogFields.Identifier(dynamic);
        string neverThrown = ExceptionLogFields.Identifier(new InvalidOperationException("not thrown"));

        Assert.Contains("|Method:", fromDynamic, StringComparison.Ordinal);
        Assert.DoesNotContain("|File:", fromDynamic, StringComparison.Ordinal);
        Assert.Equal("Solution:System|Assembly:System.Private.CoreLib|Type:System.InvalidOperationException", neverThrown);
    }

    [Fact]
    public void Reason_IsEveryMessageDownToTheInnermost_JoinedWithTheSeparator()
    {
        Exception exception = new InvalidOperationException("outer", new ArgumentException("middle", new KeyNotFoundException("inner")));

        Assert.Equal("outer >> middle >> inner", ExceptionLogFields.ReasonOf(exception));
        Assert.Equal("alone", ExceptionLogFields.ReasonOf(new InvalidOperationException("alone")));
    }

    [Fact]
    public void Reason_IsCutAt8K_AndTheCutIsMarked_ButTheIdentifierIsNotCut()
    {
        Exception exception = new InvalidOperationException(new string('x', 6000), new InvalidOperationException(new string('y', 6000)));

        string reason = ExceptionLogFields.ReasonOf(exception);

        Assert.StartsWith(new string('x', 6000) + " >> yyyy", reason, StringComparison.Ordinal);
        Assert.EndsWith("...(+3812)", reason, StringComparison.Ordinal);
        Assert.Equal(ExceptionLogFields.MaxReasonLength + "...(+3812)".Length, reason.Length);
    }

    [Fact]
    public void AddTo_PutsTheTypeTheIdentifierAndTheReasonInTheFields()
    {
        (Exception exception, int line) = Thrown(new ArgumentException("bad"));
        Dictionary<string, object?> fields = [];

        ExceptionLogFields.AddTo(fields, exception);

        Assert.Equal("System.ArgumentException", fields["ExceptionType"]);
        Assert.EndsWith($"|Line:{line}", (string)fields["ExceptionIdentifier"]!, StringComparison.Ordinal);
        Assert.Equal("bad", fields["Reason"]);
        Assert.Throws<ArgumentNullException>(() => ExceptionLogFields.AddTo(null!, exception));
        Assert.Throws<ArgumentNullException>(() => ExceptionLogFields.AddTo(fields, null!));
        Assert.Throws<ArgumentNullException>(() => ExceptionLogFields.Identifier(null!));
        Assert.Throws<ArgumentNullException>(() => ExceptionLogFields.ReasonOf(null!));
    }
}
