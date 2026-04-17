namespace ArandanoIRT.Web._1_Application.DTOs.Common;

/// <summary>
/// Representa un resultado paginado de una consulta, incluyendo la lista de items y la información de paginación.
/// </summary>
/// <typeparam name="T">El tipo de los elementos en la lista.</typeparam>
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}