namespace IncuSmart.Core.Utils
{
    public static class EmailTemplates
    {
        public static string RegistrationOtp(string fullName, string otp) => $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
  <h2 style=""color: #2c6e49;"">Xác thực tài khoản IncuSmart</h2>
  <p>Xin chào <strong>{fullName}</strong>,</p>
  <p>Bạn vừa đăng ký tài khoản trên hệ thống IncuSmart. Vui lòng sử dụng mã OTP dưới đây để hoàn tất đăng ký:</p>
  <div style=""background: #f4f4f4; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;"">
    <span style=""font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #2c6e49;"">{otp}</span>
  </div>
  <p>Mã OTP có hiệu lực trong <strong>10 phút</strong>. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
  <p>Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.</p>
  <hr style=""border: none; border-top: 1px solid #eee;""/>
  <p style=""color: #888; font-size: 12px;"">IncuSmart – Hệ thống quản lý máy ấp trứng</p>
</div>";

        public static string GuestOrderOtp(string fullName, string orderCode, string otp) => $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
  <h2 style=""color: #2c6e49;"">Xác nhận đơn hàng IncuSmart</h2>
  <p>Xin chào <strong>{fullName}</strong>,</p>
  <p>Đơn hàng <strong>{orderCode}</strong> của bạn đã được tạo thành công.</p>
  <p>Dưới đây là mã xác thực để nhận đơn hàng khi sản phẩm được giao đến:</p>
  <div style=""background: #f4f4f4; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;"">
    <span style=""font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #2c6e49;"">{otp}</span>
  </div>
  <p>Vui lòng lưu giữ mã này để xác nhận khi nhận hàng. <strong>Không chia sẻ mã với người khác.</strong></p>
  <hr style=""border: none; border-top: 1px solid #eee;""/>
  <p style=""color: #888; font-size: 12px;"">IncuSmart – Hệ thống quản lý máy ấp trứng</p>
</div>";

        public static string SalesOrderOtp(string fullName, string orderCode, string otp) => $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
  <h2 style=""color: #2c6e49;"">Thông báo đơn hàng IncuSmart</h2>
  <p>Xin chào <strong>{fullName}</strong>,</p>
  <p>Nhân viên bán hàng vừa tạo đơn hàng <strong>{orderCode}</strong> cho bạn.</p>
  <p>Mã xác thực đơn hàng của bạn là:</p>
  <div style=""background: #f4f4f4; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;"">
    <span style=""font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #2c6e49;"">{otp}</span>
  </div>
  <p>Vui lòng lưu giữ mã này. <strong>Không chia sẻ mã với người khác.</strong></p>
  <hr style=""border: none; border-top: 1px solid #eee;""/>
  <p style=""color: #888; font-size: 12px;"">IncuSmart – Hệ thống quản lý máy ấp trứng</p>
</div>";
    }
}
