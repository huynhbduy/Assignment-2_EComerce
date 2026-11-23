# Cổng đại lý B2B
Yêu cầu .NET 8 SDK, SQL Server (hoặc LocalDB trên Windows). Mở thư mục chứa AgentPortal.csproj trong VS Code.

1. Trong appsettings.json, đặt Wholesale trỏ đúng instance và database mà WinForms sử dụng.
2. Nếu đã chạy B2B_Wholesale.sql, dùng dữ liệu đang có. Với database mới hoặc rỗng, chạy `dotnet run -- --init-demo` một lần. EF Core sinh schema từ các entity và cấu hình Fluent API, sau đó nạp dữ liệu mẫu. Lệnh này chỉ khởi tạo, không nâng cấp schema cũ.
3. Trên Windows chạy `dotnet dev-certs https --trust`, rồi `dotnet run`. Mở https://localhost:7214.
4. Đăng nhập agent01 / Wholesale!Demo26. Chọn số lượng hai sản phẩm rồi gửi đơn. Đối chiếu mã đơn với màn hình Order Management trong WinForms. Đổi trạng thái ở WinForms, tải lại trang Đơn của tôi.
5. Kiểm tra admin và agent15 (bị khóa) với cùng mật khẩu. Cả hai phải bị từ chối. Đăng nhập agent02 để kiểm tra danh sách đơn riêng. Truy cập /Portal/History khi đã đăng xuất phải quay về đăng nhập.

Bootstrap 5.3.3 tải qua CDN. CSS cục bộ hỗ trợ bố cục cơ bản khi ngoại tuyến. Mật khẩu demo dùng cho dữ liệu bài tập.
