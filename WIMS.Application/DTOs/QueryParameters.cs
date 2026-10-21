namespace WIMS.Application.DTOs;
public class QueryParameters
{
    private int _pageSize = 10;
    private const int _maxPageSize = 100;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > _maxPageSize ? _maxPageSize : value < 1 ? 1 : value;
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
    public Dictionary<string, string> Filters { get; set; } = new();
}