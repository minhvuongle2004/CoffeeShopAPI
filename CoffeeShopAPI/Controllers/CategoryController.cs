using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Controllers
{
    [RoutePrefix("api/category")]
    public class CategoryController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        [HttpGet]
        [Route("getAll")]
        public IHttpActionResult GetAllCategories()
        {
            string query = "SELECT * FROM category";
            DataTable dt = db.ExecuteQuery(query);
            List<Category> categories = new List<Category>();

            foreach (DataRow row in dt.Rows)
            {
                categories.Add(new Category
                {
                    Id = int.Parse(row["id"].ToString()),
                    Name = row["name"].ToString()
                });
            }
            return Ok(categories);
        }

        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddCategory([FromBody] Category category)
        {
            string query = $"INSERT INTO category (name) VALUES ('{category.Name}')";
            db.ExecuteQuery(query);
            return Ok("Category added successfully");
        }

        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateCategory([FromBody] Category category)
        {
            string query = $"UPDATE category SET name = '{category.Name}' WHERE id = {category.Id}";
            db.ExecuteQuery(query);
            return Ok("Category updated successfully");
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult DeleteCategory(int id)
        {
            string query = $"DELETE FROM category WHERE id = {id}";
            db.ExecuteQuery(query);
            return Ok("Category deleted successfully");
        }
    }
}
