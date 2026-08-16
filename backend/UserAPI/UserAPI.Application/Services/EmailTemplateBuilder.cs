using System.Net;

namespace UserAPI.Application.Services;

public static class EmailTemplateBuilder
{
    public static string PasswordReset(string username, string resetLink)
    {
        var safeName = WebUtility.HtmlEncode(username);
        var safeLink = WebUtility.HtmlEncode(resetLink);

        return $$"""
        <!doctype html>
        <html lang="vi">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f7f4f7;font-family:Arial,'Helvetica Neue',sans-serif;color:#29242d">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f7f4f7;padding:36px 16px">
            <tr><td align="center">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:18px;overflow:hidden;box-shadow:0 10px 35px rgba(76,37,61,.10)">
                <tr><td style="height:8px;background:linear-gradient(90deg,#f04f9a,#bb72e6)"></td></tr>
                <tr><td align="center" style="padding:32px 36px 10px">
                  <div style="font-size:34px;font-weight:800;color:#ef4d98;letter-spacing:-2px">Comi<span style="color:#b86cde">Co</span></div>
                  <div style="margin-top:5px;color:#9b8f99;font-size:12px;letter-spacing:1.5px">THẾ GIỚI TRUYỆN TRANH CỦA BẠN</div>
                </td></tr>
                <tr><td style="padding:16px 42px 34px;text-align:center">
                  <div style="width:68px;height:68px;line-height:68px;margin:0 auto 20px;border-radius:50%;background:#fff0f7;color:#e5488f;font-size:30px">🔒</div>
                  <h1 style="margin:0 0 14px;font-size:25px;line-height:1.35;color:#2c2730">Đặt lại mật khẩu</h1>
                  <p style="margin:0 0 12px;font-size:15px;line-height:1.7;color:#625b65">Xin chào <strong>{{safeName}}</strong>,</p>
                  <p style="margin:0 auto 26px;max-width:470px;font-size:14px;line-height:1.7;color:#766e78">Comico đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nhấn nút bên dưới để tạo mật khẩu mới.</p>
                  <a href="{{safeLink}}" style="display:inline-block;padding:14px 32px;border-radius:9px;background:#e94e94;color:#fff;text-decoration:none;font-size:14px;font-weight:700;box-shadow:0 8px 18px rgba(233,78,148,.25)">Đặt lại mật khẩu</a>
                  <div style="margin:25px auto 0;padding:13px 16px;max-width:450px;border-radius:8px;background:#fff8e7;color:#82651e;font-size:12px;line-height:1.6">⏱ Liên kết chỉ có hiệu lực trong <strong>15 phút</strong> và chỉ sử dụng được một lần.</div>
                  <p style="margin:22px 0 7px;font-size:11px;line-height:1.6;color:#aaa2ac">Nếu nút không hoạt động, sao chép liên kết sau vào trình duyệt:</p>
                  <p style="margin:0 auto;max-width:500px;word-break:break-all;font-size:11px;line-height:1.5;color:#d54285">{{safeLink}}</p>
                </td></tr>
                <tr><td style="padding:20px 36px;background:#fcfafc;border-top:1px solid #f0e9ef;text-align:center;color:#a39ba5;font-size:11px;line-height:1.7">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.<br>Mật khẩu hiện tại của bạn vẫn được giữ nguyên và an toàn.</td></tr>
              </table>
              <p style="margin:18px 0 0;color:#aaa1aa;font-size:10px">© {{DateTime.UtcNow.Year}} Comico. All rights reserved.</p>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }

    public static string Verification(string username, string verificationLink)
    {
        var safeName = WebUtility.HtmlEncode(username);
        var safeLink = WebUtility.HtmlEncode(verificationLink);

        return $$"""
        <!doctype html>
        <html lang="vi">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f7f4f7;font-family:Arial,'Helvetica Neue',sans-serif;color:#29242d">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f7f4f7;padding:36px 16px">
            <tr><td align="center">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:18px;overflow:hidden;box-shadow:0 10px 35px rgba(76,37,61,.10)">
                <tr><td style="height:8px;background:linear-gradient(90deg,#f04f9a,#bb72e6)"></td></tr>
                <tr><td align="center" style="padding:34px 36px 12px">
                  <div style="font-size:34px;font-weight:800;color:#ef4d98;letter-spacing:-2px">Comi<span style="color:#b86cde">Co</span></div>
                  <div style="margin-top:5px;color:#9b8f99;font-size:12px;letter-spacing:1.5px">THẾ GIỚI TRUYỆN TRANH CỦA BẠN</div>
                </td></tr>
                <tr><td style="padding:16px 42px 34px;text-align:center">
                  <div style="width:66px;height:66px;line-height:66px;margin:0 auto 20px;border-radius:50%;background:#fff0f7;color:#e5488f;font-size:30px">✉</div>
                  <h1 style="margin:0 0 14px;font-size:24px;line-height:1.35;color:#2c2730">Xác nhận địa chỉ email</h1>
                  <p style="margin:0 0 12px;font-size:15px;line-height:1.7;color:#625b65">Xin chào <strong>{{safeName}}</strong>,</p>
                  <p style="margin:0 auto 26px;max-width:470px;font-size:14px;line-height:1.7;color:#766e78">Cảm ơn bạn đã gia nhập Comico. Hãy xác nhận email để kích hoạt tài khoản và bắt đầu khám phá kho truyện tranh của chúng tôi.</p>
                  <a href="{{safeLink}}" style="display:inline-block;padding:14px 30px;border-radius:9px;background:#e94e94;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700;box-shadow:0 8px 18px rgba(233,78,148,.25)">Xác nhận email</a>
                  <p style="margin:25px 0 8px;font-size:12px;color:#9a929c">Liên kết có hiệu lực trong 15 phút.</p>
                  <p style="margin:0;font-size:11px;line-height:1.6;color:#aaa2ac">Nếu nút không hoạt động, sao chép liên kết sau vào trình duyệt:</p>
                  <p style="margin:6px auto 0;max-width:500px;word-break:break-all;font-size:11px;line-height:1.5;color:#d54285">{{safeLink}}</p>
                </td></tr>
                <tr><td style="padding:20px 36px;background:#fcfafc;border-top:1px solid #f0e9ef;text-align:center;color:#a39ba5;font-size:11px;line-height:1.6">Bạn nhận email này vì vừa đăng ký tài khoản Comico.<br>Nếu không phải bạn, hãy bỏ qua email này.</td></tr>
              </table>
              <p style="margin:18px 0 0;color:#aaa1aa;font-size:10px">© {{DateTime.UtcNow.Year}} Comico. All rights reserved.</p>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
