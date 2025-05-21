using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Controllers
{
    [RoutePrefix("api/orderDetail")]
    public class OrderDetailController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // Lấy danh sách chi tiết đơn hàng theo OrderId
        [HttpGet]
        [Route("getByOrder/{orderId}")]
        public IHttpActionResult GetOrderDetailsByOrderId(int orderId)
        {
            string query = $"SELECT * FROM order_details WHERE order_id = {orderId}";
            DataTable dt = db.ExecuteQuery(query);
            List<OrderDetail> orderDetails = new List<OrderDetail>();

            foreach (DataRow row in dt.Rows)
            {
                orderDetails.Add(new OrderDetail
                {
                    Id = int.Parse(row["id"].ToString()),
                    OrderId = int.Parse(row["order_id"].ToString()),
                    MenuId = int.Parse(row["menu_id"].ToString()),
                    Quantity = int.Parse(row["quantity"].ToString()),
                    Subtotal = decimal.Parse(row["subtotal"].ToString())
                });
            }
            return Ok(orderDetails);
        }

        // Thêm chi tiết đơn hàng
        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddOrderDetail([FromBody] OrderDetail orderDetail)
        {
            try
            {
                // Hiển thị thông tin debug
                Console.WriteLine($"Adding order detail: OrderId={orderDetail.OrderId}, MenuId={orderDetail.MenuId}, Quantity={orderDetail.Quantity}, Subtotal={orderDetail.Subtotal}");

                string query = $"INSERT INTO order_details (order_id, menu_id, quantity, subtotal) VALUES " +
                              $"({orderDetail.OrderId}, {orderDetail.MenuId}, {orderDetail.Quantity}, {orderDetail.Subtotal})";

                int rowsAffected = db.ExecuteNonQuery(query);

                if (rowsAffected > 0)
                {
                    // Lấy ID chi tiết đơn hàng vừa thêm
                    string getIdQuery = "SELECT LAST_INSERT_ID()";
                    object result = db.ExecuteScalar(getIdQuery);

                    if (result != null && result != DBNull.Value)
                    {
                        int newDetailId = Convert.ToInt32(result);
                        return Ok(newDetailId);
                    }
                }

                return BadRequest("Không thể thêm chi tiết đơn hàng");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddOrderDetail: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // Cập nhật chi tiết đơn hàng
        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateOrderDetail([FromBody] OrderDetail orderDetail)
        {
            if (orderDetail == null || orderDetail.Id <= 0 || orderDetail.Quantity <= 0)
            {
                return BadRequest("Dữ liệu không hợp lệ! Vui lòng kiểm tra lại.");
            }

            string query = $"UPDATE order_details SET quantity = {orderDetail.Quantity}, subtotal = {orderDetail.Subtotal} WHERE id = {orderDetail.Id}";
            int rowsAffected = db.ExecuteNonQuery(query);

            if (rowsAffected > 0)
            {
                return Ok("Order detail updated successfully");
            }
            else
            {
                return BadRequest("Cập nhật chi tiết đơn hàng thất bại, có thể có lỗi với database!");
            }
        }

        // Xóa chi tiết đơn hàng
        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult DeleteOrderDetail(int id)
        {
            string query = $"DELETE FROM order_details WHERE id = {id}";
            int rowsAffected = db.ExecuteNonQuery(query);

            if (rowsAffected > 0)
            {
                return Ok("Order detail deleted successfully");
            }
            else
            {
                return BadRequest("Xóa chi tiết đơn hàng thất bại, có thể có lỗi với database!");
            }
        }

        // Xóa tất cả chi tiết đơn hàng theo OrderId - THÊM MỚI
        [HttpDelete]
        [Route("deleteByOrderId/{orderId}")]
        public IHttpActionResult DeleteOrderDetailsByOrderId(int orderId)
        {
            try
            {
                string query = $"DELETE FROM order_details WHERE order_id = {orderId}";
                int rowsAffected = db.ExecuteNonQuery(query);

                // Kiểm tra số lượng bản ghi đã bị xóa để trả về thông báo phù hợp
                if (rowsAffected > 0)
                {
                    return Ok($"Đã xóa {rowsAffected} chi tiết đơn hàng của đơn hàng #{orderId}");
                }
                else
                {
                    return Ok($"Không có chi tiết đơn hàng nào để xóa hoặc đơn hàng #{orderId} không tồn tại");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteOrderDetailsByOrderId: {ex.Message}");
                return InternalServerError(ex);
            }
        }
    }
}