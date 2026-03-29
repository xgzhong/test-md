namespace server_dotnet.Common.Result;

public class Result<T> : IResult
{
    public Result() : this(default(T))
    {
    }

    protected internal Result(T? value)
    {
        Value = value;
    }

    protected internal Result(ResultStatus status)
    {
        Status = status;
    }

    public T? Value { get; init; }

    public bool IsSuccess => Status == ResultStatus.Ok;

    public IEnumerable<string>? Errors { get; protected set; }

    public ResultStatus Status { get; protected set; } = ResultStatus.Ok;

    public string? Message { get; set; }

    public object? GetValue()
    {
        return Value;
    }

    /// <summary> 隐式转换 </summary>
    public static implicit operator Result<T>(Result result)
    {
        return new Result<T>(default(T))
        {
            Status = result.Status,
            Errors = result.Errors,
            Message = result.Message
        };
    }
}

public class Result : Result<Result>
{
    public Result() : base(null)
    {
    }

    protected internal Result(Result value) : base(value)
    {
    }

    protected internal Result(ResultStatus status) : base(status)
    {
    }

    public static Result From(IResult result)
    {
        return new Result(result.Status)
        {
            Errors = result.Errors,
            Message = result.Message
        };
    }

    public static Result Success()
    {
        return new Result(ResultStatus.Ok);
    }

    public static Result Success(string message)
    {
        return new Result(ResultStatus.Ok) { Message = message };
    }

    public static Result<T> Success<T>(T value, string? message = null)
    {
        return new Result<T>(value) { Message = message };
    }

    public static Result Failure()
    {
        return new Result(ResultStatus.Error);
    }

    public static Result Failure(string error)
    {
        return new Result(ResultStatus.Error)
        {
            Errors = new[] { error }
        };
    }

    public static Result Failure(params string[] errors)
    {
        return new Result(ResultStatus.Error)
        {
            Errors = errors.AsEnumerable()
        };
    }

    public static Result NotFound()
    {
        return new Result(ResultStatus.NotFound);
    }

    public static Result NotFound(string error)
    {
        return new Result(ResultStatus.NotFound)
        {
            Errors = new[] { error }
        };
    }

    public static Result NotFound(params string[] errors)
    {
        return new Result(ResultStatus.NotFound)
        {
            Errors = errors.AsEnumerable()
        };
    }

    public static Result Forbidden()
    {
        return new Result(ResultStatus.Forbidden);
    }

    public static Result Forbidden(string message)
    {
        return new Result(ResultStatus.Forbidden) { Message = message };
    }

    public static Result Unauthorized()
    {
        return new Result(ResultStatus.Unauthorized);
    }

    public static Result Unauthorized(string message)
    {
        return new Result(ResultStatus.Unauthorized) { Message = message };
    }

    public static Result Invalid()
    {
        return new Result(ResultStatus.Invalid);
    }

    public static Result Invalid(string error)
    {
        return new Result(ResultStatus.Invalid)
        {
            Errors = new[] { error }
        };
    }

    public static Result Invalid(params string[] errors)
    {
        return new Result(ResultStatus.Invalid)
        {
            Errors = errors.AsEnumerable()
        };
    }
}