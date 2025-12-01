using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SMA.Models;

namespace SMA.Services
{
    public class AuthService
    {
        private readonly Prn212G3Context _context;

        public AuthService()
        {
            _context = new Prn212G3Context();
        }

        // Authenticate user with email and password
        public User? Authenticate(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null)
                return null;

            // Verify password (assuming password is hashed)
            if (VerifyPassword(password, user.Password))
            {
                return user;
            }

            return null;
        }

        // Hash password using SHA256
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Verify password
        private bool VerifyPassword(string password, string hashedPassword)
        {
            string hashOfInput = HashPassword(password);
            return StringComparer.OrdinalIgnoreCase.Compare(hashOfInput, hashedPassword) == 0;
        }

        // Get user by email
        public User? GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        // Update password
        public bool UpdatePassword(string email, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null)
                return false;

            user.Password = HashPassword(newPassword);
            user.PasswordRecoveryToken = null; // Clear recovery token
            user.TokenExpiry = null; // Clear token expiry
            
            _context.SaveChanges();
            return true;
        }

        // Generate and save OTP to database
        public string GenerateAndSaveOtp(string email, int otpLength = 6)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null)
                throw new Exception("User not found");

            // Generate random OTP
            var random = new Random();
            string otp = string.Empty;
            for (int i = 0; i < otpLength; i++)
            {
                otp += random.Next(0, 10).ToString();
            }

            // Save OTP and expiry time to database
            user.PasswordRecoveryToken = otp;
            user.TokenExpiry = DateTime.Now.AddMinutes(5); // OTP valid for 5 minutes
            
            _context.SaveChanges();

            return otp;
        }

        // Verify OTP from database
        public bool VerifyOtp(string email, string otp)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null)
                return false;

            // Check if OTP exists
            if (string.IsNullOrEmpty(user.PasswordRecoveryToken))
                return false;

            // Check if OTP expired
            if (user.TokenExpiry == null || DateTime.Now > user.TokenExpiry)
                return false;

            // Check if OTP matches
            if (user.PasswordRecoveryToken != otp)
                return false;

            return true;
        }

        // Invalidate OTP by setting expiry to past (better than clearing for audit trail)
        public void InvalidateOtp(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user != null)
            {
                // Set expiry to past to invalidate OTP (keep OTP for audit trail)
                user.TokenExpiry = DateTime.Now.AddMinutes(-1);
                _context.SaveChanges();
            }
        }

        // Clear OTP completely (use after successful password reset)
        public void ClearOtp(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user != null)
            {
                user.PasswordRecoveryToken = null;
                user.TokenExpiry = null;
                _context.SaveChanges();
            }
        }

        // Check if OTP is expired
        public bool IsOtpExpired(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null || user.TokenExpiry == null)
                return true;

            return DateTime.Now > user.TokenExpiry;
        }

        // Set password recovery token (legacy - keep for compatibility)
        public bool SetPasswordRecoveryToken(string email, string token, DateTime expiry)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null)
                return false;

            user.PasswordRecoveryToken = token;
            user.TokenExpiry = expiry;
            
            _context.SaveChanges();
            return true;
        }

        // Verify recovery token (legacy - keep for compatibility)
        public bool VerifyRecoveryToken(string email, string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            
            if (user == null || user.PasswordRecoveryToken == null || user.TokenExpiry == null)
                return false;

            if (DateTime.Now > user.TokenExpiry)
                return false; // Token expired

            return user.PasswordRecoveryToken == token;
        }
    }
}
