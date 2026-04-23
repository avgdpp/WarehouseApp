using WarehouseApp.Models;

namespace WarehouseApp.Forms;

/// <summary>Форма регистрации прихода товара (поставки) с импортом из файла</summary>
public class SupplyForm : Form
{
    private readonly AppServices _svc;
    private readonly int _userId;

    private TextBox _txtName = null!;
    private TextBox _txtSupplier = null!;
    private DateTimePicker _dtpDate = null!;
    private Panel _itemsPanel = null!;
    private Label _lblTotal = null!;

    private readonly List<SupplyItemEntry> _entries = new();

    public SupplyForm(AppServices svc, int userId)
    {
        _svc = svc;
        _userId = userId;
        Build();
    }

    private void Build()
    {
        Text = "Регистрация поставки";
        ClientSize = new Size(1080, 920);
        MinimumSize = new Size(1080, 920);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UI.BgLight;
        Font = UI.DefaultFont;
        AutoScaleMode = AutoScaleMode.None;

        var root = UI.CreatePanel(UI.BgLight); root.Dock = DockStyle.Fill; root.Padding = new Padding(28); Controls.Add(root);
        var card = UI.CreateRoundedPanel(UI.BgCard, 28); card.Dock = DockStyle.Fill; root.Controls.Add(card);
        var content = UI.CreateScrollPanel(Color.Transparent); content.Dock = DockStyle.Fill; card.Controls.Add(content);
        var footer = UI.CreatePanel(Color.Transparent); footer.Dock = DockStyle.Bottom; footer.Height = 90; card.Controls.Add(footer); footer.BringToFront();

        content.Controls.Add(new Label { Text = "Регистрация поставки", Font = UI.Px(26), ForeColor = UI.TextDark, Bounds = new Rectangle(38, 28, 460, 40), TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent });

        int labelX = 48, inputX = 290, inputW = 600, y = 88;

        y = AddField(content, "Название", out _txtName, labelX, inputX, inputW, y);
        int nextNum = _svc.SupplyService.GetAll().Count + 1;
        _txtName.Text = $"Поставка {nextNum}";

        y = AddField(content, "Поставщик", out _txtSupplier, labelX, inputX, inputW, y);

        content.Controls.Add(CreateLabel("Дата поставки", labelX, y + 10));
        var dateHost = UI.CreateRoundedPanel(UI.InputWhite, 16); dateHost.Bounds = new Rectangle(inputX, y, 300, 48); content.Controls.Add(dateHost);
        _dtpDate = new DateTimePicker { Font = UI.FontMed, Format = DateTimePickerFormat.Short, Value = DateTime.Today, CalendarForeColor = UI.TextDark };
        dateHost.Controls.Add(_dtpDate); dateHost.Resize += (_, _) => _dtpDate.Bounds = new Rectangle(14, 6, dateHost.Width - 28, 36);
        _dtpDate.Bounds = new Rectangle(14, 6, dateHost.Width - 28, 36); y += 58;

        // При смене даты поставки — пересчитываем отображение крайних сроков у всех позиций
        _dtpDate.ValueChanged += (_, _) => RebuildItemRows(content);

        // Секция позиций
        y += 10;
        content.Controls.Add(new Label { Text = "Позиции поставки", Font = UI.Px(20), ForeColor = UI.TextDark, Bounds = new Rectangle(38, y, 320, 32), BackColor = Color.Transparent });

        var btnAddItem = UI.CreatePillButton("+ Добавить", UI.BtnBlue, new Size(180, 40), UI.FontMed);
        btnAddItem.Location = new Point(inputX + inputW - 360, y); btnAddItem.Click += (_, _) => AddItemRow(content); content.Controls.Add(btnAddItem);

        var btnImport = UI.CreatePillButton("📁 Импорт из файла", UI.BtnOrange, new Size(240, 40), UI.FontMed);
        btnImport.Location = new Point(inputX + inputW - 170, y); btnImport.Click += (_, _) => ImportFromFile(content); content.Controls.Add(btnImport);
        y += 54;

        _itemsPanel = UI.CreatePanel(Color.Transparent); _itemsPanel.Bounds = new Rectangle(38, y, inputX + inputW - 38, 0); content.Controls.Add(_itemsPanel);

        _lblTotal = new Label { Text = $"Итого: {_svc.CurrencyService.FormatPrice(0)}", Font = UI.Px(20), ForeColor = UI.TextDark, AutoSize = true, BackColor = Color.Transparent };
        content.Controls.Add(_lblTotal);

        // Footer buttons
        var btnSave = UI.CreatePillButton("Сохранить", UI.BtnGreen, new Size(200, 52), UI.FontMedBold); btnSave.Click += BtnSave_Click; footer.Controls.Add(btnSave);
        var btnCancel = UI.CreatePillButton("Отмена", UI.TabInactive, new Size(180, 52), UI.FontMed); btnCancel.Click += (_, _) => Close(); footer.Controls.Add(btnCancel);

        void LayoutBtns()
        {
            int gap = 18, total = btnSave.Width + gap + btnCancel.Width;
            int sx = Math.Max(18, (footer.ClientSize.Width - total) / 2);
            int by = Math.Max(12, (footer.Height - btnSave.Height) / 2);
            btnSave.Location = new Point(sx, by); btnCancel.Location = new Point(btnSave.Right + gap, by);
        }
        footer.Resize += (_, _) => LayoutBtns(); footer.HandleCreated += (_, _) => LayoutBtns(); LayoutBtns();

        // Если нет товаров в каталоге — блокируем создание поставки
        bool hasProducts = _svc.ProductService.GetAll().Count > 0;
        if (!hasProducts)
        {
            btnAddItem.Enabled = false;
            btnImport.Enabled = false;
            btnSave.Enabled = false;
            _lblTotal.Visible = false;

            var noticePanel = UI.CreateRoundedPanel(Color.FromArgb(255, 243, 220), 14);
            noticePanel.Bounds = new Rectangle(38, _itemsPanel.Top + 10, inputX + inputW - 38, 72);
            content.Controls.Add(noticePanel);

            noticePanel.Controls.Add(new Label
            {
                Text = "⚠",
                Font = UI.Px(26),
                ForeColor = Color.FromArgb(180, 120, 0),
                Bounds = new Rectangle(20, 16, 40, 40),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            });

            var msg = new Label
            {
                Text = "Создание поставки недоступно: в каталоге нет ни одного товара.\r\nДобавьте товары в каталог, чтобы зарегистрировать поставку.",
                Font = UI.FontSmall,
                ForeColor = Color.FromArgb(120, 80, 0),
                Bounds = new Rectangle(68, 10, noticePanel.Width - 80, 52),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            noticePanel.Controls.Add(msg);
            noticePanel.Resize += (_, _) => msg.Width = noticePanel.Width - 80;
        }
        else
        {
            AddItemRow(content);
        }
    }

    private void ImportFromFile(Panel content)
    {
        using var dlg = new OpenFileDialog { Filter = "CSV файлы|*.csv|Все файлы|*.*", Title = "Импорт поставки из файла" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var lines = File.ReadAllLines(dlg.FileName, System.Text.Encoding.UTF8);
            if (lines.Length < 2) { MessageBox.Show("Файл пуст или содержит только заголовок.", "Ошибка импорта"); return; }

            var products = _svc.ProductService.GetAll();
            var settings = _svc.CurrencyService.Settings;
            int imported = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(';', ',');
                if (parts.Length < 3) continue;

                string nameOrArticle = parts[0].Trim().Trim('"');
                if (!int.TryParse(parts[1].Trim(), out int qty) || qty <= 0) continue;
                if (!decimal.TryParse(parts[2].Trim().Replace('.', ','), out decimal priceInput)) continue;
                // Цена в файле считается указанной в текущей валюте настроек —
                // это тот же контракт, что и при вводе через форму.
                decimal priceRub = settings.ConvertToRub(priceInput);

                // 4-я колонка: срок реализации — число + "д" (дни) или "м" (месяцы).
                // Также поддерживается старый формат — абсолютная дата.
                int periodValue = 0;
                SalePeriodUnit periodUnit = SalePeriodUnit.Months;
                bool hasPeriod = false;

                if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
                {
                    var raw = parts[3].Trim().Trim('"').ToLowerInvariant();
                    if (TryParsePeriodString(raw, out periodValue, out periodUnit))
                    {
                        hasPeriod = true;
                    }
                    else if (DateTime.TryParse(raw, out var absDate))
                    {
                        // Обратная совместимость: дата → пересчитываем в дни от даты поставки
                        int days = Math.Max(1, (absDate.Date - _dtpDate.Value.Date).Days);
                        periodValue = days;
                        periodUnit = SalePeriodUnit.Days;
                        hasPeriod = true;
                    }
                }

                var product = products.FirstOrDefault(p =>
                    p.Article.Equals(nameOrArticle, StringComparison.OrdinalIgnoreCase)
                    || p.Name.Equals(nameOrArticle, StringComparison.OrdinalIgnoreCase));

                if (product == null) continue;

                _entries.Add(new SupplyItemEntry
                {
                    Product = product,
                    Quantity = qty,
                    PurchasePrice = priceRub,
                    HasSalePeriod = hasPeriod,
                    SalePeriodValue = hasPeriod ? periodValue : 3,
                    SalePeriodUnit = hasPeriod ? periodUnit : SalePeriodUnit.Months
                });
                imported++;
            }

            RebuildItemRows(content);
            MessageBox.Show($"Импортировано {imported} позиций.", "Импорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка импорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool TryParsePeriodString(string raw, out int value, out SalePeriodUnit unit)
    {
        value = 0; unit = SalePeriodUnit.Months;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim().ToLowerInvariant();

        // Допустимые суффиксы: д/день/дней/d и м/мес/месяцев/m
        string numPart = new string(raw.TakeWhile(ch => char.IsDigit(ch)).ToArray());
        string suffix = raw[numPart.Length..].TrimStart();

        if (!int.TryParse(numPart, out value) || value <= 0) return false;

        if (suffix.StartsWith("д") || suffix.StartsWith("d"))
            unit = SalePeriodUnit.Days;
        else if (suffix.StartsWith("м") || suffix.StartsWith("m"))
            unit = SalePeriodUnit.Months;
        else if (suffix.Length == 0)
            unit = SalePeriodUnit.Days; // число без суффикса считаем днями
        else
            return false;

        return true;
    }

    private int AddField(Panel parent, string label, out TextBox txt, int labelX, int inputX, int inputW, int y)
    {
        parent.Controls.Add(CreateLabel(label, labelX, y + 10));
        var host = UI.CreateRoundedPanel(UI.InputWhite, 16); host.Bounds = new Rectangle(inputX, y, inputW, 48); parent.Controls.Add(host);
        var tb = new TextBox { BorderStyle = BorderStyle.None, Font = UI.FontMed, BackColor = UI.InputWhite, ForeColor = UI.TextDark };
        host.Controls.Add(tb); host.Resize += (_, _) => tb.Bounds = new Rectangle(18, 12, host.Width - 36, 24); tb.Bounds = new Rectangle(18, 12, host.Width - 36, 24);
        txt = tb; return y + 58;
    }

    private Label CreateLabel(string text, int x, int y) =>
        new() { Text = text, Font = UI.FontSmall, ForeColor = UI.TextDark, Bounds = new Rectangle(x, y, 210, 28), TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };

    private void AddItemRow(Panel content)
    {
        var products = _svc.ProductService.GetAll();
        if (products.Count == 0) return;
        _entries.Add(new SupplyItemEntry());
        RebuildItemRows(content, products);
    }

    private void RebuildItemRows(Panel content, List<Product>? products = null)
    {
        products ??= _svc.ProductService.GetAll();
        _itemsPanel.SuspendLayout(); _itemsPanel.Controls.Clear();
        int y = 0, rowW = _itemsPanel.Width;

        for (int idx = 0; idx < _entries.Count; idx++)
        {
            var entry = _entries[idx];
            var row = BuildItemRowPanel(entry, idx, rowW, products, content);
            row.Location = new Point(0, y); _itemsPanel.Controls.Add(row);
            y += row.Height + 4;
        }

        _itemsPanel.Height = y; _itemsPanel.ResumeLayout();
        int totalY = _itemsPanel.Top + Math.Max(y, 10) + 14;
        _lblTotal.Location = new Point(48, totalY);
        UpdateTotal();
    }

    private Panel BuildItemRowPanel(SupplyItemEntry entry, int idx, int rowW, List<Product> products, Panel content)
    {
        var row = UI.CreateRoundedPanel(UI.Surface, 12);
        row.Size = new Size(Math.Max(rowW, 600), 100);
        row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        row.Controls.Add(new Label { Text = $"{idx + 1}.", Font = UI.FontMedBold, ForeColor = UI.TextGray, Bounds = new Rectangle(10, 10, 28, 28), BackColor = Color.Transparent });

        row.Controls.Add(new Label { Text = "Товар", Font = UI.FontTiny, ForeColor = UI.TextGray, Bounds = new Rectangle(42, 4, 70, 18), BackColor = Color.Transparent });
        var cmbHost = UI.CreateRoundedPanel(UI.InputWhite, 10); cmbHost.Bounds = new Rectangle(42, 24, 280, 34); row.Controls.Add(cmbHost);
        var cmb = new ComboBox { Font = UI.Px(13), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Standard, BackColor = UI.InputWhite, ForeColor = UI.TextDark, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 28 };
        foreach (var p in products) cmb.Items.Add(p);
        cmb.DrawItem += (_, de) =>
        {
            if (de.Index < 0) return;
            de.DrawBackground();
            var prod = (Product)cmb.Items[de.Index];
            var extraParts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(prod.ExtraField1))
            {
                var parts = prod.ExtraField1.Contains("||")
                    ? prod.ExtraField1.Split("||")
                    : new[] { prod.ExtraField1 };
                extraParts.AddRange(parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            }
            if (!string.IsNullOrWhiteSpace(prod.ExtraField2) && !prod.ExtraField2.Contains("||"))
                extraParts.Add(prod.ExtraField2);
            string extra = string.Join(" / ", extraParts.Take(3));

            // Название слева (обрезается, если не помещается), характеристика справа.
            Font nameFont = de.Font ?? UI.Px(13);
            bool ownsNameFont = de.Font == null;
            using var extraFont = UI.Px(11);
            using var nameBrush = new SolidBrush(de.ForeColor);
            using var extraBrush = new SolidBrush(UI.TextGray);

            int padding = 6;
            int extraW = 0;
            if (!string.IsNullOrWhiteSpace(extra))
            {
                var extraSize = de.Graphics.MeasureString(extra, extraFont);
                extraW = (int)Math.Ceiling(extraSize.Width) + padding;
            }

            // Область имени — от левого края до начала области характеристики
            var nameRect = new RectangleF(
                de.Bounds.X + 4,
                de.Bounds.Y + (de.Bounds.Height - nameFont.Height) / 2f,
                Math.Max(20, de.Bounds.Width - extraW - 12),
                nameFont.Height + 2);
            using (var nameFormat = new StringFormat
                   {
                       Trimming = StringTrimming.EllipsisCharacter,
                       FormatFlags = StringFormatFlags.NoWrap,
                       LineAlignment = StringAlignment.Center
                   })
            {
                de.Graphics.DrawString(prod.Name, nameFont, nameBrush, nameRect, nameFormat);
            }

            if (!string.IsNullOrWhiteSpace(extra))
            {
                var extraRect = new RectangleF(
                    de.Bounds.Right - extraW - 4,
                    de.Bounds.Y + (de.Bounds.Height - extraFont.Height) / 2f,
                    extraW,
                    extraFont.Height + 2);
                using var extraFormat = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                de.Graphics.DrawString(extra, extraFont, extraBrush, extraRect, extraFormat);
            }

            de.DrawFocusRectangle();
            if (ownsNameFont) nameFont.Dispose();
        };
        if (entry.Product != null) { for (int i = 0; i < cmb.Items.Count; i++) { if (((Product)cmb.Items[i]).Id == entry.Product.Id) { cmb.SelectedIndex = i; break; } } }
        cmb.SelectedIndexChanged += (_, _) => { entry.Product = cmb.SelectedItem as Product; if (entry.Product != null && entry.PurchasePrice == 0) entry.PurchasePrice = entry.Product.PurchasePrice; RebuildItemRows(content, products); };
        cmbHost.Controls.Add(cmb); cmbHost.Resize += (_, _) => cmb.Bounds = new Rectangle(4, 2, cmbHost.Width - 8, 30); cmb.Bounds = new Rectangle(4, 2, cmbHost.Width - 8, 30);

        row.Controls.Add(new Label { Text = "Кол-во", Font = UI.FontTiny, ForeColor = UI.TextGray, Bounds = new Rectangle(332, 4, 70, 18), BackColor = Color.Transparent });
        var qtyHost = UI.CreateRoundedPanel(UI.InputWhite, 10); qtyHost.Bounds = new Rectangle(332, 24, 84, 34); row.Controls.Add(qtyHost);
        var numQty = new NumericUpDown { Font = UI.Px(15), Minimum = 1, Maximum = 999999, Value = Math.Max(1, entry.Quantity), BorderStyle = BorderStyle.None, BackColor = UI.InputWhite, ForeColor = UI.TextDark };
        numQty.ValueChanged += (_, _) => { entry.Quantity = (int)numQty.Value; UpdateTotal(); };
        qtyHost.Controls.Add(numQty); numQty.Bounds = new Rectangle(8, 6, qtyHost.Width - 16, 22);

        row.Controls.Add(new Label { Text = $"Цена закуп. ({_svc.CurrencyService.Settings.CurrencySymbol})", Font = UI.FontTiny, ForeColor = UI.TextGray, Bounds = new Rectangle(426, 4, 120, 18), BackColor = Color.Transparent });
        var priceHost = UI.CreateRoundedPanel(UI.InputWhite, 10); priceHost.Bounds = new Rectangle(426, 24, 110, 34); row.Controls.Add(priceHost);
        // entry.PurchasePrice хранится в рублях (единица измерения внутренней модели) —
        // для отображения пересчитываем в текущую валюту, а при вводе конвертируем обратно.
        var settings = _svc.CurrencyService.Settings;
        var initialDisplayPrice = settings.ConvertFromRub(entry.PurchasePrice);
        var numPrice = new NumericUpDown { Font = UI.Px(15), Minimum = 0, Maximum = 999999999, DecimalPlaces = 2, Value = initialDisplayPrice, BorderStyle = BorderStyle.None, BackColor = UI.InputWhite, ForeColor = UI.TextDark, ThousandsSeparator = true };
        numPrice.ValueChanged += (_, _) => { entry.PurchasePrice = settings.ConvertToRub(numPrice.Value); UpdateTotal(); };
        priceHost.Controls.Add(numPrice); numPrice.Bounds = new Rectangle(8, 6, priceHost.Width - 16, 22);

        // ---- Срок реализации: чекбокс + число + единица (дни/месяцы) ----
        row.Controls.Add(new Label { Text = "Срок реализации", Font = UI.FontTiny, ForeColor = UI.TextGray, Bounds = new Rectangle(42, 64, 140, 18), BackColor = Color.Transparent });
        var chk = new CheckBox { Text = "", Checked = entry.HasSalePeriod, Bounds = new Rectangle(186, 66, 20, 20), BackColor = Color.Transparent }; row.Controls.Add(chk);

        var valueHost = UI.CreateRoundedPanel(UI.InputWhite, 10); valueHost.Bounds = new Rectangle(212, 62, 70, 30); valueHost.Visible = entry.HasSalePeriod; row.Controls.Add(valueHost);
        var numPeriod = new NumericUpDown { Font = UI.Px(14), Minimum = 1, Maximum = 3650, Value = Math.Max(1, entry.SalePeriodValue), BorderStyle = BorderStyle.None, BackColor = UI.InputWhite, ForeColor = UI.TextDark };
        valueHost.Controls.Add(numPeriod); numPeriod.Bounds = new Rectangle(6, 4, valueHost.Width - 12, 22);

        var unitHost = UI.CreateRoundedPanel(UI.InputWhite, 10); unitHost.Bounds = new Rectangle(288, 62, 100, 30); unitHost.Visible = entry.HasSalePeriod; row.Controls.Add(unitHost);
        var cmbUnit = new ComboBox { Font = UI.Px(13), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = UI.InputWhite, ForeColor = UI.TextDark };
        cmbUnit.Items.Add("дней");
        cmbUnit.Items.Add("месяцев");
        cmbUnit.SelectedIndex = entry.SalePeriodUnit == SalePeriodUnit.Days ? 0 : 1;
        unitHost.Controls.Add(cmbUnit); cmbUnit.Bounds = new Rectangle(4, 2, unitHost.Width - 8, 26);

        // Подсказка: вычисленная дата окончания
        var lblDeadline = new Label { Text = "", Font = UI.FontTiny, ForeColor = UI.TextGray, Bounds = new Rectangle(394, 66, 240, 20), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft };
        row.Controls.Add(lblDeadline);

        void UpdateDeadlineHint()
        {
            if (!entry.HasSalePeriod) { lblDeadline.Text = ""; return; }
            var deadline = ComputeDeadline(_dtpDate.Value.Date, entry.SalePeriodValue, entry.SalePeriodUnit);
            lblDeadline.Text = deadline.HasValue ? $"→ до {deadline.Value:dd.MM.yyyy}" : "";
        }

        numPeriod.ValueChanged += (_, _) => { entry.SalePeriodValue = (int)numPeriod.Value; UpdateDeadlineHint(); };
        cmbUnit.SelectedIndexChanged += (_, _) => { entry.SalePeriodUnit = cmbUnit.SelectedIndex == 0 ? SalePeriodUnit.Days : SalePeriodUnit.Months; UpdateDeadlineHint(); };
        chk.CheckedChanged += (_, _) =>
        {
            entry.HasSalePeriod = chk.Checked;
            valueHost.Visible = chk.Checked;
            unitHost.Visible = chk.Checked;
            UpdateDeadlineHint();
        };
        UpdateDeadlineHint();

        // Сумма по строке — pure UI display, конвертация через CurrencyService
        decimal subRub = entry.PurchasePrice * entry.Quantity;
        row.Controls.Add(new Label { Text = _svc.CurrencyService.FormatPrice(subRub), Font = UI.FontMedBold, ForeColor = UI.TextDark, Bounds = new Rectangle(row.Width - 180, 64, 130, 28), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right });

        var btnRemove = UI.CreateCircleButton("✕", UI.BtnRed, 32, UI.Px(14)); btnRemove.Location = new Point(row.Width - 44, 8);
        btnRemove.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRemove.Click += (_, _) => { _entries.Remove(entry); RebuildItemRows(content, products); };
        row.Controls.Add(btnRemove);

        return row;
    }

    /// <summary>Вычисление крайней даты реализации по заданному периоду</summary>
    private static DateTime? ComputeDeadline(DateTime supplyDate, int periodValue, SalePeriodUnit unit)
    {
        if (periodValue <= 0) return null;
        return unit == SalePeriodUnit.Days
            ? supplyDate.AddDays(periodValue)
            : supplyDate.AddMonths(periodValue);
    }

    private void UpdateTotal()
    {
        decimal totalRub = _entries.Sum(e => e.PurchasePrice * e.Quantity);
        _lblTotal.Text = $"Итого: {_svc.CurrencyService.FormatPrice(totalRub)}";
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("Поле «Название» обязательно.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); _txtName.Focus(); return; }

        if (_dtpDate.Value.Date > DateTime.Today) { MessageBox.Show("Дата поставки не может быть в будущем.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        string supplyName = _txtName.Text.Trim();
        if (_svc.SupplyService.GetAll().Any(s => s.Name.Equals(supplyName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"Поставка с названием «{supplyName}» уже существует. Укажите другое название.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus(); return;
        }

        if (_entries.Count == 0) { MessageBox.Show("Добавьте хотя бы одну позицию.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var items = new List<(int ProductId, decimal PurchasePrice, int Quantity, DateTime? SaleDeadline)>();
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.Product == null) { MessageBox.Show($"Выберите товар в строке {i + 1}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            DateTime? deadline = null;
            if (entry.HasSalePeriod)
            {
                deadline = ComputeDeadline(_dtpDate.Value.Date, entry.SalePeriodValue, entry.SalePeriodUnit);
                if (deadline == null)
                {
                    MessageBox.Show($"Некорректный срок реализации в строке {i + 1}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            items.Add((entry.Product.Id, entry.PurchasePrice, entry.Quantity, deadline));
        }

        var duplicates = items.GroupBy(x => (x.ProductId, x.PurchasePrice)).Where(g => g.Count() > 1).ToList();
        if (duplicates.Any())
        {
            var dup = duplicates.First();
            var prodName = _entries.First(e => e.Product?.Id == dup.Key.ProductId).Product?.Name;
            MessageBox.Show($"Товар «{prodName}» с одинаковой ценой добавлен несколько раз. Объедините позиции.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = _svc.SupplyService.CreateSupply(supplyName, _txtSupplier.Text.Trim(), _dtpDate.Value, items, _userId);
        if (!result.Success) { MessageBox.Show(result.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        MessageBox.Show(result.Message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK; Close();
    }
}

public enum SalePeriodUnit { Days, Months }

internal class SupplyItemEntry
{
    public Product? Product { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal PurchasePrice { get; set; }
    public bool HasSalePeriod { get; set; }
    public int SalePeriodValue { get; set; } = 3;
    public SalePeriodUnit SalePeriodUnit { get; set; } = SalePeriodUnit.Months;
}
