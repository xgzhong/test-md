namespace server_dotnet.Common.Result;

public interface IResult
{
    /// <summary>
    /// 错误信息列表
    /// </summary>
    IEnumerable<string>? Errors { get; }

    /// <summary>
    /// 是否成功
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// 状态码
    /// </summary>
    ResultStatus Status { get; }

    /// <summary>
    /// 提示消息
    /// </summary>
    string? Message { get; }

    /// <summary>
    /// 获取返回值
    /// </summary>
    object? GetValue();
}


public enum ResultStatus
{
    /// <summary> 成功 </summary>
    Ok = 0,

    /// <summary> 错误 </summary>
    Error = 1,

    /// <summary> 拒绝访问 </summary>
    Forbidden = 2,

    /// <summary> 未认证 </summary>
    Unauthorized = 3,

    /// <summary> 未找到 </summary>
    NotFound = 4,

    /// <summary> 操作无效 </summary>
    Invalid = 5
}