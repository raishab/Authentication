using ADL.AirportSampling.Utility.Enum;
using APIModels;
using APIModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface IUserServices
    {
        Task<ApiResponse<UserLoginResponceModel>> AuthenticateUser(UserLoginRequestModel model);
    }
}
