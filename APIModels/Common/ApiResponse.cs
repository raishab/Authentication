using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace APIModels.Common
{
    public class ApiResponse<T>
    {
        public bool Status { get; set; }
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public void SetResponse(T data, string mess = ResponseMessage.Success, bool? status = true, int? statusCode = ResponseStatusCode.Success)
        {
            Status = status.Value;
            StatusCode = statusCode.Value;
            Message = mess;
            Data = data;
        }
    }
}
