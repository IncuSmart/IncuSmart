namespace IncuSmart.Core.Utils
{
    public static class PagingUtils
    {
        public static PagedResult<T> ToPagedResult<T>(IEnumerable<T> source, int page, int pageSize)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = pageSize < 1 ? 20 : pageSize;
            var cappedPageSize = normalizedPageSize > 100 ? 100 : normalizedPageSize;
            var list = source.ToList();
            var totalItems = list.Count;

            return new PagedResult<T>
            {
                Items = list
                    .Skip((normalizedPage - 1) * cappedPageSize)
                    .Take(cappedPageSize)
                    .ToList(),
                Page = normalizedPage,
                PageSize = cappedPageSize,
                TotalItems = totalItems,
                TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)cappedPageSize)
            };
        }
    }
}
