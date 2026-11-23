HƯỚNG DẪN CHẠY B2B WHOLESALE TRÊN WINDOWS

1. Chuẩn bị
Giải nén toàn bộ ZIP. Trong VS Code, chọn File > Open Folder và mở thư mục B2BWholesale chứa B2BWholesale.sln. Các lệnh dưới đây chạy trong Terminal PowerShell tại thư mục này.
Cần .NET 8 SDK, SQL Server Express LocalDB và .NET Framework 4.8 Developer Pack (để build WinForms). Cần Internet cho lần đầu khôi phục các gói NuGet.
Kiểm tra:
  dotnet --list-sdks
  sqllocaldb info
Danh sách SDK nên có 8.0.x. Nếu chưa có instance MSSQLLocalDB sau khi cài LocalDB, chạy:
  sqllocaldb create MSSQLLocalDB
  sqllocaldb start MSSQLLocalDB

2. Khởi tạo database một lần
Nếu đã có B2BWholesale từ bài trước, dùng lại database và bỏ qua bước này.
Với database mới, chọn MỘT cách:
  A. Trong Terminal:
     dotnet run --project .\AgentPortal\AgentPortal.csproj -- --init-demo
  B. Trong SQL Server Management Studio, kết nối (localdb)\MSSQLLocalDB bằng Windows Authentication. Mở Database\B2B_Wholesale.sql rồi nhấn F5 chạy toàn bộ.
Dữ liệu mẫu gồm 15 mặt hàng, 15 đại lý, 15 đơn, 30 dòng chi tiết và 16 tài khoản.
Web đọc AgentPortal\appsettings.json. WinForms đọc BackOffice\App.config. Cả hai dùng khóa Wholesale. Nếu dùng SQL Server instance khác, chỉnh cả hai file tới cùng server và database. Trong JSON viết dấu gạch chéo ngược hai lần, trong XML viết một lần. Chạy hai ứng dụng bằng cùng tài khoản Windows khi dùng LocalDB.

3. Build và chạy
  dotnet build .\B2BWholesale.sln
Terminal 1 — web:
  dotnet dev-certs https --trust
  dotnet run --project .\AgentPortal\AgentPortal.csproj
Mở https://localhost:7214 hoặc địa chỉ HTTPS hiện ở dòng "Now listening on".
Terminal 2 — WinForms (giữ web đang chạy):
  dotnet run --project .\BackOffice\BackOffice.csproj
Nếu dùng Visual Studio 2022: mở B2BWholesale.sln. Dùng Set Startup Projects > Multiple startup projects, chọn Start cho AgentPortal và BackOffice. Nhấn F5.

4. Đăng nhập và kiểm tra luồng chính
Mật khẩu mẫu cho cả ba tài khoản: Wholesale!Demo26
  Web: agent01. WinForms: admin. agent15 bị khóa.
Trên web, ghi lại tồn kho của hai sản phẩm, đặt lần lượt 2 và 3 đơn vị. Vào Đơn của tôi, ghi lại mã đơn và kiểm tra tổng = số lượng × đơn giá cộng trên các dòng. Chọn Xem và in hóa đơn > In hóa đơn để in hoặc lưu PDF.
Trong WinForms, mở Đơn hàng > Tải lại đơn từ web. Chọn đúng mã đơn, kiểm tra chi tiết, đổi Pending sang Shipped rồi lưu. Tải lại trang Đơn của tôi trên web để thấy trạng thái mới. Kiểm tra tồn kho hai sản phẩm giảm đúng 2 và 3.
Thử admin trên web hoặc agent01 trong WinForms để kiểm tra phân quyền. agent15 phải bị web từ chối ngay cả khi mật khẩu đúng.

5. Chạy 12 ca kiểm thử
  dotnet test .\AgentPortal.Tests\AgentPortal.Tests.csproj --logger "console;verbosity=normal"
Test dùng SQLite trong bộ nhớ, độc lập với dữ liệu SQL Server. Đọc tổng số Passed/Failed ở cuối Terminal. Các ca bao phủ trừ kho, rollback khi một dòng hàng thiếu, tổng tiền theo đơn giá lúc mua và kiểm tra tài khoản bị khóa.

6. Lỗi thường gặp
- Không tìm thấy dotnet: cài SDK rồi đóng và mở lại VS Code.
- MSB3644 khi build WinForms: cài .NET Framework 4.8 Developer Pack hoặc Targeting Pack.
- Không kết nối được SQL: kiểm tra sqllocaldb info MSSQLLocalDB, tên instance và database trong hai tệp cấu hình.
- Invalid object name 'Users': kiểm tra database đã được khởi tạo đủ bảng ở bước 2.
- Khi chạy lại script báo đã tồn tại bảng: dùng database đã tạo. Giữ dữ liệu nếu đã có đơn cần demo.

7. Phiếu xuất hàng và Dashboard (WinForms)
Đăng nhập admin > Đơn hàng > chọn đúng dòng > Phiếu xuất hàng. Chọn nơi lưu HTML. Trình duyệt mở phiếu; bấm In phiếu / Lưu PDF rồi chọn máy in hoặc Save as PDF. In phiếu không đổi trạng thái đơn.
Từ màn hình chính chọn Dashboard thống kê. Hàng bán chạy xếp theo tổng số lượng đã giao; doanh thu tính theo Quantity × UnitAmount của đơn Shipped (giá tại thời điểm đặt). Chọn đại lý và bấm Áp dụng / Tải lại để lọc hai thống kê này. Tab cảnh báo dùng tồn hiện tại của toàn kho, kể cả mặt hàng có tồn đúng bằng ngưỡng.
Với dữ liệu mẫu, đặt ngưỡng 150 để dễ thấy hàng cảnh báo. Để kiểm tra doanh thu, ghi lại số liệu một đại lý, chuyển một đơn Pending của đại lý sang Shipped, tải lại Dashboard và đối chiếu số tiền tăng với tổng tiền của đơn.
