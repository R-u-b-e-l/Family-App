namespace FamilyManagement.Maui.Views;

public partial class ReportsPage : ContentPage
{
    public ReportsPage()
    {
        InitializeComponent();

        Tap(LedgerCard, "ledger");
        Tap(CashBookCard, "book?kind=cash");
        Tap(BankBookCard, "book?kind=bank");
        Tap(TrialBalanceCard, "trial-balance");
        Tap(ProfitLossCard, "profit-loss");
        Tap(BalanceSheetCard, "balance-sheet");
    }

    private static void Tap(Border card, string route)
    {
        var gesture = new TapGestureRecognizer();
        gesture.Tapped += async (_, _) => await Shell.Current.GoToAsync(route);
        card.GestureRecognizers.Add(gesture);
    }
}
