using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Reflection;
using System.Security.Cryptography;
using System.IO;

using WebSocketSharp;
using Newtonsoft.Json;


namespace SQL_Server
{
    public enum messageType
    {
        _default = -1,
        request = 0,
        response = 1
    }

    public class OCR_Database_Message
    {

        public int memberID = -1;
        public messageType messageType = messageType._default;
        public string dataKey = "";
        public string dataValue = "";

        public OCR_Database_Message() { }

        public OCR_Database_Message(int memberID, messageType messageType, string dataKey, string dataValue)
        {
            this.memberID = memberID;
            this.messageType = messageType;
            this.dataKey = dataKey;
            this.dataValue = dataValue;
        }

        

        public static string SerializeToJson(OCR_Database_Message message)
        {
            return JsonConvert.SerializeObject(message,Formatting.Indented);
        }

        public static OCR_Database_Message DeserializeFromJson(string json)
        {
            return JsonConvert.DeserializeObject<OCR_Database_Message>(json);
        }

        public override string ToString()
        {
            return string.Format("Item ID: {0}, messageType: {1}, dataKey: {2}, dataValue: {3}",this.memberID,this.messageType,this.dataKey,this.dataValue);
        }
        public string ToNoImageDataString()
        {
            return string.Format("Item ID: {0}, messageType: {1}, dataKey: {2}, dataValue: {3}", this.memberID, this.messageType, this.dataKey, "Large Image Data");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=localhost\\SQLEXPRESS01;Database=master;Trusted_Connection=True;";

            // ** table made ** //
            //AddTable(connectionString, "OCR_UT_DATABASE", "Member_Type");
            // ** table made ** //

            // ** Fabrication Process Column made ** //
            //Add_Text_ColumnToTable(connectionString, "OCR_UT_DATABASE", "Fabrication_Process");
            // ** Fabrication Process Column made ** //

            // ** image columns made ** //
            //AddBlobColumnToTable(connectionString, "OCR_UT_DATABASE", "Front_Image");
            //AddBlobColumnToTable(connectionString, "OCR_UT_DATABASE", "Back_Image");
            //AddBlobColumnToTable(connectionString, "OCR_UT_DATABASE", "Top_Image");
            //AddBlobColumnToTable(connectionString, "OCR_UT_DATABASE", "Bottom_Image");
            // ** image columns made ** //

            // ** add dummy items **//
            //for (int i = 0; i < 15; i++)
            //{
            //    InsertItem(connectionString, "OCR_UT_DATABASE", "Id", "Member_Type", i, "Normal" + i);
            //}
            // ** add dummy items **//

            // ** add dummy items **//
            //foreach (var item in columns)
            //{
            //    Console.WriteLine(item);
            //    Console.WriteLine("Please Write Data");
            //    string value = Console.ReadLine();
            //    Modify_Text_Or_Number_Item(connectionString, "testTable01", "Id", "real_number", columns.IndexOf(item), value);
            //}

            //string imagePath = @"C:\Users\akhaj\Pictures\New Wallpaper.jpg";
            //for (int i = 0; i < 15; i++)
            //{
            //    Modify_Blob_Item(connectionString,"OCR_UT_DATABASE","Id","Front_Image",i, imagePath);
            //    //InsertItem(connectionString, "OCR_UT_DATABASE", "Id", "Member_Type", i, "Normal" + i);
            //}
            // ** add dummy items **//

            bool forceClose = false;
            var webSocket = new WebSocket("ws://ocr-server-ut.glitch.me/");

            webSocket.OnOpen += (sender, e) =>
            {
                Console.WriteLine("WebSocket connection established.");
            };

            // Event triggered when a message is received
            webSocket.OnMessage += (sender, e) =>
            {
                

                if (!e.Data.IsNullOrEmpty() && !e.Data.Equals("Hello World"))
                {

                    string incomingText = e.Data;

                    try
                    {
                        OCR_Database_Message request = OCR_Database_Message.DeserializeFromJson(incomingText);


                        if (request.messageType == messageType.request)
                        {

                            switch (request.dataKey)
                            {

                                case ("Member_Type"):
                                case ("Fabrication_Process"):
                                    Console.WriteLine(request.ToString());

                                    string responseTextData = ReadItemDetail(connectionString, "OCR_UT_DATABASE", request.dataKey, request.memberID);

                                    OCR_Database_Message stringResponse = new OCR_Database_Message(request.memberID, messageType.response, request.dataKey, responseTextData);
                                    string jsonStringResponse = OCR_Database_Message.SerializeToJson(stringResponse);
                                    Console.WriteLine("Sending...");
                                    webSocket.Send(jsonStringResponse);
                                    Console.WriteLine("Sent!");

                                    break;

                                case ("Front_Image"):
                                case ("Back_Image"):
                                case ("Top_Image"):
                                case ("Bottom_Image"):
                                    Console.WriteLine(request.ToNoImageDataString());
                                    string responseImageData = Read_Blob_ItemDetail(connectionString, "OCR_UT_DATABASE", request.dataKey, request.memberID);

                                    OCR_Database_Message imageResponse = new OCR_Database_Message(request.memberID, messageType.response, request.dataKey, responseImageData);
                                    string jsonImageResponse = OCR_Database_Message.SerializeToJson(imageResponse);
                                    Console.WriteLine("Sending...");
                                    webSocket.Send(jsonImageResponse);
                                    Console.WriteLine("Sent!");
                                    break;

                                default:

                                    webSocket.Send("Item not found or column value is NULL. Please Check Spelling and Case_SeNsiViTy! and Space_Under_Lines");

                                    break;
                            }


                        }
                    }
                    catch
                    {

                        Console.WriteLine("Unrecognizable Message!");
                    }
                    




                    WriteInstructions();
                }

                

            };

            // Event triggered when the WebSocket connection is closed
            webSocket.OnClose += (sender, e) =>
            {
                Console.WriteLine("WebSocket connection closed.");
                if (!forceClose)
                {
                    while (!webSocket.IsAlive)
                    {
                        webSocket.Connect();
                    }
                    
                }
            };

            // Connect to the WebSocket server
            webSocket.Connect();
            //webSocket.Send("First Message From DataBase Client!");

            WriteInstructions();

            while (true)
            {
                string closeConnection = Console.ReadLine();
                if (closeConnection.ToUpper().Equals("CLOSE"))
                {
                    forceClose = true;
                    break;
                }
                else if (closeConnection.ToUpper().Equals("READ DATABASE"))
                {
                    ReadAllItems(connectionString, "OCR_UT_DATABASE");
                }
            }

            if (forceClose)
            {
                webSocket.Close();
            }

            Console.WriteLine("\nPress a Key to Continue.");
            Console.ReadKey();
        }


