namespace server_dotnet.Common.Paging;

public class Pagination
{
    private const int MaxPageSize = 100;

    public int CurrentPage { get; set; } = 1;

    private int _pageSize = 15;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

}

