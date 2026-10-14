namespace EAP.Models.Common;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public long TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
