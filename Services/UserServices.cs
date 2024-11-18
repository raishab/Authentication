using APIModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using APIModels;
using ADL.AirportSampling.Utility.Enum;
using Utility;
using QueryService;
using DBHelper;
using ADL.AirportSampling.DBHelper;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Claims;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using Extension;
using System.Reflection;

namespace Services
{
    public class UserServices : IUserServices
    {
        private readonly JwtConfiguration _jwtConfiguration;
        private readonly IQueryServices _commonQuery;
        private readonly AuthTokenOptions _tokenOptions;

        public UserServices(IOptions<JwtConfiguration> jwtConfiguration, IQueryServices commonQuery, AuthTokenOptions tokenOptions)
        {
            _jwtConfiguration = jwtConfiguration.Value;
            _commonQuery = commonQuery;
            _tokenOptions = tokenOptions;
        }

        public async Task<ApiResponse<UserLoginResponceModel>> AuthenticateUser(UserLoginRequestModel model)
        {
            var dataTable = await _commonQuery.ExecuteQuery(model, StoredProcedure.ValidateUsers);

            var dbUser = dataTable.DataTableToObject<UserLoginResponceModel>();

            if(dbUser.OtpErrorMessage == null)
            {
                if(dbUser != null)
                {
                    dbUser.AuthTokenValue = CreateToken(dbUser, 1, RoleType.Admin);

                }

                return ResponseHelper.GetResponse(ref dataTable, dbUser,
                      ResponseMessage.Login_Successfully,
                      !string.IsNullOrEmpty(dbUser.OtpErrorMessage) ? dbUser.OtpErrorMessage : ResponseMessage.Login_Successfully,
                      false, ResponseStatusCode.Unauthorized);
            }
            else
            {
                return ResponseHelper.GetResponse(ref dataTable, dbUser,
                    ResponseMessage.Invalid_Credentials,
                    !string.IsNullOrEmpty(dbUser.OtpErrorMessage) ? dbUser.OtpErrorMessage : ResponseMessage.Login_Successfully,
                    false, ResponseStatusCode.Unauthorized);
            }
        }
        private string CreateToken(UserLoginResponceModel user, int audience, RoleType roleType)
        {
            var handler = new JwtSecurityTokenHandler();
            var securityToken = CreateSecurityToken(handler, user, audience, roleType);

            return handler.WriteToken(securityToken);
        }

        private SecurityToken CreateSecurityToken(JwtSecurityTokenHandler handler,
            UserLoginResponceModel user, int audience, RoleType roleType)
        {
            var identity = CreateClaimsIdentity(user, roleType);
            return handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = identity,
                Issuer = _tokenOptions.Issuer,
                Audience = audience.ToString(),
                SigningCredentials = _tokenOptions.SigningCredentials,
                Expires = DateTime.UtcNow.AddHours(1)
            });
        }

        private ClaimsIdentity CreateClaimsIdentity(UserLoginResponceModel model, RoleType roleType)
        {
            var claims = new List<Claim>(3)
            {
                new Claim(Utility.ClaimTypes.Id, Convert.ToString(model.Id)),
                new Claim(ClaimTypes.Role, roleType.ToString()),
                new Claim(Utility.ClaimTypes.Id, Convert.ToString(model.Id)),
            };
            return new ClaimsIdentity(claims);
        }

    }
}