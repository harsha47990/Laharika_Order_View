using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laharika_File_Management
{
    public partial class Form1 : Form
    {
        private static string orderid,SearchType;
        private static string OrderDetailsPath, Filter, AllowAppClosing, RowEnterPress, 
                                RowDeletePress, copyOnEnter, OrderFilesPath, TodayFolderPathLocal;
        private static bool Iscommentable = true, PopUpNotification;
        private static int StatusReadLower, StatusReadUpper;
        public static DataTable gridviewdata = new DataTable();
        private static Hashtable OrderStatusCode = new Hashtable();
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
            RowEnterPress = ConfigurationManager.AppSettings["RowEnterPress"];
            RowDeletePress = ConfigurationManager.AppSettings["RowDeletePress"];
            copyOnEnter = ConfigurationManager.AppSettings["CopyOnEnter"].ToString();
            OrderFilesPath = ConfigurationManager.AppSettings["OrderFilesPath"];
            TodayFolderPathLocal = ConfigurationManager.AppSettings["TodayFolderPathLocal"];
            SearchType = ConfigurationManager.AppSettings["SearchType"];
            var lines = ConfigurationManager.AppSettings["OrderCodes"].Split(',');
            int i = 1;
            foreach(var line in lines)
            {
                OrderStatusCode.Add(line.Split(':')[0].Trim().ToString(),i);
                i++;
            }
            StatusReadLower = Convert.ToInt32(OrderStatusCode[Filter]);
            StatusReadUpper = Convert.ToInt32(OrderStatusCode[RowDeletePress]);
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["CreateTodayFolder"]))
            {
                TodayFolderPathLocal = Path.Combine(TodayFolderPathLocal, DateTime.Now.ToString("dd-MM-yyyy"));
                if (!Directory.Exists(TodayFolderPathLocal)) 
                { Directory.CreateDirectory(TodayFolderPathLocal); }
            }
            // Control.CheckForIllegalCrossThreadCalls = false;
            ///fileSystemWatcher1.Path = OrderDetailsPath;
        }
        private void CustomMsgBox(string msg)
        {
            if(!PopUpNotification)
            {
                return;
            }
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
            try
            {
                fileSystemWatcher1.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fileSystemWatcher1.Path = OrderDetailsPath;
           
            gridviewdata.Columns.Add("Order ID");
            gridviewdata.Columns.Add("Folder Name");
            gridviewdata.Columns.Add("Files Count");
            gridviewdata.Columns.Add("Status");
            gridviewdata.Columns.Add("Comments");
           
            ReadOrders();
            }
            catch(Exception ex)
            {
                string msg = "Error in Form1_Load method " + ex.Message;
                Log(msg);
                Email(msg);
            }
        }
        private void ReadOrders()
        {
            try
            {
                gridviewdata.Clear();
                var files = Directory.GetFiles(OrderDetailsPath, $"{SearchType}*", SearchOption.AllDirectories).OrderByDescending(d => new FileInfo(d).CreationTime);

                Log("number of files in the order details path is " + files.Count());
                string order, folder = "", count = "", status = "", comments = "";

                foreach (var file in files)
                {
                    if (ValidateFileOrderStatus(Path.GetFileNameWithoutExtension(file)))
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
            catch (Exception ex)
            {
                string msg = "Error in ReadOrders() method " + ex.Message;
                Log(msg);
                Email(msg);
            }
        }
        private bool ValidateFileOrderStatus(string FileName)
        {
            try
            {
                //Log("validating file name : " + FileName);
                string status = "$" + FileName.Split('$')[1];
                int val = Convert.ToInt32(OrderStatusCode[status]);
              //  Log("Status Read Lower Limit : " + StatusReadLower + " , upper limit : " + StatusReadUpper);
               // Log("order code for validated file : " + val);
                if (val >= StatusReadLower && val < StatusReadUpper)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in ReadOrders() method " + ex.Message;
                Log(msg);
                Email(msg);
            }
            return false;
        }
        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
          //   MessageBox.Show(e.Name);
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
                string temp = RowDeletePress.Substring(2).Replace(".txt", "");
               var msg  =  MessageBox.Show($"{temp} : "+ order,"Order status", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (msg == DialogResult.Yes)
                {
                    MoveFolderOnDeleteRow(order);
                    if (!UpdateOrderStatus(order, RowDeletePress))
                    {  e.Cancel = true; }
                }
                else
                {
                    e.Cancel = true;
                }
            }
           

        }

        private static void MoveFolderOnDeleteRow(string Source)
        {
            if(Convert.ToBoolean(ConfigurationManager.AppSettings["MoveOnDelete"]))
            {
                CopyFilesRecursively(Path.Combine(TodayFolderPathLocal, Source),ConfigurationManager.AppSettings["MoveOnDeletePath"]);
                VerfiyAndRemove(Path.Combine(TodayFolderPathLocal, Source), ConfigurationManager.AppSettings["MoveOnDeletePath"]);
            }
        }
        private static void VerfiyAndRemove(string source, string destination)
        {
            try
            {
                if (DirSize(destination) == DirSize(source))
                {
                    try
                    {
                        Directory.Delete(source, true);
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in Verify And Remove method, message: " + ex.Message + "\n stack trace : " + ex.StackTrace;
                Log(msg);
                Email(msg);
            }

        }

        public static long DirSize(string path)
        {
            try
            {
                DirectoryInfo d = new DirectoryInfo(path);
                long size = 0;
                // Add file sizes.
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                // Add subdirectory sizes.
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    size += DirSize(di.FullName);
                }
                return size;
            }
            catch (Exception ex)
            {
                string msg = "Error in DirSize method, message: " + ex.Message + "\n stack trace : " + ex.StackTrace;
                Log(msg);
                Email(msg);
            }
            return 0;
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            try
            {
                var FilesCount = 0;
                var FileNames = "";
                targetPath = Path.Combine(targetPath, Path.GetFileName(sourcePath));
                Directory.CreateDirectory(targetPath);
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
                }

                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    FilesCount++;
                    FileNames += "," + Path.GetFileName(newPath);
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in CopyFilesRecursively method, message: " + ex.Message + "\n stack trace : " + ex.StackTrace;
                Log(msg);
                Email(msg);
            }
        }
        private void fileSystemWatcher1_Renamed(object sender, RenamedEventArgs e)
        {
            if (Path.GetFileNameWithoutExtension(e.Name).StartsWith(SearchType))
            {
                string name = Path.GetFileNameWithoutExtension(e.Name);
                string order = name.Split('$')[0];
                string status = "$" + name.Split('$')[1];
                int val = Convert.ToInt32(OrderStatusCode[status]);
                if (val >= StatusReadLower || val < StatusReadUpper)
                {
                   // MessageBox.Show(status.Replace("$_", ""), "Order status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CustomMsgBox(order + "\n status Updated : " + status.Replace("$_", ""));
                    return;
                }
            }

             
        }

        private static bool UpdateOrderStatus(string Order,string Status)
        {
            string SourcePath = Path.Combine(OrderDetailsPath,Order + Filter + ".txt");
            if(!File.Exists(SourcePath))
            {
                SourcePath = Path.Combine(OrderDetailsPath, Order + RowEnterPress + ".txt");
            }
            if (!File.Exists(SourcePath))
            {
                MessageBox.Show("Order Can't be Updated as " + RowDeletePress.Replace("$_", "") + " before " + RowEnterPress.Replace("$_", ""));
                return false;
            }
            string DestPath = Path.Combine(OrderDetailsPath,Order + Status + ".txt");
            File.Move(SourcePath, DestPath);
            return true;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridViewRow row = dataGridView1.SelectedRows[0];
                orderid = row.Cells[0].Value.ToString();
                if (row.Cells[4].Value.ToString().Length > 2)
                {
                    Iscommentable = false;
                }
                else
                {
                    Iscommentable = true;
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in ReadOrders() method " + ex.Message;
                Log(msg);
                Email(msg);
            }
        }

        private void dataGridView1_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter && Convert.ToBoolean(copyOnEnter))
            {
                DataGridViewRow row = dataGridView1.SelectedRows[0];
                string order = row.Cells[0].Value.ToString();
                var msg = MessageBox.Show($"Copy : " + order, "Order status", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (msg == DialogResult.Yes)
                {
                    //Close();
                    CopyOrder(order);
                    MessageBox.Show(order + " Copied Successful", "Order status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ReadOrders();
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
            try
            {
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
                UpdateOrderStatus(order, RowEnterPress);
            }
            catch (Exception ex)
            {
                string msg = "Error in ReadOrders() method " + ex.Message;
                Log(msg);
                Email(msg);
            }
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
            int closetime = Convert.ToInt32(ConfigurationManager.AppSettings["ShopClosingTime24Hr"]);
            if (Convert.ToBoolean(AllowAppClosing) || DateTime.Now.Hour >= closetime)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private static void Log(string Message)
        {
            string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + DateTime.Now.ToString("dd_MM_yyyy") + "_OrderViewApp_Log.txt";
            File.AppendAllText(path, DateTime.Now + " : " + Message + "\n");
        }     

        private static void Email(string msg)
        {
            if(!Convert.ToBoolean(ConfigurationManager.AppSettings["SendEmailAlert"]))
            {
                Log("Email Alert Disabled, unable to send email");
                return;
            }
            string to = ConfigurationManager.AppSettings["AlertToEmail"]; //To address    
            string from = ConfigurationManager.AppSettings["EmailId"]; //From address
            string pass = ConfigurationManager.AppSettings["Password"];
            MailMessage message = new MailMessage(from, to);

            string mailbody = msg;
            message.Subject = "Error in Laharika Service Application";
            message.Body = mailbody;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential(from, pass);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                Log("Error Sending Email : " + ex.Message);
                //throw ex;
            }
        }
    }
}
