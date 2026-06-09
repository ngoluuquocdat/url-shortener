 using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public ResultStatus Status { get; set; }
        public T? Value { get; set; }
        public string? Error { get; set; }

        public static Result<T> Success(T value)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Status = ResultStatus.Success,
                Value = value
            };
        }

        public static Result<T> ValidationFailed(string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Status = ResultStatus.ValidationFailed,
                Error = error
            };
        }

        public static Result<T> Conflict(string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Status = ResultStatus.Conflict,
                Error = error
            };
        }

        public static Result<T> NotFound(string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Status = ResultStatus.NotFound,
                Error = error
            };
        }

        public static Result<T> Gone(string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Status = ResultStatus.Gone,
                Error = error
            };
        }

        public static Result<T> UnexpectedFailure(string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Status = ResultStatus.UnexpectedFailure,
                Error = error
            };
        }
    }

    public enum ResultStatus
    {
        Success,
        Conflict,
        ValidationFailed,
        NotFound,
        Gone,
        UnexpectedFailure
    }
}
