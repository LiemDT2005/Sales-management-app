using System;
using SMA.Models;

namespace SMA.Services
{
    /// <summary>
    /// Session Manager - Qu?n lý thông tin user ??ng nh?p (t??ng t? Session trong Java)
    /// </summary>
    public static class SessionManager
    {
        private static User? _currentUser;

        /// <summary>
        /// User hi?n t?i ?ang ??ng nh?p
        /// </summary>
        public static User? CurrentUser
        {
            get => _currentUser;
            private set => _currentUser = value;
        }

        /// <summary>
        /// Ki?m tra user ?ã ??ng nh?p ch?a
        /// </summary>
        public static bool IsLoggedIn => _currentUser != null;

        /// <summary>
        /// Ki?m tra user có ph?i Admin không
        /// </summary>
        public static bool IsAdmin => _currentUser?.Role?.ToLower() == "admin";

        /// <summary>
        /// Ki?m tra user có ph?i Staff không
        /// </summary>
        public static bool IsStaff => _currentUser?.Role?.ToLower() == "staff";

        /// <summary>
        /// L?y tên user hi?n t?i
        /// </summary>
        public static string CurrentUserName => _currentUser?.UserName ?? "Guest";

        /// <summary>
        /// L?y email user hi?n t?i
        /// </summary>
        public static string CurrentUserEmail => _currentUser?.Email ?? string.Empty;

        /// <summary>
        /// L?y role user hi?n t?i
        /// </summary>
        public static string CurrentUserRole => _currentUser?.Role ?? "Guest";

        /// <summary>
        /// ??ng nh?p - L?u thông tin user vào session
        /// </summary>
        public static void Login(User user)
        {
            _currentUser = user;
        }

        /// <summary>
        /// ??ng xu?t - Xóa thông tin user kh?i session
        /// </summary>
        public static void Logout()
        {
            _currentUser = null;
        }

        /// <summary>
        /// Ki?m tra quy?n truy c?p theo role
        /// </summary>
        public static bool HasRole(string role)
        {
            if (_currentUser == null || string.IsNullOrEmpty(role))
                return false;

            return _currentUser.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// Ki?m tra có m?t trong các quy?n không
        /// </summary>
        public static bool HasAnyRole(params string[] roles)
        {
            if (_currentUser == null || roles == null || roles.Length == 0)
                return false;

            foreach (var role in roles)
            {
                if (HasRole(role))
                    return true;
            }

            return false;
        }
    }
}
