namespace IncuSmart.API.Requests
{
    public class PagingRequest
    {
        private int _page;
        private int _pageSize;

        public int Page
        {
            get => _page > 0 ? _page : 1;
            set => _page = value;
        }

        public int PageSize
        {
            get => _pageSize > 0 ? _pageSize : 10;
            set => _pageSize = value;
        }
    }
}
