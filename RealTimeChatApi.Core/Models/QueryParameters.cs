using RealTimeChatApi.Core.Constants;

namespace RealTimeChatApi.Core.Models;

public class QueryParameters
{
    private const int MaxPageSize = ApplicationConstants.MaxPageSize;
    private int _pageSize = ApplicationConstants.DefaultPageSize;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
}