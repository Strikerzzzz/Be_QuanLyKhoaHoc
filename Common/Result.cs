namespace SampleProject.Common
{
    public class Result<T>
    {
        public bool Succeeded { get; }
        public IReadOnlyList<string> Errors { get; }
        public T Data { get; }

        internal Result(bool succeeded, IEnumerable<string> errors, T data)
        {
            Succeeded = succeeded;
            Errors = errors?.ToArray() ?? Array.Empty<string>();
            Data = data;
        }

        public static Result<T> Success(T data) => new Result<T>(true, Array.Empty<string>(), data);
        public static Result<T> Failure(IEnumerable<string> errors) => new Result<T>(false, errors ?? Array.Empty<string>(), default);

        public bool HasErrors() => Errors.Count > 0;
        public string GetErrorMessage() => string.Join("; ", Errors);
    }
}
