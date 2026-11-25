namespace IndustrialBackendDemo.Application.Common.Results;

public sealed class ResultTests
{
    private static readonly ApplicationError TestError = new("Test.Error", "This is a test error.");

    [Fact]
    public void SuccessShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ApplicationError.None, result.Error);
    }

    [Fact]
    public void FailureShouldCreateFailedResult()
    {
        var result = Result.Failure(TestError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void FailureShouldRejectNullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void FailureShouldRejectNoneError()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(ApplicationError.None));
    }

    [Fact]
    public void GenericSuccessShouldContainValue()
    {
        const string expectedValue = "test";

        var result = Result<string>.Success(expectedValue);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(ApplicationError.None, result.Error);
    }

    [Fact]
    public void GenericSuccessShouldRejectNullValue()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void GenericFailureShouldContainError()
    {
        var result = Result<string>.Failure(TestError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void GenericFailureShouldNotAllowValueAccess()
    {
        var result = Result<string>.Failure(TestError);

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void GenericFailureShouldRejectNullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Fact]
    public void GenericFailureShouldRejectNoneError()
    {
        Assert.Throws<ArgumentException>(() => Result<string>.Failure(ApplicationError.None));
    }
}
