using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laharika_File_Management
{
    public partial class Form1 : Form
    {
        private static string orderid,SearchType;
        private static string OrderDetailsPath, Filter, AllowAppClosing, PrintingMsg, 
                                PrintCompletedMsg, copyOnEnter, OrderFilesPath, TodayFolderPathLocal;
        private static bool Iscommentable = true, PopUpNotification;
        public static DataTable gridviewdata = new DataTable();
        public Form1()
        {
            ReadConfigurations();
          //  InitializeWatcher();
            InitializeComponent();
          
        }


        private void ReadConfigurations()
        {
            OrderDetailsPath = ConfigurationManager.AppSettings["OrderDetailsPath"];
            PopUpNotification = Convert.ToBoolean(ConfigurationManager.AppSettings["PopUpNotification"]);
            Filter = ConfigurationManager.AppSettings["Filter"].ToString();
            AllowAppClosing = ConfigurationManager.AppSettings["AllowAppClosing"];
            PrintingMsg = ConfigurationManager.AppSettings["PrintingMsg"];
            PrintCompletedMsg = ConfigurationManager.AppSettings["PrintCompletedMsg"];
            copyOnEnter = ConfigurationManager.AppSettings["CopyOnEnter"].ToString();
            OrderFilesPath = ConfigurationManager.AppSettings["OrderFilesPath"];
            TodayFolderPathLocal = ConfigurationManager.AppSettings["TodayFolderPathLocal"];
            SearchType = ConfigurationManager.AppSettings["SearchType"];
           // Control.CheckForIllegalCrossThreadCalls = false;
            ///fileSystemWatcher1.Path = OrderDetailsPath;
        }
        private void CustomMsgBox(string msg)
        {
            Form frm = new Form();
            frm.Text = "Message";
            Label lb = new Label();
            lb.Text = msg;
            lb.Font = new Font("Arial", 15, FontStyle.Bold);
            lb.TextAlign = ContentAlignment.MiddleCenter;
            lb.Dock = DockStyle.Fill;
            frm.BackColor = Color.White;
            frm.StartPosition = FormStartPosition.CenterScreen;
            //frm.Size = new Size(300, 100);
            frm.AutoSize = true;
            frm.Controls.Add(lb);
            frm.TopMost = true;
            frm.Show();

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            fileSystemWatcher1.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fileSystemWatcher1.Path = OrderDetailsPath;
           
            gridviewdata.Columns.Add("Order ID");
            gridviewdata.Columns.Add("Folder Name");
            gridviewdata.Columns.Add("Files Count");
            gridviewdata.Columns.Add("Status");
            gridviewdata.Columns.Add("Comments");
            try
            {
                ReadOrders();
            }
            catch(Exception ex)
            {
                Log("Error in ReadOrders() method " +ex.Message);
            }
        }
        private void ReadOrders()
        {
            gridviewdata.Clear();
            string[] files = Directory.GetFiles(OrderDetailsPath, $"{SearchType}*", SearchOption.AllDirectories);
              
            string order, folder="", count="", status="", comments="";

            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file).Contains(Filter) || Path.GetFileNameWithoutExtension(file).Contains(PrintingMsg))
                {
                    comments = "";
                    order = Path.GetFileName(file).Split('$')[0];
                    status = Path.GetFileNameWithoutExtension(file).Split('$')[1].Substring(1);
                    string[] data = File.ReadAllLines(file);
                    if (data.Length >= 3)
                    {
                        folder = data[0].Split(':')[1];
                        count = data[1].Split(':')[1];
                        if (data.Length > 3)
                        {
                            comments = data[3].Split(':')[1];
                        }
                    }

                    gridviewdata.Rows.Add(order, folder, count, status, comments);
                }
            }
            if (gridviewdata.Rows.Count > 0)
            {
                orderid = gridviewdata.Rows[0][0].ToString();
            }
            dataGridView1.DataSource = gridviewdata;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Update();
            GC.Collect();
        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {

        }

        private void fileSystemWatcher1_Created(object sender, FileSystemEventArgs e)
        {
            int a, b;
            while (true)
            {
                try
                {
                    a = File.ReadLines(e.FullPath).Count();
                    Thread.Sleep(5000);
                    b = File.ReadLines(e.FullPath).Count();
                    if (a == b)
                    { break;}
                }
                catch { }
            }

            if(!PopUpNotification)
            {
                return;
            }
            if (Path.GetFileNameWithoutExtension(e.Name).StartsWith(SearchType))
            {
                if (Path.GetFileNameWithoutExtension(e.Name).Contains(Filter))
                {
                    string order = Path.GetFileNameWithoutExtension(e.Name).Split('$')[0];
                    order = "Order No:- " + order;
                    ReadOrders();
                    CustomMsgBox(order);
                }
            }
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            e.Cancel = false;
            dataGridView1.AllowUserToDeleteRows = true;
            var rows = dataGridView1.SelectedRows;
             foreach(var row in rows)
            {
                string order = gridviewdata.Rows[e.Row.Index]["Order ID"].ToString();
                string temp = PrintCompletedMsg.Substring(2).Replace(".txt", "");
               var msg  =  MessageBox.Show($"{temp} : "+ order,"Order status", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
             
                if(msg == DialogResult.Yes)
                {
                    UpdateOrderStatus(order, PrintCompletedMsg);
                    Close();
                }
                else
                {
                    e.Cancel = true;
                }
            }
           

        }

        private static void UpdateOrderStatus(string Order,string Status)
        {
            string SourcePath = Path.Combine(OrderDetailsPath,Order + Filter + ".txt");
            string DestPath = Path.Combine(OrderDetailsPath,Order + Status + ".txt");
            File.Move(SourcePath, DestPath);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = dataGridView1.SelectedRows[0];
            orderid = row.Cells[0].Value.ToString();
            if(row.Cells[4].Value.ToString().Length > 2)
            {
                Iscommentable = false;
            }
            else
            {
                Iscommentable = true;
            }
        }

        private void dataGridView1_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                DataGridViewRow row = dataGridView1.SelectedRows[0];
                string order = row.Cells[0].Value.ToString();
                var msg = MessageBox.Show($"Copy : " + order, "Order status", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (msg == DialogResult.Yes)
                {
                    Close();
                    CopyOrder(order);
                    MessageBox.Show(order + " Copied", "Order status", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            fileSystemWatcher1.Dispose();
            Application.Exit();
        }
        private static void CopyOrder(string order)
        {
            ///////////////////
            string sourcePath = Path.Combine(OrderFilesPath, order);
            string targetPath = Path.Combine(TodayFolderPathLocal, order);
                Directory.CreateDirectory(targetPath);
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
                }

                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
                }
            UpdateOrderStatus(order,PrintingMsg);
        }
        private void Search_Click(object sender, EventArgs e)
        {
            var name = TextBox_Name.Text;
            var order = TextBox_Order_ID.Text;
            var temp = gridviewdata.Select($"[Folder Name] LIKE '%{name}%' AND [Order ID] LIKE '%{order}%'").CopyToDataTable();
            dataGridView1.DataSource = temp;
            dataGridView1.Refresh();
        }

        private void UpdateComment_Click(object sender, EventArgs e)
        {
            if(!Iscommentable)
            {
                MessageBox.Show("Comments can't be modified");
                return;
            }
           string path = Directory.GetFiles(OrderDetailsPath, $"{orderid}*")[0];
            DialogResult msg = MessageBox.Show($"add comments to order : {orderid}","Message", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (msg == DialogResult.Yes)
                File.AppendAllText(path, $"Comments : {TextBox_Comments.Text}\n");
            ReadOrders();
            TextBox_Comments.Text = "";
        }

        private void Refresh(object sender, EventArgs e)
        {
            ReadOrders();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AllowAppClosing == "True")
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Log(string Message)
        {
            string path = ConfigurationManager.AppSettings["LogPath"];
            File.AppendAllText(path, DateTime.Now + " : " + Message + "\n");
                
        }
    }
}