        static void WriteInstructions()
        {
            Console.WriteLine("\nTo close the connection, write 'close' and press enter to stop Websocket client.");
            Console.WriteLine("To read database data write 'read database' and press enter.");
        }

        static SqlConnection GetSqlConnection(string connectionString)
        {
            SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            string connString = connectionStringBuilder.ConnectionString;
            SqlConnection connection = new SqlConnection(connString);
            return connection;
        }


        static List<string> GetColumnNames(string connectionString, string tableName)
        {
            SqlConnection connection = GetSqlConnection(connectionString);

            string selectQuery = string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", tableName);

            SqlCommand command = new SqlCommand(selectQuery, connection);

            connection.Open();

            SqlDataReader reader = command.ExecuteReader();

            List<string> columnNames = new List<string>();
            while (reader.Read())
            {
                columnNames.Add(reader["COLUMN_NAME"].ToString());
            }

            connection.Close();

            return columnNames;
        }

        static bool Add_Integer_ColumnToTable(string connectionString, string tableName, string nameKey)
        {
            try
            {
                SqlConnection connection = GetSqlConnection(connectionString);

                string alterTableQuery = string.Format("ALTER TABLE {0} ADD {1} {2}", tableName, nameKey, "INT");

                SqlCommand command = new SqlCommand(alterTableQuery, connection);

                connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
                return true;
            }
            catch
            {

                return false;
            }
            

        }

