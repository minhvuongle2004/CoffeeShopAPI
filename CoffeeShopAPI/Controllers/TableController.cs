using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MySql.Data.MySqlClient;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Controllers
{
    public class TableController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // Lấy danh sách tất cả bàn
        [HttpGet]
        [Route("api/table/getAll")]
        public IHttpActionResult GetTables()
        {
            string query = "SELECT * FROM tables";
            DataTable dt = db.ExecuteQuery(query);
            List<Table> tables = new List<Table>();

            foreach (DataRow row in dt.Rows)
            {
                tables.Add(new Table
                {
                    Id = Convert.ToInt32(row["id"]),
                    TableName = row["table_name"].ToString(),
                    Status = row["status"].ToString()
                });
            }

            return Ok(tables);
        }

        // Lấy bàn theo ID
        [HttpGet]
        [Route("api/table/getById/{id}")]
        public IHttpActionResult GetTableById(int id)
        {
            string query = "SELECT * FROM tables WHERE id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };
            DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            DataRow row = dt.Rows[0];
            Table table = new Table
            {
                Id = Convert.ToInt32(row["id"]),
                TableName = row["table_name"].ToString(),
                Status = row["status"].ToString()
            };

            return Ok(table);
        }

        // Thêm bàn mới
        [HttpPost]
        [Route("api/table/add")]
        public IHttpActionResult CreateTable([FromBody] Table table)
        {
            if (table == null || string.IsNullOrEmpty(table.TableName) ||
                !(table.Status == "full" || table.Status == "empty"))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            string query = "INSERT INTO tables (table_name, status) VALUES (@tableName, @status)";
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableName", table.TableName),
                new MySqlParameter("@status", table.Status)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Thêm bàn thành công.");
            else
                return BadRequest("Không thể thêm bàn.");
        }

        // Cập nhật thông tin bàn
        [HttpPut]
        [Route("api/table/update/{id}")]
        public IHttpActionResult UpdateTable(int id, [FromBody] Table table)
        {
            if (table == null || string.IsNullOrEmpty(table.TableName) ||
                !(table.Status == "full" || table.Status == "empty"))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            string query = "UPDATE tables SET table_name = @tableName, status = @status WHERE id = @id";
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableName", table.TableName),
                new MySqlParameter("@status", table.Status),
                new MySqlParameter("@id", id)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Cập nhật bàn thành công.");
            else
                return NotFound();
        }

        // Xóa bàn theo ID
        [HttpDelete]
        [Route("api/table/delete/{id}")]
        public IHttpActionResult DeleteTable(int id)
        {
            string query = "DELETE FROM tables WHERE id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Xóa bàn thành công.");
            else
                return NotFound();
        }
    }
}
