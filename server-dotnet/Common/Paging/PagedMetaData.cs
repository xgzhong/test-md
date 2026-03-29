namespace server_dotnet.Common.Paging;

public class PagedMetaData
{
    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int PageSize { get; set; }

    public long TotalCount { get; set; }

    public bool HasPrevious => CurrentPage > 1;

    public bool HasNext => CurrentPage < TotalPages;
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    public PagedMetaData MetaData { get; set; } = new();
}
