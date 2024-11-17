using ADL.AirportSampling.Utility.Enum;
using APIModels;
using APIModels.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace AnandTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public readonly IUserServices _userServices;
        public UserController(IUserServices userServices) { 
            _userServices = userServices;
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ApiResponse<UserLoginResponceModel>> Login([FromBody]UserLoginRequestModel model)
        {
            return await _userServices.AuthenticateUser(model);
        }
    }
}
