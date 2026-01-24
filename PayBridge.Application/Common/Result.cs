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
        public string Error { get; }
        public string? ErrorCode { get; }

        private Result(bool isSuccess, T? data, string error, string? errorCode)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
            ErrorCode = errorCode;
        }

        public static Result<T> Success(T data)
        {
            return new(true, data, string.Empty, null);
        }
            
        public static Result<T> Failure(string error, string? errorCode = null)
        {
            if (error is null)
                throw new ArgumentNullException(nameof(error));

            return new(false, default, error, errorCode);
        }
    }
}
