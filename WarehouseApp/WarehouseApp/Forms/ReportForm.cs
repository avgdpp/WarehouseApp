using WarehouseApp.Models;

namespace WarehouseApp.Forms;

/// <summary>Форма отчётности по отгрузкам за период</summary>
public class ReportForm : Form
{
    private readonly AppServices _svc;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Panel _scrollArea = null!;
    private Label _lblSummary = null!;
    private List<Shipment> _currentData = new();

    public ReportForm(AppServices svc)
    {
        _svc = svc;
        Build();
        RunReport();
    }

    private void Build()
    {
        Text = "Отчёт по отгрузкам";
        ClientSize = new Size(1200, 800);
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UI.BgLight;
        Font = UI.DefaultFont;
        AutoScaleMode = AutoScaleMode.None;

        // --- Top filter bar ---
        var topBar = UI.CreatePanel(UI.TopBar);
        topBar.Height = 72;
        topBar.Dock = DockStyle.Top;
        Controls.Add(topBar);

        var lblTitle = new Label
        {
            Text = "Отчёт по отгрузкам",
            Font = UI.Px(24),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 22),
            BackColor = Color.Transparent
        };
        topBar.Controls.Add(lblTitle);

        var lblFrom = new Label { Text = "С:", Font = UI.FontMed, ForeColor = Color.White, AutoSize = true, Location = new Point(320, 24), BackColor = Color.Transparent };
        topBar.Controls.Add(lblFrom);

        _dtpFrom = new DateTimePicker
        {
            Font = UI.FontMed,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddMonths(-1),
            Location = new Point(350, 18),
            Width = 160
        };
        topBar.Controls.Add(_dtpFrom);

        var lblTo = new Label { Text = "По:", Font = UI.FontMed, ForeColor = Color.White, AutoSize = true, Location = new Point(530, 24), BackColor = Color.Transparent };
        topBar.Controls.Add(lblTo);

        _dtpTo = new DateTimePicker
        {
            Font = UI.FontMed,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
            Location = new Point(572, 18),
            Width = 160
        };
        topBar.Controls.Add(_dtpTo);

        var btnApply = UI.CreatePillButton("Показать", UI.BtnBlue, new Size(140, 42), UI.FontMed);
        btnApply.Location = new Point(756, 14);
        btnApply.Click += (_, _) => RunReport();
        topBar.Controls.Add(btnApply);

        var btnExport = UI.CreatePillButton("Экспорт CSV", UI.BtnGreen, new Size(160, 42), UI.FontMed);
        btnExport.Location = new Point(910, 14);
        btnExport.Click += (_, _) => ExportReport();
        topBar.Controls.Add(btnExport);

