using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace CoffeeShopAPI.Utils
{
    public class DbHelper
    {
        private static string connectionString = "server=localhost;database=coffee_shop;user=root;password=vuong642004";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
  