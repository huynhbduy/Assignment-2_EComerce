using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Wholesale
{
    sealed class Catalog : Form
    {
        readonly bool items;
        readonly string table, id;
        readonly string[] fields;
        readonly DataGridView grid = UI.Grid();
        internal Catalog(bool items)
        {
            this.items = items;
            table = items ? "dbo.Item" : "dbo.Agent";
            id = items ? "ItemID" : "AgentID";
            fields = items ? new[] {"ItemName","Size","Price","StockQuantity"} :
                             new[] {"AgentName","Address","Phone"};
            Text = items ? "Quản lý hàng hóa" : "Quản lý đại lý";
            ClientSize = new Size(850,480); StartPosition = FormStartPosition.CenterParent;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            bar.Controls.AddRange(new Control[] { UI.Button("Thêm", () => Edit(null)),
                UI.Button("Sửa", () => Edit(Selected())), UI.Button("Xóa", Delete),
                UI.Button("Tải lại", Reload) });
            Controls.Add(grid); Controls.Add(bar); Shown += (s,e) => UI.Run(Reload);
        }
        void Reload()
        {
            grid.DataSource = Db.Read("SELECT "+id+","+string.Join(",",fields)+" FROM "+table+" ORDER BY "+id);
        }
        DataRow Selected()
        {
            if (grid.CurrentRow == null) throw new InvalidOperationException("Hãy chọn một dòng.");
            return ((DataRowView)grid.CurrentRow.DataBoundItem).Row;
        }
        // Compare original values to avoid overwriting concurrent stock deductions or edits.
        string Match(SqlCommand cmd, DataRow row)
        {
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = row[id];
            string where = " WHERE "+id+"=@id";
            foreach (string f in fields)
            {
                where += " AND "+f+"=@old"+f;
                cmd.Parameters.AddWithValue("@old"+f,row[f]);
            }
            return where;
        }
        void Delete()
        {
            var row = Selected();
            if (MessageBox.Show("Xóa "+row[fields[0]]+"?",Text,MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            using (var cn = Db.Connect())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM "+table+Match(cmd,row);
                cn.Open(); if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException();
            }
            Reload();
        }
        void Edit(DataRow row)
        {
            using (var form = new Form { Text = row == null ? "Thêm mới" : "Chỉnh sửa",
                ClientSize = new Size(410,245), StartPosition = FormStartPosition.CenterParent })
            {
                var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
                var editors = new Control[fields.Length];
                string[] labels = items ? new[] {"Tên hàng","Quy cách","Giá (VND)","Tồn kho"} :
                                          new[] {"Tên đại lý","Địa chỉ","Điện thoại"};
                for (int i=0; i<fields.Length; i++)
                {
                    string f = fields[i];
                    if (f == "Price" || f == "StockQuantity")
                    {
                        bool price = f == "Price";
                        var n = new NumericUpDown { DecimalPlaces = price ? 2 : 0,
                            Maximum = price ? 9999999999999999.99m : int.MaxValue,
                            Minimum = price ? 0.01m : 0m, ThousandsSeparator = true };
                        if (row != null) n.Value = Convert.ToDecimal(row[f]);
                        editors[i] = n;
                    }
                    else editors[i] = new TextBox { Text = row == null ? "" : row[f].ToString(),
                        MaxLength = f == "Address" ? 240 : f == "Size" ? 50 : f == "Phone" ? 20 : 120 };
                    editors[i].Width = 235;
                    panel.Controls.Add(new Label { Text = labels[i], AutoSize = true },0,i);
                    panel.Controls.Add(editors[i],1,i);
                }
                var save = UI.Button("Lưu", () => {
                    using (var cn = Db.Connect())
                    using (var cmd = cn.CreateCommand())
                    {
                        var assignments = new string[fields.Length];
                        for (int i=0; i<fields.Length; i++)
                        {
                            object value;
                            if (editors[i] is NumericUpDown n) value = n.Value;
                            else {
                                value = editors[i].Text.Trim();
                                if ((string)value == "") throw new InvalidOperationException("Vui lòng nhập "+labels[i]+".");
                            }
                            cmd.Parameters.AddWithValue("@"+fields[i],value);
                            assignments[i] = fields[i]+"=@"+fields[i];
                        }
                        cmd.CommandText = row == null
                            ? "INSERT INTO "+table+" ("+string.Join(",",fields)+") VALUES (@"+string.Join(",@",fields)+")"
                            : "UPDATE "+table+" SET "+string.Join(",",assignments)+Match(cmd,row);
                        cn.Open(); if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException();
                    }
                    form.DialogResult = DialogResult.OK;
                });
                panel.Controls.Add(save,1,fields.Length); form.Controls.Add(panel); form.AcceptButton = save;
                if (form.ShowDialog(this) == DialogResult.OK) Reload();
            }
        }
    }
}
