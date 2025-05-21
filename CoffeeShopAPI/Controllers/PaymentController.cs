using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using CoffeeShopAPI.Models;
using MySql.Data.MySqlClient;

namespace CoffeeShopAPI.Controllers
{
    public class PaymentController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // GET api/payment
        [HttpGet]
        [Route("api/payment/getAll")]
        public IHttpActionResult GetAllPayments()
        {
            try
            {
                string query = "SELECT id, order_id, method, amount, paid_at FROM payments";
                DataTable dt = db.ExecuteQuery(query);

                List<Payment> payments = new List<Payment>();

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Payment payment = new Payment
                        {
                            Id = Convert.ToInt32(row["id"]),
                            OrderId = Convert.ToInt32(row["order_id"]),
                            Method = row["method"].ToString(),
                            Amount = Convert.ToDecimal(row["amount"]),
                            // Cẩn thận khi chuyển đổi TIMESTAMP từ MySQL sang DateTime
                            PaidAt = row["paid_at"] != DBNull.Value ?
                                DateTime.Parse(row["paid_at"].ToString()) :
                                DateTime.MinValue
                        };
                        payments.Add(payment);
                    }
                    catch (Exception ex)
                    {
                        // Sử dụng một logger thay vì Console.WriteLine
                        System.Diagnostics.Debug.WriteLine($"Error processing payment record: {ex.Message}");
                    }
                }

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving payments: " + ex.Message));
            }
        }

        // GET api/payment/5
        [HttpGet]
        [Route("api/payment/getByID/{id}")]
        public IHttpActionResult GetPayment(int id)
        {
            try
            {
                // Sử dụng tham số hóa truy vấn để tránh SQL injection
                string query = "SELECT id, order_id, method, amount, paid_at FROM payments WHERE id = @id";
                MySqlParameter[] parameters = {
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
                };

                DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    return NotFound();
                }

                DataRow row = dt.Rows[0];
                Payment payment = new Payment
                {
                    Id = Convert.ToInt32(row["id"]),
                    OrderId = Convert.ToInt32(row["order_id"]),
                    Method = row["method"].ToString(),
                    Amount = Convert.ToDecimal(row["amount"]),
                    PaidAt = row["paid_at"] != DBNull.Value ?
                        DateTime.Parse(row["paid_at"].ToString()) :
                        DateTime.MinValue
                };

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving payment: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("api/payment/byOrder/{orderId}")]
        public IHttpActionResult GetPaymentsByOrder(int orderId)
        {
            try
            {
                string query = "SELECT id, order_id, method, amount, paid_at FROM payments WHERE order_id = @orderId";
                MySqlParameter[] parameters = {
                    new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = orderId }
                };

                DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

                List<Payment> payments = new List<Payment>();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Payment payment = new Payment
                        {
                            Id = Convert.ToInt32(row["id"]),
                            OrderId = Convert.ToInt32(row["order_id"]),
                            Method = row["method"].ToString(),
                            Amount = Convert.ToDecimal(row["amount"]),
                            PaidAt = row["paid_at"] != DBNull.Value ?
                                DateTime.Parse(row["paid_at"].ToString()) :
                                DateTime.MinValue
                        };
                        payments.Add(payment);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing payment record: {ex.Message}");
                    }
                }

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving payments by order: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("api/payment/add")]
        public IHttpActionResult CreatePayment([FromBody] Payment payment)
        {
            try
            {
                if (payment == null)
                {
                    return BadRequest("Payment object is null");
                }

                if (string.IsNullOrEmpty(payment.Method) ||
                    (payment.Method != "cash" && payment.Method != "card" && payment.Method != "e-wallet"))
                {
                    return BadRequest("Invalid payment method. Allowed methods: cash, card, e-wallet");
                }

                if (payment.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }

                string checkOrderQuery = "SELECT id FROM orders WHERE id = @orderId";
                MySqlParameter[] checkOrderParams = {
                    new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = payment.OrderId }
                };

                object orderExists = db.ExecuteScalarParameterized(checkOrderQuery, checkOrderParams);
                if (orderExists == null)
                {
                    return BadRequest($"Order with ID {payment.OrderId} does not exist");
                }

                string insertQuery = @"INSERT INTO payments (order_id, method, amount, paid_at) 
                                      VALUES (@orderId, @method, @amount, NOW());
                                      SELECT LAST_INSERT_ID()";

                MySqlParameter[] insertParams = {
                    new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = payment.OrderId },
                    new MySqlParameter("@method", MySqlDbType.VarChar) { Value = payment.Method },
                    new MySqlParameter("@amount", MySqlDbType.Decimal) { Value = payment.Amount }
                };

                object newId = db.ExecuteScalarParameterized(insertQuery, insertParams);
                if (newId != null)
                {
                    payment.Id = Convert.ToInt32(newId);
                    payment.PaidAt = DateTime.Now;

                    string updateOrderQuery = "UPDATE orders SET status = 'paid', end_time = NOW() WHERE id = @orderId";
                    MySqlParameter[] updateOrderParams = {
                        new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = payment.OrderId }
                    };

                    db.ExecuteNonQueryParameterized(updateOrderQuery, updateOrderParams);

                    return Ok(payment);
                }
                else
                {
                    return InternalServerError(new Exception("Error creating payment: Unable to get new ID"));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error creating payment: " + ex.Message));
            }
        }

        [HttpPut]
        [Route("api/payment/update/{id}")]
        public IHttpActionResult UpdatePayment(int id, [FromBody] Payment payment)
        {
            try
            {
                if (payment == null)
                {
                    return BadRequest("Payment object is null");
                }

                if (id != payment.Id)
                {
                    return BadRequest("Payment ID mismatch");
                }

                if (string.IsNullOrEmpty(payment.Method) ||
                    (payment.Method != "cash" && payment.Method != "card" && payment.Method != "e-wallet"))
                {
                    return BadRequest("Invalid payment method. Allowed methods: cash, card, e-wallet");
                }

                if (payment.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }

                string checkQuery = "SELECT id FROM payments WHERE id = @id";
                MySqlParameter[] checkParams = {
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
                };

                object paymentExists = db.ExecuteScalarParameterized(checkQuery, checkParams);
                if (paymentExists == null)
                {
                    return NotFound();
                }

                string updateQuery = @"UPDATE payments 
                                      SET order_id = @orderId, 
                                          method = @method, 
                                          amount = @amount 
                                      WHERE id = @id";

                MySqlParameter[] updateParams = {
                    new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = payment.OrderId },
                    new MySqlParameter("@method", MySqlDbType.VarChar) { Value = payment.Method },
                    new MySqlParameter("@amount", MySqlDbType.Decimal) { Value = payment.Amount },
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
                };

                int rowsAffected = db.ExecuteNonQueryParameterized(updateQuery, updateParams);
                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error updating payment: " + ex.Message));
            }
        }

        [HttpDelete]
        [Route("api/payment/delete/{id}")]
        public IHttpActionResult DeletePayment(int id)
        {
            try
            {
                string checkQuery = "SELECT order_id FROM payments WHERE id = @id";
                MySqlParameter[] checkParams = {
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
                };

                object result = db.ExecuteScalarParameterized(checkQuery, checkParams);
                if (result == null)
                {
                    return NotFound();
                }

                int orderId = Convert.ToInt32(result);

                string deleteQuery = "DELETE FROM payments WHERE id = @id";
                MySqlParameter[] deleteParams = {
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
                };

                int rowsAffected = db.ExecuteNonQueryParameterized(deleteQuery, deleteParams);
                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                string checkOtherPaymentsQuery = "SELECT COUNT(*) FROM payments WHERE order_id = @orderId";
                MySqlParameter[] checkOtherParams = {
                    new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = orderId }
                };

                object otherPaymentsCount = db.ExecuteScalarParameterized(checkOtherPaymentsQuery, checkOtherParams);
                int otherPayments = Convert.ToInt32(otherPaymentsCount);

                if (otherPayments == 0)
                {
                    // Nếu không còn payment nào, đặt lại trạng thái order về pending
                    string updateOrderQuery = "UPDATE orders SET status = 'pending', end_time = NULL WHERE id = @orderId";
                    MySqlParameter[] updateOrderParams = {
                        new MySqlParameter("@orderId", MySqlDbType.Int32) { Value = orderId }
                    };

                    db.ExecuteNonQueryParameterized(updateOrderQuery, updateOrderParams);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error deleting payment: " + ex.Message));
            }
        }
    }
}