using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using MySql.Data.MySqlClient;
using CoffeeShopAPI.Models;
using System.Linq;

namespace CoffeeShopAPI.Controllers
{
    public class ShiftController : ApiController
    {
        private DatabaseContext db = new DatabaseContext();

        // API gốc - giữ nguyên
        // Lấy danh sách tất cả ca làm việc
        [HttpGet]
        [Route("api/shift/getAll")]
        public IHttpActionResult GetAllShifts()
        {
            string query = @"SELECT s.*, u.username, u.fullname 
                            FROM shifts s 
                            JOIN users u ON s.user_id = u.id";
            DataTable dt = db.ExecuteQuery(query);
            List<Shift> shifts = new List<Shift>();

            foreach (DataRow row in dt.Rows)
            {
                shifts.Add(MapRowToShift(row));
            }

            return Ok(shifts);
        }

        // Lấy thông tin ca làm việc theo ID
        [HttpGet]
        [Route("api/shift/getById/{id}")]
        public IHttpActionResult GetShiftById(string id)
        {
            string query = @"SELECT s.*, u.username, u.fullname 
                            FROM shifts s 
                            JOIN users u ON s.user_id = u.id 
                            WHERE s.id = @id";
            MySqlParameter[] parameters = { new MySqlParameter("@id", id) };
            DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            Shift shift = MapRowToShift(dt.Rows[0]);
            return Ok(shift);
        }

        // Lấy ca làm việc hiện tại của người dùng
        [HttpGet]
        [Route("api/shift/getCurrentByUserId/{userId}")]
        public IHttpActionResult GetCurrentShiftByUserId(int userId)
        {
            string query = @"SELECT s.*, u.username, u.fullname 
                            FROM shifts s 
                            JOIN users u ON s.user_id = u.id 
                            WHERE s.user_id = @userId AND s.status = 'open'";
            MySqlParameter[] parameters = { new MySqlParameter("@userId", userId) };
            DataTable dt = db.ExecuteParameterizedQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            Shift shift = MapRowToShift(dt.Rows[0]);
            return Ok(shift);
        }

        // Tạo ca làm việc mới
        [HttpPost]
        [Route("api/shift/create")]
        public IHttpActionResult CreateShift([FromBody] Shift shift)
        {
            try
            {
                if (shift == null || shift.UserId <= 0 || string.IsNullOrEmpty(shift.Session))
                    return BadRequest("Dữ liệu không hợp lệ.");

                // Kiểm tra xem người dùng đã có ca làm việc đang mở chưa
                string checkQuery = "SELECT COUNT(*) FROM shifts WHERE user_id = @userId AND status = 'open'";
                MySqlParameter[] checkParams = { new MySqlParameter("@userId", shift.UserId) };
                int count = Convert.ToInt32(db.ExecuteScalarParameterized(checkQuery, checkParams));

                if (count > 0)
                    return BadRequest("Người dùng đã có ca làm việc đang mở.");

                // Tạo ID ca làm việc: id nhân viên + 10 số ngẫu nhiên
                Random random = new Random();
                string shiftId = shift.UserId.ToString();
                for (int i = 0; i < 10; i++)
                {
                    shiftId += random.Next(0, 10).ToString();
                }

                // In ra log để debug
                Console.WriteLine($"Tạo ca làm việc: ID={shiftId}, UserId={shift.UserId}, Session={shift.Session}, OpeningCash={shift.OpeningCash}");

                // Tạo ca làm việc mới - sửa câu truy vấn để phù hợp với cấu trúc bảng
                string query = @"INSERT INTO shifts 
                        (id, user_id, start_time, opening_cash, total_cash, total_bill, status, session) 
                        VALUES 
                        (@id, @userId, @startTime, @openingCash, 0, 0, 'open', @session)";

                MySqlParameter[] parameters = {
                    new MySqlParameter("@id", shiftId),
                    new MySqlParameter("@userId", shift.UserId),
                    new MySqlParameter("@startTime", DateTime.Now),
                    new MySqlParameter("@openingCash", shift.OpeningCash),
                };

                // Thêm chuỗi session vào parameters
                if (shift.Session == "morning" || shift.Session == "afternoon" || shift.Session == "evening")
                {
                    parameters = parameters.Concat(new[] { new MySqlParameter("@session", shift.Session) }).ToArray();
                }
                else
                {
                    return BadRequest("Buổi làm việc không hợp lệ. Các giá trị hợp lệ là: morning, afternoon, evening");
                }

                int result = db.ExecuteNonQueryParameterized(query, parameters);
                Console.WriteLine($"Kết quả thực thi: {result} dòng bị ảnh hưởng");

                if (result > 0)
                {
                    // Lấy ca làm việc vừa tạo để trả về
                    string getQuery = "SELECT * FROM shifts WHERE id = @id";
                    MySqlParameter[] getParams = { new MySqlParameter("@id", shiftId) };
                    DataTable dt = db.ExecuteParameterizedQuery(getQuery, getParams);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        Shift createdShift = new Shift
                        {
                            Id = shiftId,
                            UserId = shift.UserId,
                            StartTime = Convert.ToDateTime(row["start_time"]),
                            EndTime = null,
                            OpeningCash = shift.OpeningCash,
                            TotalCash = 0,
                            TotalBill = 0,
                            Status = "open",
                            Session = shift.Session
                        };
                        return Ok(createdShift);
                    }
                    else
                    {
                        return Ok(new { Id = shiftId, Message = "Đã tạo ca làm việc, nhưng không thể lấy thông tin đầy đủ." });
                    }
                }
                else
                {
                    return BadRequest("Không thể tạo ca làm việc. Không có dòng nào bị ảnh hưởng.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo ca làm việc: {ex.Message}");
                return BadRequest($"Lỗi khi tạo ca làm việc: {ex.Message}");
            }
        }

