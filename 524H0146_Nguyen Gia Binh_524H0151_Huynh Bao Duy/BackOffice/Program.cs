using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Wholesale
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var login = new Login())
                if (login.ShowDialog() == DialogResult.OK)
                    Application.Run(new Dashboard());
        }
    }

    static class Db
    {
        internal static SqlConnection Connect()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["Wholesale"].ConnectionString);
        }
        internal static DataTable Read(string sql, params SqlParameter[] parameters)
        {
            using (var cn = Connect())
            using (var cmd = new SqlCommand(sql, cn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddRange(parameters);
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }
    }

    static class UI
    {
        internal static Button Button(string text, Action action)
        {
            var b = new Button { Text = text, AutoSize = true };
            b.Click += (s,e) => Run(action);
            return b;
        }
        internal static void Run(Action action)
        {
            try { action(); }
            catch (DBConcurrencyException) { Alert("Dữ liệu đã thay đổi. Hãy tải lại trước khi sửa tiếp."); }
            catch (SqlException ex) { Alert(ex.Number == 547 ? "Dữ liệu vi phạm ràng buộc hoặc đang được tham chiếu." : "Lỗi SQL Server (" + ex.Number + "). Kiểm tra kết nối và cơ sở dữ liệu."); }
            catch (Exception ex) { Alert(ex.Message); }
        }
        internal static void Alert(string message) { MessageBox.Show(message, "Back-Office"); }
        internal static DataGridView Grid(bool editable = false)
        {
            return new DataGridView { Dock = DockStyle.Fill, ReadOnly = !editable,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        }
    }

    sealed class Login : Form
    {
        readonly TextBox name = new TextBox { MaxLength = 80, Width = 245 };
        readonly TextBox password = new TextBox { UseSystemPasswordChar = true, Width = 245 };
        internal Login()
        {
            Text = "Đăng nhập quản trị kho"; ClientSize = new Size(310,195);
            StartPosition = FormStartPosition.CenterScreen;
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18),
                FlowDirection = FlowDirection.TopDown, WrapContents = false };
            var submit = UI.Button("Đăng nhập", Authenticate);
            panel.Controls.AddRange(new Control[] { new Label { Text = "Tên đăng nhập", AutoSize = true }, name,
                new Label { Text = "Mật khẩu", AutoSize = true }, password, submit });
            Controls.Add(panel); AcceptButton = submit;
        }
        void Authenticate()
        {
            var table = Db.Read("SELECT [password] FROM dbo.Users WHERE UserName=@name AND Role='Admin' AND [lock]=0",
                new SqlParameter("@name", SqlDbType.NVarChar,80) { Value = name.Text.Trim() });
            if (table.Rows.Count == 1 && Verify(password.Text, (string)table.Rows[0][0]))
                DialogResult = DialogResult.OK;
            else { password.Clear(); password.Focus(); UI.Alert("Thông tin đăng nhập hoặc quyền truy cập không hợp lệ."); }
        }
        // Identity V3 header contains big-endian PRF, iteration count and salt length.
        static int Number(byte[] data, int pos)
        {
            return data[pos]<<24 | data[pos+1]<<16 | data[pos+2]<<8 | data[pos+3];
        }
        static bool Verify(string input, string encoded)
        {
            byte[] data;
            try { data = Convert.FromBase64String(encoded); } catch (FormatException) { return false; }
            if (data.Length < 45 || data[0] != 1) return false;
            int prf = Number(data,1), rounds = Number(data,5), saltLength = Number(data,9);
            if (prf < 0 || prf > 2 || rounds < 1 || rounds > 2000000 ||
                saltLength < 16 || saltLength > data.Length-29) return false;
            var salt = new byte[saltLength]; Array.Copy(data,13,salt,0,saltLength);
            var algorithms = new[] { HashAlgorithmName.SHA1, HashAlgorithmName.SHA256, HashAlgorithmName.SHA512 };
            using (var hash = new Rfc2898DeriveBytes(input,salt,rounds,algorithms[prf]))
            {
                int start = 13 + saltLength;
                var key = hash.GetBytes(data.Length-start);
                int difference = 0;
                for (int i=0; i<key.Length; i++) difference |= key[i] ^ data[start+i];
                return difference == 0;
            }
        }
    }

    sealed class Dashboard : Form
    {
        internal Dashboard()
        {
            Text = "Quản trị kho B2B"; ClientSize = new Size(650,95);
            StartPosition = FormStartPosition.CenterScreen;
            var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            menu.Controls.Add(UI.Button("Hàng hóa", () => { using (var f = new Catalog(true)) f.ShowDialog(this); }));
            menu.Controls.Add(UI.Button("Đại lý", () => { using (var f = new Catalog(false)) f.ShowDialog(this); }));
            menu.Controls.Add(UI.Button("Đơn hàng", () => { using (var f = new Orders()) f.ShowDialog(this); }));
            menu.Controls.Add(UI.Button("Dashboard thống kê", () => { using (var f = new Statistics()) f.ShowDialog(this); }));
            Controls.Add(menu);
        }
    }
}
