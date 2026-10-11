using FamilyManagement.Shared.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FamilyManagement.API.Services;

/// <summary>Renders the financial reports as downloadable PDF statements.</summary>
public class PdfReportService
{
    private const string HeaderColor = "#1D7A62";
    private readonly record struct Col(string Title, float Weight, bool AlignRight);

    public byte[] BuildLedger(LedgerReportDto report, IReadOnlyList<LedgerRowDto> rows, string familyName, string currency)
    {
        var subtitle = $"{report.AccountCode} — {report.AccountName}   |   {report.DateFrom:dd MMM yyyy} to {report.DateTo:dd MMM yyyy}";
        return Build(familyName, "Ledger Statement", subtitle, content =>
        {
            Summary(content, currency,
                ("Opening Balance", report.OpeningBalance),
                ("Total Debit", report.TotalDebit),
                ("Total Credit", report.TotalCredit),
                ("Closing Balance", report.ClosingBalance));

            Table(content,
                [new("Date", 1.3f, false), new("Voucher", 1.6f, false), new("Narration", 3.2f, false),
                 new("Debit", 1.2f, true), new("Credit", 1.2f, true), new("Balance", 1.4f, true)],
                rows.Select(r => new[]
                {
                    r.Date.ToString("dd-MMM-yy"), r.VoucherNumber, r.Narration ?? "",
                    Money(r.Debit), Money(r.Credit), $"{r.RunningBalance:N2}"
                }));
        });
    }

    public byte[] BuildBook(BookReportDto report, IReadOnlyList<BookRowDto> rows, string familyName, string currency)
    {
        var subtitle = $"{report.DateFrom:dd MMM yyyy} to {report.DateTo:dd MMM yyyy}";
        return Build(familyName, report.Title, subtitle, content =>
        {
            Summary(content, currency,
                ("Opening Balance", report.OpeningBalance),
                ("Total Receipts", report.TotalReceipts),
                ("Total Payments", report.TotalPayments),
                ("Closing Balance", report.ClosingBalance));

            Table(content,
                [new("Date", 1.3f, false), new("Voucher", 1.6f, false), new("Account", 1.8f, false),
                 new("Narration", 2.6f, false), new("Receipt", 1.2f, true), new("Payment", 1.2f, true), new("Balance", 1.4f, true)],
                rows.Select(r => new[]
                {
                    r.Date.ToString("dd-MMM-yy"), r.VoucherNumber, r.AccountName, r.Narration ?? "",
                    Money(r.Receipt), Money(r.Payment), $"{r.RunningBalance:N2}"
                }));
        });
    }

    public byte[] BuildTrialBalance(TrialBalanceDto report, string familyName, string currency)
    {
        return Build(familyName, "Trial Balance", $"As of {report.AsOfDate:dd MMM yyyy}", content =>
        {
            Table(content,
                [new("Code", 1f, false), new("Account", 4f, false), new("Debit", 1.6f, true), new("Credit", 1.6f, true)],
                report.Rows.Select(r => new[]
                {
                    r.AccountCode, r.AccountName, Money(r.Debit), Money(r.Credit)
                }),
                footer: ["", "Total", $"{report.TotalDebit:N2}", $"{report.TotalCredit:N2}"]);

            content.Item().PaddingTop(8).Text(report.IsBalanced
                    ? $"Debit and credit totals agree ({currency} {report.TotalDebit:N2})."
                    : "WARNING: totals do not agree!")
                .FontSize(9).FontColor(report.IsBalanced ? Colors.Grey.Darken1 : Colors.Red.Medium);
        });
    }

    public byte[] BuildProfitLoss(ProfitLossDto report, string familyName, string currency)
    {
        var subtitle = $"{report.DateFrom:dd MMM yyyy} to {report.DateTo:dd MMM yyyy}";
        return Build(familyName, "Profit & Loss Statement", subtitle, content =>
        {
            Section(content, "Income", report.IncomeLines, report.TotalIncome);
            Section(content, "Expenses", report.ExpenseLines, report.TotalExpense);

            content.Item().PaddingTop(10).Background(Colors.Grey.Lighten3).Padding(8).Row(row =>
            {
                row.RelativeItem().Text(report.NetProfit >= 0 ? "NET PROFIT" : "NET LOSS").Bold().FontSize(11);
                row.ConstantItem(140).AlignRight()
                    .Text($"{currency} {Math.Abs(report.NetProfit):N2}").Bold().FontSize(11)
                    .FontColor(report.NetProfit >= 0 ? HeaderColor : Colors.Red.Medium);
            });
        });
    }

