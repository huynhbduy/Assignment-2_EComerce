using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Wholesale
{
    static class ShippingManifest
    {
        static string Field(DataRow row, string name)
        {
            return WebUtility.HtmlEncode(Convert.ToString(row[name]));
        }

        internal static void Export(Form owner, int orderId)
        {
            var rows = Db.Read(@"SELECT o.OrderDate, o.OrderStatus, a.AgentName, a.Address, a.Phone,
                    d.ItemID, i.ItemName, i.Size, d.Quantity
                FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID
                LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID
                LEFT JOIN dbo.Item i ON i.ItemID=d.ItemID
                WHERE o.OrderID=@id ORDER BY d.ID",
                new SqlParameter("@id", SqlDbType.Int) { Value = orderId });
            if (rows.Rows.Count == 0) { UI.Alert("Đơn đã bị xóa. Hãy tải lại danh sách."); return; }
            var first = rows.Rows[0];
            var html = new StringBuilder(@"<!doctype html><html lang='vi'><meta charset='utf-8'>
<title>Phiếu xuất hàng #").Append(orderId).Append(@"</title>
<style>
@page{size:A4;margin:16mm}body{max-width:800px;margin:2em auto;font:16px/1.5 Arial,sans-serif;color:#173a34}
table{width:100%;border-collapse:collapse}td,th{border:1px solid #aaa;padding:6px;text-align:left}
footer{display:flex;justify-content:space-around;margin-top:2em;break-inside:avoid}
@media print{button{display:none}body{margin:0}thead{display:table-header-group}tr{break-inside:avoid}}
</style><body><button onclick='window.print()'>In phiếu / Lưu PDF</button><h1>Phiếu xuất hàng</h1><p>Đơn #");
            html.Append(orderId).Append(" · Ngày đặt: ").Append(((DateTime)first["OrderDate"]).ToString("dd/MM/yyyy"))
                .Append(" · Trạng thái: ").Append(Field(first,"OrderStatus"))
                .Append("</p><p><b>Đại lý nhận: ").Append(Field(first,"AgentName")).Append("</b><br>Địa chỉ: ")
                .Append(Field(first,"Address")).Append("<br>Điện thoại: ").Append(Field(first,"Phone"))
                .Append("</p><table><thead><tr><th>Mã hàng</th><th>Tên hàng</th><th>Quy cách</th><th>Số lượng</th></tr></thead><tbody>");
            long total = 0;
            foreach (DataRow row in rows.Rows)
            {
                if (row.IsNull("ItemID")) continue;
                html.Append("<tr><td>").Append(Field(row,"ItemID")).Append("</td><td>").Append(Field(row,"ItemName"))
                    .Append("</td><td>").Append(Field(row,"Size")).Append("</td><td>").Append(Field(row,"Quantity")).Append("</td></tr>");
                total += (int)row["Quantity"];
            }
            html.Append("</tbody></table><p>Tổng số lượng: <b>").Append(total)
                .Append("</b></p><footer><b>Người xuất kho</b><b>Người nhận hàng</b></footer></body></html>");
            using (var dialog = new SaveFileDialog { FileName = "PhieuXuat_" + orderId + ".html",
                Filter = "Phiếu xuất hàng HTML (*.html)|*.html", DefaultExt = "html" })
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK) return;
                File.WriteAllText(dialog.FileName, html.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
    }
}
