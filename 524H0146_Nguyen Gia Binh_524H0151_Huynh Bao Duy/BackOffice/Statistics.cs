using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Wholesale
{
    sealed class Statistics : Form
    {
        readonly DataGridView sales = UI.Grid(), stock = UI.Grid(), revenue = UI.Grid();
        readonly ComboBox agents = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 185 };
        readonly NumericUpDown threshold = new NumericUpDown { Minimum = 0, Maximum = int.MaxValue, Value = 20, Width = 80 };

        internal Statistics()
        {
            Text = "Dashboard thống kê"; ClientSize = new Size(940,510);
            StartPosition = FormStartPosition.CenterParent;
            var tabs = new TabControl { Dock = DockStyle.Fill };
            AddTab(tabs,"Hàng bán chạy · đã giao",sales);
            AddTab(tabs,"Cảnh báo tồn kho",stock);
            AddTab(tabs,"Doanh thu đại lý · đã giao",revenue);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
            bar.Controls.AddRange(new Control[] {
                new Label { Text = "Đại lý (bán hàng)", AutoSize = true }, agents,
                new Label { Text = "Cảnh báo khi tồn ≤", AutoSize = true }, threshold,
                UI.Button("Áp dụng / Tải lại", RefreshStats) });
            Controls.Add(tabs); Controls.Add(bar);
            Shown += (s,e) => UI.Run(RefreshStats);
        }

        static void AddTab(TabControl tabs, string title, DataGridView grid)
        {
            var page = new TabPage(title); page.Controls.Add(grid); tabs.TabPages.Add(page);
        }

        static SqlParameter AgentFilter(int id)
        {
            return new SqlParameter("@agent", SqlDbType.Int) { Value = id };
        }

        void RefreshStats()
        {
            int id = agents.SelectedValue is int ? (int)agents.SelectedValue : 0;
            var choices = Db.Read("SELECT AgentID, AgentName FROM dbo.Agent ORDER BY AgentName, AgentID");
            var all = choices.NewRow(); all["AgentID"] = 0; all["AgentName"] = "Tất cả đại lý";
            choices.Rows.InsertAt(all,0);
            if (choices.Select("AgentID = " + id).Length == 0) id = 0;
            var best = Db.Read(@"SELECT i.ItemID AS [Mã hàng], i.ItemName AS [Tên hàng], i.Size AS [Quy cách],
                    SUM(CAST(d.Quantity AS bigint)) AS [Số lượng đã giao]
                FROM dbo.OrderDetail d JOIN dbo.[Order] o ON o.OrderID=d.OrderID
                JOIN dbo.Item i ON i.ItemID=d.ItemID
                WHERE o.OrderStatus='Shipped' AND (@agent=0 OR o.AgentID=@agent)
                GROUP BY i.ItemID,i.ItemName,i.Size ORDER BY [Số lượng đã giao] DESC,i.ItemID",AgentFilter(id));
            var low = Db.Read(@"SELECT ItemID AS [Mã hàng], ItemName AS [Tên hàng], Size AS [Quy cách],
                    StockQuantity AS [Tồn kho] FROM dbo.Item WHERE StockQuantity<=@limit
                    ORDER BY StockQuantity,ItemID",
                new SqlParameter("@limit",SqlDbType.Int) { Value = (int)threshold.Value });
            var totals = Db.Read(@"SELECT a.AgentID AS [Mã đại lý], a.AgentName AS [Đại lý],
                    COUNT(DISTINCT o.OrderID) AS [Số đơn đã giao],
                    COALESCE(SUM(d.Quantity*d.UnitAmount),0) AS [Doanh thu (VND)]
                FROM dbo.Agent a LEFT JOIN dbo.[Order] o ON o.AgentID=a.AgentID AND o.OrderStatus='Shipped'
                LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID
                WHERE @agent=0 OR a.AgentID=@agent GROUP BY a.AgentID,a.AgentName
                ORDER BY [Doanh thu (VND)] DESC,a.AgentID",AgentFilter(id));
            agents.DisplayMember = "AgentName"; agents.ValueMember = "AgentID";
            agents.DataSource = choices; agents.SelectedValue = id;
            sales.DataSource = best; stock.DataSource = low; revenue.DataSource = totals;
            revenue.Columns["Doanh thu (VND)"].DefaultCellStyle.Format = "#,0.##";
        }
    }
}
