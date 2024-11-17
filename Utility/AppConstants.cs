namespace Utility
{
    public struct JwtSecurityOptions
    {
        public const string PublicKey = "JwtSecurityOptions:Asymmetric:PublicKey";

        public const string PrivateKey = "JwtSecurityOptions:Asymmetric:PrivateKey";

        public const string Audiences = "JwtSecurityOptions:Audiences";

        public const string Issuer = "JwtSecurityOptions:Issuer";
    }
    public struct ClaimTypes
    {
        public const string Name = "Name";
        public const string AUTH_TOKEN = "AuthToken";
        public const string Password = "Password";
        public const string Id = "Id";
        public const string Email = "Email";
        public const string Audience = "aud";
    }
    public struct AuthorizationPolicies
    {
        public const string Admin_Authenticated = "Admin_Authenticated";
        public const string Admin_RoleType = "Admin_RoleType";
        public const string Viewer_RoleType = "Viewer_RoleType";
        public const string Auth_Authenticated = "Auth_Authenticated";
    }
}
