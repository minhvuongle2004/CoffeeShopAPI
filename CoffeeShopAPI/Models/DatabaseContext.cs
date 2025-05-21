using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace CoffeeShopAPI.Models
{
    public class DatabaseContext
    {
        private MySqlConnection connection;

        public DatabaseContext()
        {
            connection = Utils.DbHelper.GetConnection();
        }

        public DataTable ExecuteQuery(string query)
        {
            DataTable dt = new DataTable();
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return dt;
        }

        public int ExecuteNonQuery(string query)
        {
            int rowsAffected = 0;
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                rowsAffected = cmd.ExecuteNonQuery(); // Trả về số dòng bị ảnh hưởng
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message); // In lỗi chi tiết ra Console
            }
            finally
            {
                connection.Close();
            }
            return rowsAffected;
        }

        public object ExecuteScalar(string query)
        {
            object result = null;
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                result = cmd.ExecuteScalar(); // Trả về giá trị duy nhất
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message); // In lỗi chi tiết ra Console
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        // Thêm các phương thức tham số hóa mới
        public DataTable ExecuteParameterizedQuery(string query, MySqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return dt;
        }

        public int ExecuteNonQueryParameterized(string query, MySqlParameter[] parameters)
        {
            int rowsAffected = 0;
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return rowsAffected;
        }

        public object ExecuteScalarParameterized(string query, MySqlParameter[] parameters)
        {
            object result = null;
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                result = cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return result;
        }
    }
}