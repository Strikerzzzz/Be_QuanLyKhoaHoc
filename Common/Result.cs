using System.Text.Json.Serialization;

namespace SampleProject.Common
{
    public class Result<T>
    {
        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; }

        [JsonPropertyName("errors")]
        public IReadOnlyList<string> Errors { get; }

        [JsonPropertyName("data")]
        public T Data { get; }

        public Result(bool succeeded, IEnumerable<string> errors, T data)
        {
            Succeeded = succeeded;
            Errors = errors?.ToArray() ?? Array.Empty<string>();
            Data = data;
        }

        public static Result<T> Success(T data) => new Result<T>(true, Array.Empty<string>(), data);

        public static Result<T> Failure(IEnumerable<string> errors)
            => new Result<T>(false, errors ?? Array.Empty<string>(), default(T));

        public bool HasErrors() => Errors.Count > 0;

        public bool HasData() => Data != null;

        public string GetErrorMessage()
            => Errors.Count > 0 ? string.Join("; ", Errors) : "No errors found.";

        public bool IsSuccess() => Succeeded;

        public bool IsFailure() => !Succeeded;
    }
}