        // Kết thúc ca làm việc
        [HttpPut]
        [Route("api/shift/close/{id}")]
        public IHttpActionResult CloseShift(string id)
        {
            // Cập nhật trạng thái ca làm việc
            string query = @"UPDATE shifts 
                            SET status = 'closed', end_time = @endTime 
                            WHERE id = @id AND status = 'open'";

            MySqlParameter[] parameters = {
                new MySqlParameter("@id", id),
                new MySqlParameter("@endTime", DateTime.Now)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Đã kết thúc ca làm việc.");
            else
                return NotFound();
        }

        // Cập nhật thông tin ca làm việc (giới hạn)
        [HttpPut]
        [Route("api/shift/update/{id}")]
        public IHttpActionResult UpdateShift(string id, [FromBody] Shift shift)
        {
            if (shift == null)
                return BadRequest("Dữ liệu không hợp lệ.");

            string query = @"UPDATE shifts 
                            SET total_cash = @totalCash, total_bill = @totalBill 
                            WHERE id = @id AND status = 'open'";

            MySqlParameter[] parameters = {
                new MySqlParameter("@id", id),
                new MySqlParameter("@totalCash", shift.TotalCash),
                new MySqlParameter("@totalBill", shift.TotalBill)
            };

            int result = db.ExecuteNonQueryParameterized(query, parameters);
            if (result > 0)
                return Ok("Cập nhật thông tin ca làm việc thành công.");
            else
                return NotFound();
        }

        // API MỚI: Cập nhật đầy đủ thông tin ca làm việc cho Admin
        [HttpPut]
        [Route("api/shift/admin/update/{id}")]
        public IHttpActionResult AdminUpdateShift(string id, [FromBody] Shift shift)
        {
            if (shift == null)
                return BadRequest("Dữ liệu không hợp lệ.");

            try
            {
                // Kiểm tra xem ca làm việc có tồn tại không
                string checkQuery = "SELECT COUNT(*) FROM shifts WHERE id = @id";
                MySqlParameter[] checkParams = { new MySqlParameter("@id", id) };
                int count = Convert.ToInt32(db.ExecuteScalarParameterized(checkQuery, checkParams));

                if (count == 0)
                    return NotFound();

                // Kiểm tra nếu đang thay đổi trạng thái từ đóng sang mở
                if (shift.Status == "open")
                {
                    // Kiểm tra xem đã có ca làm việc nào đang mở chưa
                    string openShiftQuery = "SELECT COUNT(*) FROM shifts WHERE status = 'open' AND id != @id";
                    MySqlParameter[] openShiftParams = { new MySqlParameter("@id", id) };
                    int openShifts = Convert.ToInt32(db.ExecuteScalarParameterized(openShiftQuery, openShiftParams));

                    if (openShifts > 0)
                        return BadRequest("Đã có ca làm việc đang mở. Chỉ được phép có một ca làm việc mở tại một thời điểm!");
                }

                // Cập nhật đầy đủ thông tin ca làm việc
                string query = @"UPDATE shifts 
                            SET user_id = @userId,
                                start_time = @startTime,
                                end_time = @endTime,
                                opening_cash = @openingCash,
                                closed_cash = @closedCash,
                                total_cash = @totalCash,
                                total_bill = @totalBill,
                                status = @status,
                                session = @session
                            WHERE id = @id";

                MySqlParameter[] parameters = {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@userId", shift.UserId),
                    new MySqlParameter("@startTime", shift.StartTime),
                    new MySqlParameter("@endTime", shift.EndTime.HasValue ? (object)shift.EndTime : DBNull.Value),
                    new MySqlParameter("@openingCash", shift.OpeningCash),
                    new MySqlParameter("@closedCash", shift.ClosedCash),
                    new MySqlParameter("@totalCash", shift.TotalCash),
                    new MySqlParameter("@totalBill", shift.TotalBill),
                    new MySqlParameter("@status", shift.Status),
                    new MySqlParameter("@session", shift.Session)
                };

                int result = db.ExecuteNonQueryParameterized(query, parameters);
                if (result > 0)
                    return Ok("Cập nhật thông tin ca làm việc thành công.");
                else
                    return BadRequest("Không thể cập nhật thông tin ca làm việc.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật ca làm việc: {ex.Message}");
                return BadRequest($"Lỗi khi cập nhật ca làm việc: {ex.Message}");
            }
        }

        // Phương thức cập nhật ca sau khi thanh toán
        [HttpPut]
        [Route("api/shift/updateAfterPayment/{id}")]
        public IHttpActionResult UpdateShiftAfterPayment(string id, [FromBody] object data)
        {
            try
            {
                // Lấy dữ liệu từ request
                dynamic paymentData = Newtonsoft.Json.JsonConvert.DeserializeObject(data.ToString());

                double amount = paymentData.amount;
                bool isCash = paymentData.isCash;

                // Lấy thông tin ca hiện tại
                string getQuery = "SELECT * FROM shifts WHERE id = @id AND status = 'open'";
                MySqlParameter[] getParams = { new MySqlParameter("@id", id) };
                DataTable dt = db.ExecuteParameterizedQuery(getQuery, getParams);

                if (dt.Rows.Count == 0)
                    return NotFound();

                DataRow row = dt.Rows[0];
                double currentTotal = Convert.ToDouble(row["total_cash"]);
                int currentBills = Convert.ToInt32(row["total_bill"]);
                double currentClosed = row["closed_cash"] == DBNull.Value ?
                                      Convert.ToDouble(row["opening_cash"]) :
                                      Convert.ToDouble(row["closed_cash"]);

                // Cập nhật số liệu
                double newTotalCash = currentTotal + amount;
                int newTotalBill = currentBills + 1;
                double newClosedCash = isCash ? currentClosed + amount : currentClosed;

                // Tạo câu truy vấn cập nhật
                string query = @"UPDATE shifts 
                        SET total_cash = @totalCash, 
                            total_bill = @totalBill,
                            closed_cash = @closedCash 
                        WHERE id = @id AND status = 'open'";

                MySqlParameter[] parameters = {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@totalCash", newTotalCash),
                    new MySqlParameter("@totalBill", newTotalBill),
                    new MySqlParameter("@closedCash", newClosedCash)
                };

                int result = db.ExecuteNonQueryParameterized(query, parameters);

                if (result > 0)
                    return Ok(new
                    {
                        TotalCash = newTotalCash,
                        TotalBill = newTotalBill,
                        ClosedCash = newClosedCash,
                        Message = "Cập nhật thông tin ca làm việc thành công."
                    });
                else
                    return BadRequest("Không thể cập nhật thông tin ca làm việc.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật ca làm việc: {ex.Message}");
                return BadRequest($"Lỗi khi cập nhật ca làm việc: {ex.Message}");
            }
        }

        // Hàm hỗ trợ chuyển đổi DataRow thành đối tượng Shift
        private Shift MapRowToShift(DataRow row)
        {
            return new Shift
            {
                Id = row["id"].ToString(),
                UserId = Convert.ToInt32(row["user_id"]),
                StartTime = Convert.ToDateTime(row["start_time"]),
                EndTime = row["end_time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["end_time"]),
                OpeningCash = Convert.ToDouble(row["opening_cash"]),
                ClosedCash = row["closed_cash"] == DBNull.Value ? 0 : Convert.ToDouble(row["closed_cash"]),
                TotalCash = Convert.ToDouble(row["total_cash"]),
                TotalBill = Convert.ToInt32(row["total_bill"]),
                Status = row["status"].ToString(),
                Session = row["session"].ToString(),
                Username = row["username"].ToString(),
                Fullname = row["fullname"].ToString()
            };
        }
    }
}