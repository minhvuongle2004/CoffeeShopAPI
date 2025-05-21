using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Controllers
{
    [RoutePrefix("api/inventory")]
    public class InventoryController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        [HttpGet]
        [Route("getAll")]
        public IHttpActionResult GetAllInventory()
        {
            string query = "SELECT * FROM inventory";
            DataTable dt = db.ExecuteQuery(query);
            List<Inventory> inventoryList = new List<Inventory>();

            foreach (DataRow row in dt.Rows)
            {
                inventoryList.Add(new Inventory
                {
                    Id = int.Parse(row["id"].ToString()),
                    Name = row["name"].ToString(),
                    Stock = int.Parse(row["stock"].ToString()),
                    Unit = row["unit"].ToString(),
                    UpdatedAt = row["updated_at"] == DBNull.Value ? DateTime.Now : DateTime.Parse(row["updated_at"].ToString())
                });
            }
            return Ok(inventoryList);
        }

        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddInventory([FromBody] Inventory inventory)
        {
            string query = $"INSERT INTO inventory (name, stock, unit, updated_at) VALUES ('{inventory.Name}', {inventory.Stock}, '{inventory.Unit}', NOW())";
            db.ExecuteQuery(query);
            return Ok("Inventory item added successfully");
        }

        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateInventory([FromBody] Inventory inventory)
        {
            string query = $"UPDATE inventory SET name = '{inventory.Name}', stock = {inventory.Stock}, unit = '{inventory.Unit}', updated_at = NOW() WHERE id = {inventory.Id}";
            db.ExecuteQuery(query);
            return Ok("Inventory item updated successfully");
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult DeleteInventory(int id)
        {
            string query = $"DELETE FROM inventory WHERE id = {id}";
            db.ExecuteQuery(query);
            return Ok("Inventory item deleted successfully");
        }
    }
}
