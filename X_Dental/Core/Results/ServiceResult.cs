namespace Dental.Core.Results
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }

        public List<string> Errors { get; set; } = new();

        public static ServiceResult<T> Ok(T data, string message = "")
        {
            return new ServiceResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ServiceResult<T> Fail(params string[] errors)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Errors = errors.ToList()
            };
        }
    }
}