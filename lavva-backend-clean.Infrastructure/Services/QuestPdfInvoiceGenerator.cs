using lavva_backend_clean.Domain.Entities.Invoices;
using lavva_backend_clean.Domain.Entities.Orders;
using lavva_backend_clean.Shared.Resources;
using Microsoft.Extensions.Localization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace lavva_backend_clean.Infrastructure.Services;

public class QuestPdfInvoiceGenerator : IInvoicePdfGenerator
{
    private readonly IStringLocalizer<SharedResource>? _localizer;

    static QuestPdfInvoiceGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "NotoSansArabic-Regular.ttf");
        if (File.Exists(fontPath))
        {
            FontManager.RegisterFont(File.OpenRead(fontPath));
        }
    }

    public QuestPdfInvoiceGenerator(IStringLocalizer<SharedResource>? localizer = null)
    {
        _localizer = localizer;
    }

    public Task<byte[]> GeneratePdfAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        return Task.FromResult(GeneratePdf(invoice));
    }

    private byte[] GeneratePdf(Invoice invoice)
    {
        var lang = invoice.Order?.Customer?.PreferredLanguage ?? CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        var isArabic = lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var currency = invoice.Order?.Currency ?? string.Empty;
        string Amount(decimal value) => $"{value.ToString("0.##", CultureInfo.InvariantCulture)} {currency}".Trim();

        string T(string key, string enFallback, string arFallback)
        {
            if (_localizer != null)
            {
                var val = _localizer[key];
                if (!val.ResourceNotFound && !string.IsNullOrWhiteSpace(val.Value))
                    return val.Value;
            }
            return isArabic ? arFallback : enFallback;
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Noto Sans Arabic").FontSize(12));
                
                if (isArabic)
                    page.ContentFromRightToLeft();
                else
                    page.ContentFromLeftToRight();

                page.Header()
                    .AlignCenter()
                    .Text($"LAVVA — {T("Invoice_Header", "INVOICE", "فاتورة")}: {invoice.InvoiceNumber}")
                    .SemiBold().FontSize(20).FontColor("#2A5841");

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col2 =>
                            {
                                col2.Item().Text($"{T("Invoice_Number", "Invoice Number", "رقم الفاتورة")}: {invoice.InvoiceNumber}");
                                col2.Item().Text($"{T("Invoice_Date", "Date", "التاريخ")}: {invoice.IssuedAt:dd/MM/yyyy HH:mm}");
                                col2.Item().Text($"{T("Invoice_Order", "Order", "الطلب")}: {invoice.Order?.OrderNumber ?? invoice.OrderId.ToString()[..8]}");
                                if (invoice.Order?.Country != null)
                                    col2.Item().Text($"{T("Invoice_Country", "Country", "الدولة")}: {invoice.Order.Country.Name}");
                            });

                            row.RelativeItem().Column(col2 =>
                            {
                                col2.Item().Text(T("Invoice_CustomerInfo", "Customer Information:", "بيانات العميل:")).Bold();
                                if (invoice.Order?.Customer != null)
                                {
                                    col2.Item().Text($"{T("Invoice_CustomerName", "Name", "الاسم")}: {invoice.Order.Customer.Name}");
                                    col2.Item().Text($"{T("Invoice_CustomerEmail", "Email", "البريد الإلكتروني")}: {invoice.Order.Customer.Email}");
                                    col2.Item().Text($"{T("Invoice_CustomerPhone", "Phone", "الهاتف")}: {invoice.Order.Customer.Phone}");
                                }
                                else if (invoice.Order != null)
                                {
                                    var guestName = invoice.Order.GuestName ?? invoice.Order.Phone ?? (isArabic ? "زائر" : "Guest");
                                    var guestPhone = invoice.Order.GuestPhone ?? invoice.Order.Phone ?? "-";
                                    var guestAddr = invoice.Order.GuestAddress ?? invoice.Order.Address ?? "-";
                                    col2.Item().Text($"{T("Invoice_CustomerName", "Name", "الاسم")}: {guestName}");
                                    col2.Item().Text($"{T("Invoice_CustomerPhone", "Phone", "الهاتف")}: {guestPhone}");
                                    col2.Item().Text($"{T("Invoice_CustomerAddress", "Address", "العنوان")}: {guestAddr}");
                                }
                                else
                                {
                                    col2.Item().Text(T("Invoice_GuestOrder", "Guest Order", "طلب زائر"));
                                }
                            });
                        });


                        col.Item().PaddingTop(1, Unit.Centimetre).LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Invoice_TableProduct", "Product", "المنتج")).Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Invoice_TableQty", "Qty", "الكمية")).Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Invoice_TableUnitPrice", "Unit Price", "سعر الوحدة")).Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Invoice_TableTotal", "Total", "الإجمالي")).Bold().AlignRight();
                            });

                            if (invoice.Order?.OrderItems != null && invoice.Order.OrderItems.Any())
                            {
                                foreach (var item in invoice.Order.OrderItems)
                                {
                                    var totalPrice = item.Quantity * item.PriceAtOrder;
                                    var productName = isArabic 
                                        ? (!string.IsNullOrWhiteSpace(item.Product?.NameAr) ? item.Product.NameAr : (item.Product?.NameEn ?? "منتج"))
                                        : (!string.IsNullOrWhiteSpace(item.Product?.NameEn) ? item.Product.NameEn : (item.Product?.NameAr ?? "Product"));
                                    
                                    table.Cell().Padding(5).Text(productName);
                                    table.Cell().Padding(5).Text(item.Quantity.ToString()).AlignRight();
                                    table.Cell().Padding(5).Text(Amount(item.PriceAtOrder)).AlignRight();
                                    table.Cell().Padding(5).Text(Amount(totalPrice)).AlignRight();
                                }
                            }
                            else
                            {
                                table.Cell().Padding(5).Text(T("Invoice_NoItems", "No items loaded", "لا توجد عناصر"));
                                table.Cell().Padding(5).Text("-");
                                table.Cell().Padding(5).Text("-");
                                table.Cell().Padding(5).Text("-");
                            }
                        });

                        col.Item().PaddingTop(1, Unit.Centimetre)
                            .AlignRight()
                            .Text($"{T("Invoice_TotalAmount", "Total Amount", "المبلغ الإجمالي")}: {Amount(invoice.TotalAmount.Value)}")
                            .SemiBold().FontSize(16).FontColor(Colors.Green.Darken2);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(T("Invoice_ThankYou", "Thank you for your business!", "شكراً لتعاملكم معنا!"))
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }
}
