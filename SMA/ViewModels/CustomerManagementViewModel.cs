using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SMA.Helper;
using SMA.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SMA.Services;

namespace SMA.ViewModels
{
    public class CustomerManagementViewModel : INotifyPropertyChanged
    {
        // =====================================================
        // 1️⃣ Properties
        // =====================================================
        public ObservableCollection<Customer> Customers { get; set; }

        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand SendEmailCommand { get; }

        private bool _canAdd = true;
        public bool CanAdd
        {
            get => _canAdd;
            set { _canAdd = value; OnPropertyChanged(); }
        }

        private bool _canEditDelete = false;
        public bool CanEditDelete
        {
            get => _canEditDelete;
            set { _canEditDelete = value; OnPropertyChanged(); }
        }

        public CustomerManagementViewModel()
        {
            Customers = new ObservableCollection<Customer>();
            LoadCustomer();

            AddCommand = new RelayCommand(Add, _ => CanAdd);
            UpdateCommand = new RelayCommand(Update, _ => CanEditDelete);
            DeleteCommand = new RelayCommand(Delete, _ => CanEditDelete);
            SearchCommand = new RelayCommand(Search);
            SendEmailCommand = new RelayCommand(async _ => await SendEmail(_), _ => CanEditDelete);

            textBoxItem = new Customer();
        }

        // =====================================================
        // 2️⃣ Binding properties
        // =====================================================
        private Customer _textBoxItem;
        public Customer textBoxItem
        {
            get => _textBoxItem;
            set { _textBoxItem = value; OnPropertyChanged(); }
        }

        private Customer _selectedItem;
        public Customer selectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();

                if (_selectedItem != null)
                {
                    // Copy sang textbox
                    textBoxItem = JsonConvert.DeserializeObject<Customer>(
                        JsonConvert.SerializeObject(value));

                    CanAdd = false;
                    CanEditDelete = true;
                }
                else
                {
                    CanAdd = true;
                    CanEditDelete = false;
                }

                // Cập nhật trạng thái của command (re-evaluate CanExecute)
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _searchText;
        public string searchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        // =====================================================
        // 3️⃣ Load Data
        // =====================================================
        private void LoadCustomer()
        {
            using var context = new Prn212G3Context();
            var data = context.Customers.ToList();
            Customers.Clear();
            foreach (var item in data)
                Customers.Add(item);
        }

        private Task RefreshCustomerAsync()
        {
            using var context = new Prn212G3Context();
            var list = context.Customers.ToList();
            Customers.Clear();
            foreach (var c in list)
                Customers.Add(c);
            return Task.CompletedTask;
        }

        // =====================================================
        // 4️⃣ Generate ID
        // =====================================================
        public string GenerateCustomerId()
        {
            using var context = new Prn212G3Context();
            var lastId = context.Customers
                .OrderByDescending(c => c.CustomerId)
                .Select(c => c.CustomerId)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(lastId)) return "C001";
            int number = int.Parse(lastId.Substring(1)) + 1;
            return $"C{number:000}";
        }