        static bool Add_Text_ColumnToTable(string connectionString, string tableName, string nameKey)
        {
            try
            {
                SqlConnection connection = GetSqlConnection(connectionString);

                string alterTableQuery = string.Format("ALTER TABLE {0} ADD {1} {2}", tableName, nameKey, "VARCHAR(150)");

                SqlCommand command = new SqlCommand(alterTableQuery, connection);

                connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
                Console.WriteLine("Column Added!");
                return true;
            }
            catch
            {

                return false;
            }


        }

        static bool Add_RealNumber_ColumnToTable(string connectionString, string tableName, string nameKey)
        {
            try
            {
                SqlConnection connection = GetSqlConnection(connectionString);

                string alterTableQuery = string.Format("ALTER TABLE {0} ADD {1} {2}", tableName, nameKey, "REAL");

                SqlCommand command = new SqlCommand(alterTableQuery, connection);

                connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
                return true;
            }
            catch
            {

                return false;
            }


        }

        // Add column for big data
        static bool AddBlobColumnToTable(string connectionString, string tableName, string nameKey)
        {
            
            try
            {

                SqlConnection connection = GetSqlConnection(connectionString);

                string alterTableQuery = string.Format("ALTER TABLE {0} ADD {1} VARBINARY(MAX)", tableName, nameKey);

                SqlCommand command = new SqlCommand(alterTableQuery, connection);

                connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
                    return true;
            }
            catch
            {

                return false;
            }

        }

        static bool ReadAllItems(string connectionString, string tableName)
        {
            try
            {


                List<string> columns = GetColumnNames(connectionString, tableName);
                

                SqlConnection connection = GetSqlConnection(connectionString);

                string query = string.Format("SELECT * FROM {0}", tableName);

                SqlCommand command = new SqlCommand(query, connection);

                //command.Parameters.AddWithValue("@" + columnKey, itemColumnValue);

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                Console.WriteLine("Reading Data...\n");
                
                foreach (var item in columns)
                {
                    Console.Write(" | " + item);
                }

                while (reader.Read())
                {
                    string data = "\n";
                    foreach (var item in columns)
                    {
                        data += string.Format(" | {0}", reader[item]);
                    }

                    Console.Write(data);
                }

                Console.WriteLine("");

                WriteInstructions();

                connection.Close();
                return true;
            }
            catch
            {

                return false;
            }


        }

        static string ReadItem(string connectionString, string tableName, string columnKey, int idValue)
        {
            try {

                Console.WriteLine("Reading Data...\n");

                List<string> columns = GetColumnNames(connectionString, tableName);
                foreach (var item in columns)
                {
                    Console.Write(" | " + item);
                }
                Console.WriteLine("");

                SqlConnection connection = GetSqlConnection(connectionString);

                string query = string.Format("SELECT * FROM {0} WHERE {1} = @{1}", tableName, columnKey);

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@" + columnKey, idValue);

                connection.Open();

                string data = "";
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    
                    foreach (var item in columns)
                    {
                        data += string.Format(" | {0}", reader[item]);
                    }

                    Console.WriteLine(data);
                }

                connection.Close();
                return data;
            }
            catch
            {

                return "";
            }


        }


        static string ReadItemDetail(string connectionString, string tableName, string columnKey, int IdValue)
        {
            try
            {                
                SqlConnection connection = GetSqlConnection(connectionString);

                string query = string.Format("SELECT {0} FROM {1} WHERE Id = @IdValue", columnKey, tableName, IdValue);

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdValue", IdValue);

                connection.Open();

                object result = command.ExecuteScalar();

                connection.Close();

                string columnValue = "";

                if (result != null && result != DBNull.Value)
                {
                    columnValue = result.ToString();
                }
                else
                {
                    columnValue = "Item not found or column value is NULL. Please Check Spelling and Case_SeNsiViTy! and Space_Under_Lines";
                }
                
                return columnValue;
            }
            catch
            {

                return "Data Not Found in Data Base. Please Check Spelling and CaseSeNsiViTy!";
            }


        }

