using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Wholesale
{
    sealed class Orders : Form
    {
        readonly DataGridView orders = UI.Grid(), lines = UI.Grid();
        readonly ComboBox status = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        bool loading;
        internal Orders()
        {
            Text = "Quản lý đơn hàng"; ClientSize = new Size(920,580);
            StartPosition = FormStartPosition.CenterParent;
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
                Size = ClientSize, SplitterDistance = 275 };
            split.Panel1.Controls.Add(orders); split.Panel2.Controls.Add(lines);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            status.Items.AddRange(new object[] {"Pending","Shipped"});
            bar.Controls.AddRange(new Control[] { UI.Button("Tải lại đơn từ web", Reload), status,
                UI.Button("Lưu trạng thái", Save), UI.Button("Phiếu xuất hàng", () => {
                    if (orders.CurrentRow == null) { UI.Alert("Hãy chọn một đơn hàng."); return; }
                    ShippingManifest.Export(this, (int)orders.CurrentRow.Cells["OrderID"].Value);
                }) });
            Controls.Add(split); Controls.Add(bar);
            orders.SelectionChanged += (s,e) => { if (!loading) UI.Run(Details); };
            Shown += (s,e) => UI.Run(Reload);
        }
        void Reload()
        {
            loading = true;
            try {
                orders.DataSource = Db.Read(@"SELECT o.OrderID,o.OrderDate,a.AgentName,o.OrderStatus,
                    COALESCE((SELECT SUM(d.Quantity*d.UnitAmount) FROM dbo.OrderDetail d
                    WHERE d.OrderID=o.OrderID),0) AS TotalAmount
                    FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID
                    ORDER BY o.OrderDate DESC,o.OrderID DESC");
            } finally { loading = false; }
            Details();
        }
        void Details()
        {
            lines.DataSource = null; status.SelectedIndex = -1;
            if (orders.CurrentRow == null) return;
            var row = orders.CurrentRow;
            status.SelectedItem = row.Cells["OrderStatus"].Value;
            lines.DataSource = Db.Read(@"SELECT i.ItemName,i.Size,d.Quantity,d.UnitAmount,
                d.Quantity*d.UnitAmount AS LineAmount FROM dbo.OrderDetail d
                JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE d.OrderID=@id ORDER BY d.ID",
                new SqlParameter("@id",SqlDbType.Int) { Value = row.Cells["OrderID"].Value });
        }
        void Save()
        {
            if (orders.CurrentRow == null || status.SelectedItem == null) return;
            var row = orders.CurrentRow;
            using (var cn = Db.Connect())
            using (var cmd = new SqlCommand(@"UPDATE dbo.[Order] SET OrderStatus=@new
                WHERE OrderID=@id AND OrderStatus=@old",cn))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value = row.Cells["OrderID"].Value;
                cmd.Parameters.Add("@new",SqlDbType.VarChar,10).Value = status.SelectedItem;
                cmd.Parameters.Add("@old",SqlDbType.VarChar,10).Value = row.Cells["OrderStatus"].Value;
                cn.Open(); if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException();
            }
            row.Cells["OrderStatus"].Value = status.SelectedItem;
            UI.Alert("Đã lưu trạng thái đơn #"+row.Cells["OrderID"].Value+".");
        }
    }
}
