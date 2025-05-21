// Cập nhật Model/Shift.cs để thêm trường ClosedCash
using System;

namespace CoffeeShopAPI.Models
{
    public class Shift
    {
        public string Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double OpeningCash { get; set; }
        public double ClosedCash { get; set; }
        public double TotalCash { get; set; }
        public int TotalBill { get; set; }
        public string Status { get; set; }
        public string Session { get; set; }

        // Thông tin bổ sung cho frontend (nếu cần)
        public string Username { get; set; }
        public string Fullname { get; set; }
    }
}
