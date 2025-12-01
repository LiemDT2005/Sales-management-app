using SMA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SMA.Helper
{
    public static class ProductInputValidator
    {
        // Regex chấp nhận tên file không chứa khoảng trắng/ký tự lạ, chỉ .jpg
        // ví dụ: abc.jpg, a-b_c123.jpg, A.B.C.jpg
        private static readonly Regex JpgFileRegex =
            new Regex(@"^[A-Za-z0-9][A-Za-z0-9_\-\.]{0,63}\.jpg$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<string> ValidateAll(
            string name,
            int categoryId,
            int stockQuantity,
            decimal price,
            string imageFileName,
            IEnumerable<Category> categories,
            string description = null)
        {
            var errors = new List<string>();

            // ProductName
            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Product Name is required.");
            else if (name.Length < 2 || name.Length > 100)
                errors.Add("Product Name length must be between 2 and 100 characters.");

            // Category
            if (categoryId <= 0 || (categories != null && !categories.Any(c => c.CategoryId == categoryId)))
                errors.Add("Please select a valid Category.");

            // Stock
            if (stockQuantity < 0)
                errors.Add("Stock must be >= 0.");
            else if (stockQuantity > 1_000_000)
                errors.Add("Stock is too large.");

            // Price
            if (price < 1)
                errors.Add("Price must be >= 1.");
            else if (price > 1_000_000_000m)
                errors.Add("Price is too large.");

            // Description
            if (string.IsNullOrEmpty(description) || description.Length > 2000)
                errors.Add("Description cannot null (max 2000 chars).");


            return errors;
        }

    }
}