        // =====================================================
        // 5️⃣ CREATE (Add)
        // =====================================================
        private void Add(object obj)
        {
            if (!ValidateInput()) return;

            try
            {
                using var context = new Prn212G3Context();

                bool exists = context.Customers.Any(c =>
                    c.Phone == textBoxItem.Phone || c.Email == textBoxItem.Email);

                if (exists)
                {
                    MessageBox.Show("Phone number or Email already exists!", "Duplicate data",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newCus = new Customer
                {
                    CustomerId = GenerateCustomerId(),
                    CustomerName = textBoxItem.CustomerName.Trim(),
                    Phone = textBoxItem.Phone?.Trim(),
                    Gender = textBoxItem.Gender,
                    Address = textBoxItem.Address?.Trim(),
                    Point = Math.Max(0, textBoxItem?.Point ?? 0),
                    Email = textBoxItem?.Email?.Trim()
                };

                context.Customers.Add(newCus);
                context.SaveChanges();

                MessageBox.Show("Added customer successfully! ", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshCustomerAsync();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while Insert: {ex.Message}");
            }
        }

        // =====================================================
        // 6️⃣ UPDATE
        // =====================================================
        private void Update(object obj)
        {
            if (_selectedItem == null)
            {
                MessageBox.Show("Please select a customer to update!");
                return;
            }
            if (!ValidateInput()) return;

            try
            {
                using var context = new Prn212G3Context();
                var existing = context.Customers.Find(_textBoxItem.CustomerId);
                if (existing == null)
                {
                    MessageBox.Show("No customers found!");
                    return;
                }

                bool duplicate = context.Customers.Any(c =>
                    (c.Phone == _textBoxItem.Phone || c.Email == _textBoxItem.Email)
                    && c.CustomerId != _textBoxItem.CustomerId);

                if (duplicate)
                {
                    MessageBox.Show("Phone number or Email already exists in another customer!", "Duplicate data",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                context.Entry(existing).CurrentValues.SetValues(_textBoxItem);
                context.SaveChanges();

                MessageBox.Show("Update successful!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshCustomerAsync();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while updating: {ex.Message}");
            }
        }

        // =====================================================
        // 7️⃣ DELETE (Check if customer has orders)
        // =====================================================
        private void Delete(object obj)
        {
            if (_selectedItem == null)
            {
                MessageBox.Show("Please select a customer to delete!");
                return;
            }

            try
            {
                using var context = new Prn212G3Context();

                // ✅ Kiểm tra khách hàng có đơn hàng không
                bool hasOrders = context.Orders.Any(o => o.CustomerId == _selectedItem.CustomerId);
                if (hasOrders)
                {
                    MessageBox.Show($"Customer '{_selectedItem.CustomerName}' already has orders and cannot be deleted.",
                        "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to delete customer {_selectedItem.CustomerName}?",
                    "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                var existing = context.Customers.Find(_selectedItem.CustomerId);
                if (existing != null)
                {
                    context.Customers.Remove(existing);
                    context.SaveChanges();
                }

                MessageBox.Show("Delete successful!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshCustomerAsync();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while deleting: {ex.Message}");
            }
        }

        // =====================================================
        // 8️⃣ SEARCH
        // =====================================================
        private void Search(object obj)
        {
            using var context = new Prn212G3Context();
            var query = _searchText?.Trim().ToLower() ?? "";

            var filtered = string.IsNullOrWhiteSpace(query)
                ? context.Customers.ToList()
                : context.Customers
                    .Where(s =>
                        s.CustomerName.ToLower().Contains(query) ||
                        s.Phone.ToLower().Contains(query) ||
                        s.Address.ToLower().Contains(query) ||
                        s.Email.ToLower().Contains(query))
                    .ToList();

            Customers.Clear();
            foreach (var c in filtered)
                Customers.Add(c);
        }

        // =====================================================
        // 9️⃣ SEND EMAIL
        // =====================================================
        private async Task SendEmail(object obj)
        {
            if (_selectedItem == null)
            {
                MessageBox.Show("Please select a customer to send email!");
                return;
            }

            try
            {
                var sendEmailUtility = new SendInvoiceEmail();
                await sendEmailUtility.SendEmailToCustomer(_selectedItem.CustomerId, 1);
                MessageBox.Show($"Email sent successfully to {_selectedItem.Email}!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send email: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        // 🔟 Helper methods
        // =====================================================
        private bool ValidateInput()
        {
            string patternEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            string patternPhone = @"^0\d{9}$";
            if (string.IsNullOrWhiteSpace(textBoxItem.CustomerName))
            {
                MessageBox.Show("⚠️ Name not empty!");
                return false;
            }
            if (string.IsNullOrWhiteSpace(textBoxItem.Phone))
            {
                MessageBox.Show("⚠️ Phone not empty!");
                return false;
            }
            if (!textBoxItem.Phone.All(char.IsDigit) || !Regex.IsMatch(textBoxItem.Phone, patternPhone))
            {
                MessageBox.Show("⚠️ Phone not valid!");
                return false;
            }
            if (string.IsNullOrWhiteSpace(textBoxItem.Email) || !Regex.IsMatch(textBoxItem.Email, patternEmail))
            {
                MessageBox.Show("⚠️ Email not valid!");
                return false;
            }
            return true;
        }

        private void ResetForm()
        {
            textBoxItem = new Customer();
            selectedItem = null;
            CanAdd = true;
            CanEditDelete = false;
            CommandManager.InvalidateRequerySuggested();
        }

        // =====================================================
        // Notify property changed
        // =====================================================
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
