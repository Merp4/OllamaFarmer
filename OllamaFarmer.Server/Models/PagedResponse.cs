namespace OllamaFarmer.Server.Models
{
    /// <summary>
    /// Represents a paginated response containing data and metadata about pagination.
    /// </summary>
    /// <typeparam name="T">The type of the data contained in the response.</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>
        /// Gets or sets the current cursor position for pagination.
        /// </summary>
        public int Cursor { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the count of items after applying filters.
        /// </summary>
        public int FilteredCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total count of items before applying filters.
        /// </summary>
        public int TotalCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the data contained in the response.
        /// </summary>
        public T? Data { get; set; } = default;
    }

    public class PagedRequest<T>
    {
        /// <summary>
        /// Gets or sets the cursor position for pagination.
        /// </summary>
        public int Cursor { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 10;


        public T? Filter { get; set; } = default;

        public string? Search { get; set; } = null;
        public string? SortBy { get; set; } = null;
    }
}
