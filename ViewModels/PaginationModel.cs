namespace QuanLyBanHang.ViewModels;

public class PaginationModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public string? Action { get; set; }
    public string? Controller { get; set; }
    public object? RouteValues { get; set; }
}

public static class PaginationExtensions
{
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize, out int total)
    {
        total = query.Count();
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
