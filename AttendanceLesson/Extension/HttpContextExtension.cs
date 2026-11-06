namespace AttendanceLesson.Extension;

public static class HttpContextExtension
{
    public static void AppendCookiesInfo(this HttpContext context, string nameCookie, string data, DateTime expires, string path = "/")
    {
        context.Response.Cookies.Append(nameCookie, data , new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            Expires = expires.AddDays(7),
            SameSite = SameSiteMode.Strict,
            Path = path
        });
    }
}