        static string Read_Blob_ItemDetail(string connectionString, string tableName, string columnKey, int IdValue)
        {
            try
            {
                SqlConnection connection = GetSqlConnection(connectionString);

                string query = string.Format("SELECT {0} FROM {1} WHERE Id = @IdValue", columnKey, tableName, IdValue);

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdValue", IdValue);

                connection.Open();

                byte[] result = (byte[]) command.ExecuteScalar();

                connection.Close();

                string columnValue = "";

                if (result != null && result.Length > 0)
                {
                    columnValue = Convert.ToBase64String(result);
                }
                else
                {
                    columnValue = "Item not found or column value is NULL. Please Check Spelling and Case_SeNsiViTy! and Space_Under_Lines";
                }

                return columnValue;
            }
            catch
            {

                return "Data Not Found in Data Base. Please Check Spelling and CaseSeNsiViTy!";
            }

        }

        static void InsertItem(string connectionString, string tableName, string idKey, string nameKey, int idValue,string keyValue)
        {

            SqlConnection connection = GetSqlConnection(connectionString);

            string query = string.Format("INSERT INTO {0} ({1}, {2}) VALUES (@{1}, @{2})", tableName, idKey, nameKey);

            Console.WriteLine(query);
            //Console.ReadKey();
            SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@" + idKey, idValue);
            command.Parameters.AddWithValue("@" + nameKey, keyValue);

            connection.Open();
            Console.WriteLine("open");
            command.ExecuteNonQuery();
            Console.WriteLine("wrote");
            connection.Close();
            Console.WriteLine("done");

        }

        static bool Modify_Text_Or_Number_Item(string connectionString, string tableName, string idKey, string nameKey, int idValue, object keyValue)
        {
            try
            {
                SqlConnection connection = GetSqlConnection(connectionString);

                string query = string.Format("UPDATE {0} SET {1} = @NewItem WHERE {2} = @{2}", tableName, nameKey, idKey);

                //Console.WriteLine(query);
                //Console.ReadKey();
                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@" + idKey, idValue);
                command.Parameters.AddWithValue("@NewItem", keyValue.ToString());

                connection.Open();
                Console.WriteLine("open");
                command.ExecuteNonQuery();
                Console.WriteLine("wrote");
                connection.Close();
                Console.WriteLine("done");
                return true;

            }
            catch 
            {

                return false;
            }
            

        }

        static bool Modify_Blob_Item(string connectionString, string tableName, string idKey, string nameKey, int idValue, string imagePath)
        {
            try
            {
                SqlConnection connection = GetSqlConnection(connectionString);

                string imageString = ConvertImageToBase64(imagePath);

                string query = string.Format("UPDATE {0} SET {1} = @NewItem WHERE {2} = @{2}", tableName, nameKey, idKey);

                //Console.WriteLine(query);
                //Console.ReadKey();
                SqlCommand command = new SqlCommand(query, connection);

                byte[] byteArrayData = Convert.FromBase64String(imageString);

                command.Parameters.AddWithValue("@" + idKey, idValue);
                command.Parameters.AddWithValue("@NewItem", byteArrayData);

                connection.Open();
                Console.WriteLine("open");
                command.ExecuteNonQuery();
                Console.WriteLine("wrote");
                connection.Close();
                Console.WriteLine("done");
                return true;
            }
            catch 
            {

                return false;
            }
            

        }

        static string ConvertImageToBase64(string imagePath)
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64String = Convert.ToBase64String(imageBytes);
            return base64String;
        }


        static void DeleteItem(string connectionString, string tableName, string idKey, int idValue)
        {
            SqlConnection connection = GetSqlConnection(connectionString);

            string query = string.Format("DELETE FROM {0} WHERE {1} = @{1}", tableName, idKey, idValue);

            Console.WriteLine(query);
            //Console.ReadKey();
            SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@"+idKey, idValue);

            connection.Open();
            Console.WriteLine("open");
            command.ExecuteNonQuery();
            Console.WriteLine("wrote");
            connection.Close();
            Console.WriteLine("done");
        }
        static void AddTable(string connectionString, string tableName, string itemInfo1)
        {
            SqlConnection connection = GetSqlConnection(connectionString);

            string query = string.Format("CREATE TABLE {0} (Id INT PRIMARY KEY, {1} VARCHAR(150))",tableName, itemInfo1);

            SqlCommand command = new SqlCommand(query, connection);

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
            Console.WriteLine("Table Made!");

        }
    }
}
