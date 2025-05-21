using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Controllers
{
    [RoutePrefix("api/order")]
    public class OrderController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // Lấy danh sách đơn hàng
        [HttpGet]
        [Route("getAll")]
        public IHttpActionResult GetAllOrders()
        {
            string query = "SELECT * FROM orders";
            DataTable dt = db.ExecuteQuery(query);
            List<Order> orders = new List<Order>();

            foreach (DataRow row in dt.Rows)
            {
                orders.Add(new Order
                {
                    Id = int.Parse(row["id"].ToString()),
                    TableId = int.Parse(row["table_id"].ToString()),
                    UserId = int.Parse(row["user_id"].ToString()),
                    TotalPrice = decimal.Parse(row["total_price"].ToString()),
                    TotalGuest = row["total_guest"] != DBNull.Value ? int.Parse(row["total_guest"].ToString()) : 1,
                    Status = row["status"].ToString(),
                    StartTime = DateTime.Parse(row["start_time"].ToString()),
                    EndTime = row["end_time"] != DBNull.Value ? (DateTime?)DateTime.Parse(row["end_time"].ToString()) : null,
                    CreatedAt = DateTime.Parse(row["created_at"].ToString())
                });
            }

            return Ok(orders);
        }


        // Thêm đơn hàng mới
        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddOrder([FromBody] Order order)
        {
            try
            {
                // Thêm đơn hàng
                string insertQuery = $"INSERT INTO orders (table_id, user_id, total_price, total_guest, status, start_time) " +
                                    $"VALUES ({order.TableId}, {order.UserId}, {order.TotalPrice}, {order.TotalGuest}, '{order.Status}', '{order.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}')";

                int rowsAffected = db.ExecuteNonQuery(insertQuery);

                if (rowsAffected > 0)
                {
                    // Sử dụng LAST_INSERT_ID() của MySQL để lấy ID vừa được tạo
                    string getIdQuery = "SELECT LAST_INSERT_ID()";
                    object result = db.ExecuteScalar(getIdQuery);

                    if (result != null && result != DBNull.Value)
                    {
                        int newOrderId = Convert.ToInt32(result);
                        return Ok(newOrderId);
                    }
                }

                return BadRequest("Không thể thêm đơn hàng");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Cập nhật đơn hàng
        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateOrder([FromBody] Order order)
        {
            string query = $"UPDATE orders SET table_id = {order.TableId}, total_price = {order.TotalPrice}, status = '{order.Status}' WHERE id = {order.Id}";
            int result = db.ExecuteNonQuery(query);

            if (result > 0)
                return Ok("Order updated successfully");
            else
                return BadRequest("Failed to update order");
        }

        // Xóa đơn hàng
        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult DeleteOrder(int id)
        {
            string query = $"DELETE FROM orders WHERE id = {id}";
            db.ExecuteQuery(query);
            return Ok("Order deleted successfully");
        }


        [HttpPut]
        [Route("moveTable")]
        public IHttpActionResult MoveTable([FromBody] MoveTableRequest request)
        {
            try
            {
                // Cập nhật table_id của đơn hàng
                string query = $"UPDATE orders SET table_id = {request.TargetTableId} WHERE id = {request.OrderId}";
                int result = db.ExecuteNonQuery(query);

                if (result > 0)
                {
                    return Ok("Chuyển bàn thành công");
                }
                else
                {
                    return BadRequest("Không thể chuyển bàn");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Class request
        public class MoveTableRequest
        {
            public int OrderId { get; set; }
            public int SourceTableId { get; set; }
            public int TargetTableId { get; set; }
            public bool IsMerge { get; set; } // Đánh dấu có phải gộp bàn không
        }
        [HttpPost]
        [Route("mergeOrders")]
        public IHttpActionResult MergeOrders([FromBody] MergeOrdersRequest request)
        {
            try
            {
                // 1. Lấy thông tin chi tiết của đơn hàng nguồn
                string getSourceDetailsQuery = $"SELECT * FROM order_details WHERE order_id = {request.SourceOrderId}";
                DataTable sourceDetailsDt = db.ExecuteQuery(getSourceDetailsQuery);

                // 2. Lấy thông tin chi tiết của đơn hàng đích
                string getTargetDetailsQuery = $"SELECT * FROM order_details WHERE order_id = {request.TargetOrderId}";
                DataTable targetDetailsDt = db.ExecuteQuery(getTargetDetailsQuery);

                // 3. Lấy thông tin đơn hàng nguồn và đích
                string getSourceOrderQuery = $"SELECT * FROM orders WHERE id = {request.SourceOrderId}";
                DataTable sourceOrderDt = db.ExecuteQuery(getSourceOrderQuery);

                string getTargetOrderQuery = $"SELECT * FROM orders WHERE id = {request.TargetOrderId}";
                DataTable targetOrderDt = db.ExecuteQuery(getTargetOrderQuery);

                if (sourceOrderDt.Rows.Count == 0 || targetOrderDt.Rows.Count == 0)
                {
                    return BadRequest("Không tìm thấy đơn hàng nguồn hoặc đích");
                }

                // 4. Cập nhật tổng số khách và tổng tiền của đơn hàng đích
                int sourceGuests = int.Parse(sourceOrderDt.Rows[0]["total_guest"].ToString());
                int targetGuests = int.Parse(targetOrderDt.Rows[0]["total_guest"].ToString());
                decimal sourceTotal = decimal.Parse(sourceOrderDt.Rows[0]["total_price"].ToString());
                decimal targetTotal = decimal.Parse(targetOrderDt.Rows[0]["total_price"].ToString());

                string updateTargetQuery = $"UPDATE orders SET total_guest = {sourceGuests + targetGuests}, " +
                                          $"total_price = {sourceTotal + targetTotal} " +
                                          $"WHERE id = {request.TargetOrderId}";
                db.ExecuteNonQuery(updateTargetQuery);

                // 5. Chuyển tất cả orderdetails từ đơn hàng nguồn sang đơn hàng đích
                foreach (DataRow row in sourceDetailsDt.Rows)
                {
                    int menuId = int.Parse(row["menu_id"].ToString());
                    int quantity = int.Parse(row["quantity"].ToString());
                    decimal subtotal = decimal.Parse(row["subtotal"].ToString());

                    // Kiểm tra xem món này đã có trong đơn hàng đích chưa
                    bool found = false;
                    foreach (DataRow targetRow in targetDetailsDt.Rows)
                    {
                        if (int.Parse(targetRow["menu_id"].ToString()) == menuId)
                        {
                            // Cập nhật số lượng và subtotal nếu đã có
                            int targetQuantity = int.Parse(targetRow["quantity"].ToString());
                            decimal targetSubtotal = decimal.Parse(targetRow["subtotal"].ToString());

                            string updateDetailQuery = $"UPDATE order_details SET quantity = {quantity + targetQuantity}, " +
                                                     $"subtotal = {subtotal + targetSubtotal} " +
                                                     $"WHERE id = {targetRow["id"]}";
                            db.ExecuteNonQuery(updateDetailQuery);

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Thêm mới món vào đơn hàng đích nếu chưa có
                        string insertDetailQuery = $"INSERT INTO order_details (order_id, menu_id, quantity, subtotal) " +
                                                 $"VALUES ({request.TargetOrderId}, {menuId}, {quantity}, {subtotal})";
                        db.ExecuteNonQuery(insertDetailQuery);
                    }
                }

                // 6. Xóa đơn hàng nguồn và chi tiết đơn hàng nguồn
                string deleteSourceDetailsQuery = $"DELETE FROM order_details WHERE order_id = {request.SourceOrderId}";
                db.ExecuteNonQuery(deleteSourceDetailsQuery);

                string deleteSourceOrderQuery = $"DELETE FROM orders WHERE id = {request.SourceOrderId}";
                db.ExecuteNonQuery(deleteSourceOrderQuery);

                return Ok("Gộp đơn hàng thành công");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
       
        // Class request
        public class MergeOrdersRequest
        {
            public int SourceOrderId { get; set; }
            public int TargetOrderId { get; set; }
            public int SourceTableId { get; set; }
            public int TargetTableId { get; set; }
        }
    }
}
