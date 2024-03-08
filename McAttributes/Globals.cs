using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace McAttributes {
    public static class Globals {
        public const string AUTH_SCHEMES = CookieAuthenticationDefaults.AuthenticationScheme + "," + OpenIdConnectDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme;
    }
}
