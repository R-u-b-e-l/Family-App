using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

/// <summary>The member-wise dashboard opened for a specific member (admin view).</summary>
public class MemberStatementPage(MyDashboardViewModel viewModel) : MyDashboardPage(viewModel);
