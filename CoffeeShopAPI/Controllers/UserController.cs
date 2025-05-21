using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MySql.Data.MySqlClient;
using CoffeeShopAPI.Models;
using Org.BouncyCastle.Crypto.Generators;

namespace CoffeeShopAPI.Controllers
{
    public class UserController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // Lấy danh sách tất cả người dùng
        [HttpGet]
        [Route("api/user/getAll")]
        public IHttpActionResult GetAllUsers()
        {
            string query = "SELECT id, fullname, username, role, phone FROM users";
            DataTable dt = db.ExecuteQuery(query);
            List<User> users = new List<User>();

            foreach (DataRow row in dt.Rows)
            {
                users.Add(new User
                {
                    Id = Convert.ToInt32(row["id"]),
                    Fullname = row["fullname"].ToString(),
                    Username = row["username"].ToString(),
                    Role = row["role"].ToString(),
                    Phone = row["phone"] == DBNull.Value ? null : row["phone"].ToString()
                });
            }

            return Ok(users);
        }

        // Lấy thông tin người dùng theo ID
        [HttpGet]
        [Route("api/user/getById/{id}")]
        public IHttpActionResult GetUserById(int id)
        {
            string query = "SELECT id, fullname, username, role, phone FROM users WHERE id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };
            DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            DataRow row = dt.Rows[0];
            User user = new User
            {
                Id = Convert.ToInt32(row["id"]),
                Fullname = row["fullname"].ToString(),
                Username = row["username"].ToString(),
                Role = row["role"].ToString(),
                Phone = row["phone"] == DBNull.Value ? null : row["phone"].ToString()
            };

            return Ok(user);
        }
        // Đăng ký tài khoản mới
        [HttpPost]
        [Route("api/user/register")]
        public IHttpActionResult RegisterUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Fullname) || string.IsNullOrEmpty(user.Username) ||
                string.IsNullOrEmpty(user.Password) || !(user.Role == "admin" || user.Role == "cashier" || user.Role == "staff"))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            // Kiểm tra username đã tồn tại chưa
            string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
            MySqlParameter[] checkParams = { new MySqlParameter("@username", user.Username) };
            int count = Convert.ToInt32(db.ExecuteScalarParameterized(checkQuery, checkParams));

            if (count > 0)
                return BadRequest("Tên đăng nhập đã tồn tại.");

            // Mã hóa mật khẩu (có thể thay bằng bcrypt)
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

            string query = "INSERT INTO users (fullname, username, password, role, phone) VALUES (@fullname, @username, @password, @role, @phone)";
            MySqlParameter[] parameters = {
                new MySqlParameter("@fullname", user.Fullname),
                new MySqlParameter("@username", user.Username),
                new MySqlParameter("@password", hashedPassword),
                new MySqlParameter("@role", user.Role),
                new MySqlParameter("@phone", user.Phone ?? (object)DBNull.Value)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Đăng ký thành công.");
            else
                return BadRequest("Không thể đăng ký.");
        }

        // Đăng nhập
        [HttpPost]
        [Route("api/user/login")]
        public IHttpActionResult LoginUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");

            string query = "SELECT id, fullname, username, password, role, phone FROM users WHERE username = @username";
            MySqlParameter[] parameters = { new MySqlParameter("@username", user.Username) };
            DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return Unauthorized(); // Không tìm thấy tài khoản

            DataRow row = dt.Rows[0];
            string hashedPassword = row["password"].ToString();

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(user.Password, hashedPassword))
                return Unauthorized(); // Sai mật khẩu

            User loggedInUser = new User
            {
                Id = Convert.ToInt32(row["id"]),
                Fullname = row["fullname"].ToString(),
                Username = row["username"].ToString(),
                Role = row["role"].ToString(),
                Phone = row["phone"] == DBNull.Value ? null : row["phone"].ToString()
            };

            return Ok(loggedInUser);
        }

        // Cập nhật thông tin người dùng
        [HttpPut]
        [Route("api/user/update/{id}")]
        public IHttpActionResult UpdateUser(int id, [FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Fullname) ||
                !(user.Role == "admin" || user.Role ==  "cashier" || user.Role == "staff"))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            string query = "UPDATE users SET fullname = @fullname, role = @role, phone = @phone WHERE id = @id";
            MySqlParameter[] parameters = {
                new MySqlParameter("@fullname", user.Fullname),
                new MySqlParameter("@role", user.Role),
                new MySqlParameter("@phone", user.Phone ?? (object)DBNull.Value),
                new MySqlParameter("@id", id)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Cập nhật thông tin thành công.");
            else
                return NotFound();
        }

        // Xóa tài khoản người dùng
        [HttpDelete]
        [Route("api/user/delete/{id}")]
        public IHttpActionResult DeleteUser(int id)
        {
            string query = "DELETE FROM users WHERE id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Xóa tài khoản thành công.");
            else
                return NotFound();
        }
    }
}
