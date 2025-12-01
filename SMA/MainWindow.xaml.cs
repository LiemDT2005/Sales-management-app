using SMA.ViewModels;
using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SMA.Models;
using SMA.Views;
using SMA.Helper;
using SMA.Views.Orders;
using SMA.Views.Staff;
using SMA.Views.Admin;

namespace SMA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double CollapsedWidth = 60;
        private const double ExpandedWidth = 260;

        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
                // Initialize column width to match initial VM state (no animation on startup)
                SidebarColumn.Width = new GridLength(vm.IsSidebarCollapsed ? CollapsedWidth : ExpandedWidth, GridUnitType.Pixel);
            }
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsSidebarCollapsed) && sender is MainViewModel vm)
            {
                AnimateSidebar(vm.IsSidebarCollapsed);
            }
        }

        private void AnimateSidebar(bool collapsed)
        {
            double from = SidebarColumn.Width.IsStar ? SidebarColumn.ActualWidth : SidebarColumn.Width.Value;
            double to = collapsed ? CollapsedWidth : ExpandedWidth;

            var animation = new GridLengthAnimation
            {
                From = new GridLength(from, GridUnitType.Pixel),
                To = new GridLength(to, GridUnitType.Pixel),
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };

            // Animate the ColumnDefinition.Width property directly
            SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);
        }

        private void MainFrame_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (sender is Button b)
                MessageBox.Show($"Bạn vừa bấm: {b.Content}");
        }

        private void OpenOrderList_Click(object sender, RoutedEventArgs e)
        {
            // Load OrderList page into MainContent
            var orderListView = new Views.Orders.OrderList();
            MainContent.Content = orderListView;
        }

        private void ManageUsers_Click(object sender, RoutedEventArgs e)
        {
            // Load UserManagementView into MainContent
            var userManagementView = new Views.Admin.UserManagementView();
            MainContent.Content = userManagementView;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Load UserManagementView into MainContent
            var userManagementView = new Views.Admin.UserManagementView();
            MainContent.Content = userManagementView;
        }

        private void ManageCustomer_Click(object sender, RoutedEventArgs e)
        {

            var view = new Views.Staff.CustomerManagementView();
            MainContent.Content = view;
        }

        private void Button_Product(object sender, RoutedEventArgs e)
        {

            var view = new Views.Admin.ProductView();
            MainContent.Content = view;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Content?.ToString() == "Orders")
                {
                    MessageBox.Show("Orders view will be shown here.");
                }
                else if (button.Content?.ToString() == "Customers")
                {
                    if (DataContext is MainViewModel vm)
                    {
                        // Example: vm.CurrentViewModel = new CustomerVM(); if you have a VM
                        MessageBox.Show("Customers view will be shown here.");
                    }
                }
            }
        }

        private void OpenProductView_Click(object sender, RoutedEventArgs e)
        {
            // Load ProductView into MainContent
            var productView = new Views.Admin.ProductView();
            MainContent.Content = productView;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // Categories placeholder
            MessageBox.Show("Categories feature coming soon.");
        }
    }
}