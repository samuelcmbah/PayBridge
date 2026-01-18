using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string? Error { get; }
        public string? ErrorCode { get; }

        private Result(bool isSuccess, T? data, string? error, string? errorCode)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
            ErrorCode = errorCode;
        }

        public static Result<T> Success(T data) => new(true, data, null, null);

        public static Result<T> Failure(string error, string? errorCode = null) =>
            new(false, default, error, errorCode);
    }
}