    public byte[] BuildBalanceSheet(BalanceSheetDto report, string familyName, string currency)
    {
        return Build(familyName, "Balance Sheet", $"As of {report.AsOfDate:dd MMM yyyy}", content =>
        {
            Section(content, "Assets", report.AssetLines, report.TotalAssets);
            Section(content, "Liabilities", report.LiabilityLines, report.TotalLiabilities);
            Section(content, "Equity", report.EquityLines, report.TotalEquity);

            content.Item().PaddingTop(8).Text(report.IsBalanced
                    ? $"Assets ({currency} {report.TotalAssets:N2}) = Liabilities + Equity ({currency} {report.TotalLiabilities + report.TotalEquity:N2})"
                    : "WARNING: the balance sheet does not balance!")
                .FontSize(9).FontColor(report.IsBalanced ? Colors.Grey.Darken1 : Colors.Red.Medium);
        });
    }

    // ---- building blocks ---------------------------------------------------

    private static byte[] Build(string familyName, string title, string subtitle, Action<ColumnDescriptor> body)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(familyName).Bold().FontSize(15).FontColor(HeaderColor);
                            c.Item().Text(title).FontSize(12).Bold();
                            c.Item().Text(subtitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(150).AlignRight().AlignBottom()
                            .Text($"Generated {DateTime.Now:dd MMM yyyy, hh:mm tt}")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    header.Item().PaddingTop(6).LineHorizontal(1.2f).LineColor(HeaderColor);
                    header.Item().PaddingBottom(8);
                });

                page.Content().Column(body);

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("Family Management & Accounting System  ·  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Summary(ColumnDescriptor content, string currency, params (string Label, decimal Value)[] items)
    {
        content.Item().PaddingBottom(10).Row(row =>
        {
            foreach (var (label, value) in items)
            {
                row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                {
                    c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{currency} {value:N2}").Bold().FontSize(10);
                });
                row.ConstantItem(6);
            }
        });
    }

    private static void Table(ColumnDescriptor content, Col[] columns,
        IEnumerable<string[]> rows, string[]? footer = null)
    {
        content.Item().Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                foreach (var col in columns)
                    def.RelativeColumn(col.Weight);
            });

            table.Header(header =>
            {
                foreach (var col in columns)
                {
                    var cell = header.Cell().Background(HeaderColor).Padding(5);
                    (col.AlignRight ? cell.AlignRight() : cell).Text(col.Title).FontColor(Colors.White).Bold().FontSize(8.5f);
                }
            });

            var even = false;
            foreach (var row in rows)
            {
                even = !even;
                for (var i = 0; i < columns.Length; i++)
                {
                    var cell = table.Cell()
                        .Background(even ? Colors.White : Colors.Grey.Lighten4)
                        .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(5);
                    (columns[i].AlignRight ? cell.AlignRight() : cell).Text(row[i]);
                }
            }

            if (footer is not null)
            {
                for (var i = 0; i < columns.Length; i++)
                {
                    var cell = table.Cell().Background(Colors.Grey.Lighten3).Padding(5);
                    (columns[i].AlignRight ? cell.AlignRight() : cell).Text(footer[i]).Bold();
                }
            }
        });
    }

    private static void Section(ColumnDescriptor content, string title, IReadOnlyList<ReportLineDto> lines, decimal total)
    {
        content.Item().PaddingTop(8).Text(title).Bold().FontSize(11).FontColor(HeaderColor);
        content.Item().PaddingTop(2).Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                def.RelativeColumn(1);
                def.RelativeColumn(4);
                def.RelativeColumn(1.6f);
            });

            foreach (var line in lines)
            {
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(line.AccountCode);
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(line.AccountName);
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{line.Amount:N2}");
            }

            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("");
            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text($"Total {title}").Bold();
            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{total:N2}").Bold();
        });
    }

    private static string Money(decimal value) => value == 0 ? "" : value.ToString("N2");
}
