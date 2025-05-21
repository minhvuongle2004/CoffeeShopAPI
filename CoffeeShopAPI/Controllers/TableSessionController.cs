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
    public class TableSessionController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // Lấy danh sách tất cả phiên bàn
        [HttpGet]
        [Route("api/tablesession/getAll")]
        public IHttpActionResult GetAllSessions()
        {
            string query = "SELECT * FROM table_sessions";
            DataTable dt = db.ExecuteQuery(query);
            List<TableSession> sessions = new List<TableSession>();

            foreach (DataRow row in dt.Rows)
            {
                sessions.Add(new TableSession
                {
                    Id = Convert.ToInt32(row["id"]),
                    TableId = Convert.ToInt32(row["table_id"]),
                    UserId = Convert.ToInt32(row["user_id"]),
                    StartTime = Convert.ToDateTime(row["start_time"]),
                    EndTime = row["end_time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["end_time"]),
                    Status = row["status"].ToString()
                });
            }

            return Ok(sessions);
        }

        // Lấy phiên bàn theo ID
        [HttpGet]
        [Route("api/tablesession/getById/{id}")]
        public IHttpActionResult GetSessionById(int id)
        {
            string query = "SELECT * FROM table_sessions WHERE id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };
            DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            DataRow row = dt.Rows[0];
            TableSession session = new TableSession
            {
                Id = Convert.ToInt32(row["id"]),
                TableId = Convert.ToInt32(row["table_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                StartTime = Convert.ToDateTime(row["start_time"]),
                EndTime = row["end_time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["end_time"]),
                Status = row["status"].ToString()
            };

            return Ok(session);
        }

        // Tạo phiên bàn mới
        [HttpPost]
        [Route("api/tablesession/add")]
        public IHttpActionResult CreateSession([FromBody] TableSession session)
        {
            if (session == null || session.TableId <= 0 || session.UserId <= 0 ||
                !(session.Status == "active" || session.Status == "completed"))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            string query = "INSERT INTO table_sessions (table_id, user_id, start_time, status) VALUES (@tableId, @userId, NOW(), @status)";
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableId", session.TableId),
                new MySqlParameter("@userId", session.UserId),
                new MySqlParameter("@status", session.Status)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Tạo phiên bàn thành công.");
            else
                return BadRequest("Không thể tạo phiên bàn.");
        }

        // Cập nhật phiên bàn
        [HttpPut]
        [Route("api/tablesession/update/{id}")]
        public IHttpActionResult UpdateSession(int id, [FromBody] TableSession session)
        {
            if (session == null || session.TableId <= 0 || session.UserId <= 0 ||
                !(session.Status == "active" || session.Status == "completed"))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            string query = "UPDATE table_sessions SET table_id = @tableId, user_id = @userId, end_time = @endTime, status = @status WHERE id = @id";
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableId", session.TableId),
                new MySqlParameter("@userId", session.UserId),
                new MySqlParameter("@endTime", session.EndTime ?? (object)DBNull.Value),
                new MySqlParameter("@status", session.Status),
                new MySqlParameter("@id", id)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Cập nhật phiên bàn thành công.");
            else
                return NotFound();
        }

        // Xóa phiên bàn theo ID
        [HttpDelete]
        [Route("api/tablesession/delete/{id}")]
        public IHttpActionResult DeleteSession(int id)
        {
            string query = "DELETE FROM table_sessions WHERE id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Xóa phiên bàn thành công.");
            else
                return NotFound();
        }
    }
}
