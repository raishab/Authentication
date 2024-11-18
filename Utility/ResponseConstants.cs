using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public struct ResponseStatusCode
    {
        public const int Success = 200;
        public const int InternalServerError = 500;
        public const int NotFound = 400;
        public const int Unauthorized = 401;
        public const int SuccessCreate = 201;
    }
    
    public struct ResponseMessage
    {
        public const string Success = "Success";
        public const string Error = "Error";
        public const string BadRequest = "Bad Request";
        public const string Invalid_Credentials = "Invalid credentials. Please try again.";
        public const string Login_Successfully = "Login Successfully";
        public const string Save = "Save successfully.";
        public const string Something_Went_Wrong = "Something Went Wrong";
        public const string No_Record_Found = "No records found.";
    }
}
