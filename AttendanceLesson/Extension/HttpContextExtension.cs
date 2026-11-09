namespace AttendanceLesson.Extension;

public static class HttpContextExtension
{
    public static void AppendCookiesInfo(this HttpContext context, string nameCookie, string data, DateTime expires, string path = "/")
    {
        context.Response.Cookies.Append(nameCookie, data , new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            Expires = expires,
            SameSite = SameSiteMode.Strict,
            Path = path
        });
    }
}
