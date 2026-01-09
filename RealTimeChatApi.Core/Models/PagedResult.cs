namespace RealTimeChatApi.Core.Models;

public class PagedResult<T>
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public IEnumerable<T> Items { get; set; } = new List<T>();

    public PagedResult(IEnumerable<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize;
        CurrentPage = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        HasPrevious = pageNumber > 1;
        HasNext = pageNumber < TotalPages;
        Items = items;
    }
}