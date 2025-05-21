using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Controllers
{
    [RoutePrefix("api/menu")]
    public class MenuController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // Lấy danh sách menu
        [HttpGet]
        [Route("getAll")]
        public IHttpActionResult GetAllMenus()
        {
            string query = "SELECT * FROM menu";
            DataTable dt = db.ExecuteQuery(query);
            List<Menu> menus = new List<Menu>();

            foreach (DataRow row in dt.Rows)
            {
                menus.Add(new Menu
                {
                    Id = int.Parse(row["id"].ToString()),
                    Name = row["name"].ToString(),
                    CategoryId = int.Parse(row["category_id"].ToString()),
                    Price = decimal.Parse(row["price"].ToString()),
                    Image = row["image"]?.ToString()
                });
            }
            return Ok(menus);
        }

        // Thêm món mới
        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddMenu([FromBody] Menu menu)
        {
            if (menu == null || menu.CategoryId <= 0)
                return BadRequest("Dữ liệu không hợp lệ!");

            string query = $@"
                INSERT INTO menu (name, category_id, price, image)
                VALUES (N'{menu.Name}', {menu.CategoryId}, {menu.Price},             
                        {(string.IsNullOrEmpty(menu.Image) ? "NULL" : $"'{menu.Image}'")})";

            int rowsAffected = db.ExecuteNonQuery(query);
            if (rowsAffected > 0)
                return Ok("Thêm thành công");
            else
                return BadRequest("Thêm thất bại");

        }

        // Cập nhật món
        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateMenu([FromBody] Menu menu)
        {
            if (menu == null || menu.Id <= 0) return BadRequest("Dữ liệu không hợp lệ!");

            string query = $@"
                UPDATE menu SET 
                    name = N'{menu.Name}', 
                    category_id = {menu.CategoryId},  
                    price = {menu.Price},
                    image = {(string.IsNullOrEmpty(menu.Image) ? "NULL" : $"'{menu.Image}'")}
                WHERE id = {menu.Id}";

            int rowsAffected = db.ExecuteNonQuery(query);
            if (rowsAffected > 0)
                return Ok("Cập nhật thành công");
            else
                return BadRequest("Cập nhật thất bại");
        }

        // Xóa món
        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult DeleteMenu(int id)
        {
            string query = $"DELETE FROM menu WHERE id = {id}";  
            int rowsAffected = db.ExecuteNonQuery(query);
            if (rowsAffected > 0)
                return Ok("Xóa thành công");
            else
                return BadRequest("Xóa thất bại");
        }
        // Lọc menu theo category_id
        [HttpGet]
        [Route("byCategory/{id}")]
        public IHttpActionResult GetMenuByCategory(int id)
        {
            string query = $"SELECT * FROM menu WHERE category_id = {id}";
            DataTable dt = db.ExecuteQuery(query);
            List<Menu> menus = new List<Menu>();

            foreach (DataRow row in dt.Rows)
            {
                menus.Add(new Menu
                {
                    Id = int.Parse(row["id"].ToString()),
                    Name = row["name"].ToString(),
                    CategoryId = int.Parse(row["category_id"].ToString()),
                    Price = decimal.Parse(row["price"].ToString()),
                    Image = row["image"]?.ToString()
                });
            }

            return Ok(menus);
        }
    }
}
