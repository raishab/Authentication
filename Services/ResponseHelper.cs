using APIModels.Common;
using Microsoft.VisualBasic;
using System.Data;
using Utility;

namespace Services
{
    public static class ResponseHelper
    {
        public static ApiResponse<T> GetResponse<T>(ref DataTable dataTable, T data,
            string successMes = ResponseMessage.Success, string errorMes = ResponseMessage.Error,
            bool? status = false, int? statusCode = ResponseStatusCode.Success)
        {
            ApiResponse<T> response = new ApiResponse<T>();

            if(dataTable != null && dataTable.Rows.Count > 0)
            {
                if(dataTable.Columns[DBConstants.ErrorMessage] != null)
                {
                    response.SetResponse(default, ResponseMessage.Something_Went_Wrong, false,
                        ResponseStatusCode.InternalServerError);
                }
                else
                {
                    response.SetResponse(data, successMes, true, statusCode);
                }
                dataTable.Dispose();
            }
            else
            {
                response.SetResponse(default, errorMes, status, statusCode);
            }

            dataTable = null;
            return response;
        }

        public static ApiResponse<T> GetResponse<T>(ref DataSet dataSet, T data,
            string successMes = ResponseMessage.Success, string errorMes = ResponseMessage.Error,
            bool? status = false, int? statusCode = ResponseStatusCode.Success)
        {
            ApiResponse<T> response = new ApiResponse<T>();

            if(dataSet != null && dataSet.Tables[0].Rows.Count > 0)
            {
                if(dataSet.Tables[0].Columns[DBConstants.ErrorMessage] != null)
                {
                    response.SetResponse(default, ResponseMessage.Something_Went_Wrong, false,
                        ResponseStatusCode.InternalServerError);
                }
                else
                {
                    response.SetResponse(data, successMes);
                }
                dataSet.Dispose();
            }
            else
            {
                response.SetResponse(default, errorMes, status, statusCode);
            }

            dataSet = null;
            return response;
        }
        public static ApiResponse<T> GetResponse<T>(ref string msg, T data,
           string successMes = ResponseMessage.Success, string errorMes = ResponseMessage.Error,
           bool? status = false, int? statusCode = ResponseStatusCode.Success)
        {
            ApiResponse<T> response = new ApiResponse<T>();

            if(msg != null)
            {

                response.SetResponse(data, successMes);
            }
            else
            {
                response.SetResponse(default, errorMes, status, statusCode);
            }

            msg = null;
            return response;
        }
        public static ApiResponse<bool> GetResponse(bool data,
            string successMes = ResponseMessage.Success, string errorMes = ResponseMessage.Error)
        {
            ApiResponse<bool> response = new ApiResponse<bool>();

            if(data)
            {
                response.SetResponse(data, successMes);
            }
            else
            {
                response.SetResponse(false, errorMes, false);
            }
            return response;
        }

        public static PaginationApiResponse<T> GetPaginationResponse<T>(ref DataTable dataTable, T data,
            string successMes = ResponseMessage.Success, string errorMes = ResponseMessage.Error,
            bool? status = false)
        {
            PaginationApiResponse<T> response = new PaginationApiResponse<T>();

            if(dataTable.Rows.Count > 0)
            {
                if(dataTable.Columns[DBConstants.ErrorMessage] != null)
                {
                    response.SetResponse(default, ResponseMessage.Something_Went_Wrong, false,
                        ResponseStatusCode.InternalServerError);
                }
                else
                {
                    response.SetResponse(data, successMes);
                    response.Count = (int?)dataTable.Rows[0][DBConstants.TotalCount];
                }
                dataTable.Dispose();
            }
            else
            {
                response.SetResponse(data, errorMes, status);
            }

            dataTable = null;
            return response;
        }

    }
}
