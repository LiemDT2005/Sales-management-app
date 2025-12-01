# Sales Management Application – Contribution by **Đặng Thanh Liêm**

## 👤 Role: Project Leader

## Group size: 4

**Responsibilities:**

- Overall project coordination
- Designing system architecture (WPF, MVVM, EF Core)
- Implementing core product-related features
- Implementing PDF export for invoices
- Code reviewing and ensuring coding standards
- Supporting team members during development

---

## 🔧 Technologies Used

- **.NET 8**
- **WPF (Windows Presentation Foundation)**
- **MVVM Architecture**
- **Entity Framework Core**
- **SQL Server**
- **iText7 / QuestPDF** (for PDF Export)

---

## 📌 Features Implemented by Me

### **1. Product Management Module**

I implemented the full CRUD flow for product management:

#### ✅ View List Product\*

- Display all products with pagination + search
- Bind data via ObservableCollection in ViewModel
- Support filtering by category

#### ✅ View Product Detail\*

- Modal/detail dialog showing full product information
- Auto-update UI using INotifyPropertyChanged

#### ✅ Create Product\*

- Form validation (required fields, numeric check, category validation)
- Insert function using EF Core
- Auto-refresh UI after creation

#### ✅ Update Product\*

- Edit existing product details
- Handle concurrency update with EF Core
- Prevent editing deleted items

#### ❌ Delete Product\*

- Soft delete or hard delete
- Show confirmation dialog
- Refresh UI after deletion

#### 🔍 Search Product\*

- Search by **product name, code, category**
- LINQ-based filtering
- Auto-updated UI using MVVM commands

---

## 📞 Contact

**Đặng Thanh Liêm – CE190697**
📧 Email: [liemdt.ce190697@gmail.com]
📎 GitHub: [https://github.com/LiemDT2005]

---

> _This README describes only the contribution section belonging to Liêm (LiemDT) in the team project._