        // --- Column headers ---
        var header = UI.CreatePanel(UI.HeaderRow);
        header.Height = 48;
        header.Dock = DockStyle.Top;
        header.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(UI.TextDark);
            using var f = UI.Px(16);
            var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            var sfL = new StringFormat { LineAlignment = StringAlignment.Center };
            int w = header.Width;
            // Текущая валюта из настроек — используется в заголовках денежных колонок.
            string cur = _svc.CurrencyService.Settings.Currency == "RUB"
                ? "₽"
                : _svc.CurrencyService.Settings.Currency;
            e.Graphics.DrawString("Дата", f, brush, new RectangleF(16, 0, 110, 48), sfL);
            e.Graphics.DrawString("Покупатель", f, brush, new RectangleF(130, 0, 280, 48), sfL);
            e.Graphics.DrawString("Адрес", f, brush, new RectangleF(420, 0, 240, 48), sfL);
            e.Graphics.DrawString($"Сумма ({cur})", f, brush, new RectangleF(w - 440, 0, 130, 48), sf);
            e.Graphics.DrawString($"Себестоимость ({cur})", f, brush, new RectangleF(w - 300, 0, 130, 48), sf);
            e.Graphics.DrawString($"Прибыль ({cur})", f, brush, new RectangleF(w - 160, 0, 130, 48), sf);
        };
        Controls.Add(header);

        // --- Summary bar ---
        _lblSummary = new Label
        {
            Font = UI.FontMedBold,
            ForeColor = UI.TextDark,
            BackColor = UI.BgCard,
            Dock = DockStyle.Bottom,
            Height = 52,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_lblSummary);

        // --- Scroll area ---
        _scrollArea = UI.CreatePanel(Color.White);
        _scrollArea.AutoScroll = true;
        _scrollArea.Dock = DockStyle.Fill;
        Controls.Add(_scrollArea);
        _scrollArea.BringToFront();
    }

    private void RunReport()
    {
        _currentData = _svc.ReportService.GetShipmentsByPeriod(_dtpFrom.Value, _dtpTo.Value);
        RenderTable();
    }

    private void RenderTable()
    {
        _scrollArea.SuspendLayout();
        _scrollArea.Controls.Clear();

        int y = 0;
        int w = Math.Max(_scrollArea.ClientSize.Width - 2, 600);

        foreach (var s in _currentData)
        {
            var row = UI.CreatePanel(Color.White);
            row.Bounds = new Rectangle(0, y, w, 52);
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Paint += (_, e) =>
            {
                using var pen = new Pen(UI.RowBorder, 1);
                e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
            };

            row.Controls.Add(MakeLabel(s.DisplayDate, new Rectangle(16, 0, 110, 52)));
            row.Controls.Add(MakeLabel(s.Recipient, new Rectangle(130, 0, 280, 52)));
            var addrLbl = MakeLabel(s.Address, new Rectangle(420, 0, 240, 52));
            addrLbl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Controls.Add(addrLbl);

            var sumLbl = MakeLabel(FmtAt(s.TotalCost, s), new Rectangle(w - 440, 0, 130, 52), ContentAlignment.MiddleRight);
            sumLbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(sumLbl);

            var costLbl = MakeLabel(FmtAt(s.TotalPurchaseCost, s), new Rectangle(w - 300, 0, 130, 52), ContentAlignment.MiddleRight);
            costLbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(costLbl);

            Color profitColor = s.Profit >= 0 ? Color.FromArgb(20, 140, 20) : UI.BtnRed;
            var profitLbl = MakeLabel(FmtAt(s.Profit, s), new Rectangle(w - 160, 0, 130, 52), ContentAlignment.MiddleRight);
            profitLbl.ForeColor = profitColor;
            profitLbl.Font = UI.FontMedBold;
            profitLbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(profitLbl);

            _scrollArea.Controls.Add(row);
            y += 52;
        }

        _scrollArea.ResumeLayout();

        // Итоги: КРИТИЧНО — сначала конвертируем каждую отгрузку по её историческому
        // курсу, и только потом суммируем. Суммировать сырые рубли и конвертировать
        // итог по одному курсу нельзя — это нарушит принцип исторических курсов
        // (разные отгрузки были при разных курсах).
        decimal totalSum = _currentData.Sum(s => ConvertAmount(s.TotalCost, s));
        decimal totalCost = _currentData.Sum(s => ConvertAmount(s.TotalPurchaseCost, s));
        decimal totalProfit = _currentData.Sum(s => ConvertAmount(s.Profit, s));
        _lblSummary.Text = $"Итого за период: {_currentData.Count} отгрузок  |  " +
                           $"Сумма: {FmtAggregate(totalSum)}  |  " +
                           $"Себестоимость: {FmtAggregate(totalCost)}  |  " +
                           $"Прибыль: {FmtAggregate(totalProfit)}";
    }

    /// <summary>Форматирует сумму по историческому курсу отгрузки.</summary>
    private string FmtAt(decimal rub, Shipment s) =>
        _svc.CurrencyService.FormatPriceAt(rub,
            s.GetStoredRate(_svc.CurrencyService.Settings.Currency));

    /// <summary>Конвертирует сумму в рублях в текущую валюту по курсу,
    /// зафиксированному в конкретной отгрузке (fallback — актуальный курс).</summary>
    private decimal ConvertAmount(decimal rub, Shipment s)
    {
        var settings = _svc.CurrencyService.Settings;
        if (settings.Currency == "RUB") return rub;
        decimal rate = s.GetStoredRate(settings.Currency) ?? settings.GetRate(settings.Currency);
        if (rate <= 0) return rub;
        return Math.Round(rub / rate, 2);
    }

    /// <summary>Форматирует агрегированное значение, УЖЕ сконвертированное
    /// в текущую валюту (отдельный метод нужен, чтобы не применить конверсию
    /// повторно через CurrencyService.FormatPrice).</summary>
    private string FmtAggregate(decimal amountInCurrentCurrency)
    {
        var settings = _svc.CurrencyService.Settings;
        if (settings.Currency == "RUB")
            return $"{amountInCurrentCurrency:N0} р.";
        return $"{amountInCurrentCurrency:N2} {settings.CurrencySymbol}";
    }

    private static Label MakeLabel(string text, Rectangle bounds, ContentAlignment align = ContentAlignment.MiddleLeft)
    {
        return new Label
        {
            Text = text,
            Font = UI.FontMed,
            ForeColor = UI.TextDark,
            Bounds = bounds,
            TextAlign = align,
            AutoEllipsis = true
        };
    }

    private void ExportReport()
    {
        if (_currentData.Count == 0)
        {
            MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "CSV файл|*.csv",
            FileName = $"Отчёт_{_dtpFrom.Value:dd.MM.yyyy}_{_dtpTo.Value:dd.MM.yyyy}.csv"
        };

        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var csv = _svc.ReportService.ExportToCsv(_currentData);
            File.WriteAllText(dlg.FileName, csv, System.Text.Encoding.UTF8);
            MessageBox.Show("Отчёт успешно экспортирован.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
