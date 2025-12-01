using Microsoft.EntityFrameworkCore;
using SMA;
using SMA.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SMA.Views.Orders
{
    public partial class OrderDetailView : UserControl
    {

        public OrderDetailView(int orderId)
        {
            InitializeComponent();
            DataContext = new OrderDetailViewModel(orderId);
        }
    }
}
