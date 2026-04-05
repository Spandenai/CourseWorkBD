using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace CourseWork
{
    public partial class Contracts : Form
    {
        // Строка подключения к базе данных
        private readonly string connectionString = @"Server=DESKTOP-1G427TO\SQLEXPRESS;Database=InternetProviderDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // Основные цвета формы
        private readonly Color formBackColor = Color.FromArgb(241, 245, 249);
        private readonly Color cardBackColor = Color.White;
        private readonly Color borderColor = Color.FromArgb(214, 223, 235);
        private readonly Color titleColor = Color.FromArgb(15, 23, 42);
        private readonly Color textColor = Color.FromArgb(51, 65, 85);
        private readonly Color mutedTextColor = Color.FromArgb(100, 116, 139);

        // Акцентные цвета кнопок
        private readonly Color accentBlue = Color.FromArgb(14, 165, 233);
        private readonly Color accentCyan = Color.FromArgb(6, 182, 212);
        private readonly Color accentEmerald = Color.FromArgb(16, 185, 129);
        private readonly Color accentAmber = Color.FromArgb(245, 158, 11);
        private readonly Color accentRose = Color.FromArgb(244, 63, 94);

        // Таблица с договорами и таблицы для выпадающих списков
        private readonly DataTable contractsTable = new DataTable();
        private readonly DataTable clientsLookupTable = new DataTable();
        private readonly DataTable tariffsLookupTable = new DataTable();
        private readonly BindingSource bindingSource = new BindingSource();

        // Элементы таблицы и поиска
        private DataGridView dgvContracts = null!;
        private TextBox txtSearch = null!;
        private Label lblTotal = null!;

        // Поля карточки договора
        private TextBox txtContractId = null!;
        private TextBox txtContractNumber = null!;
        private DateTimePicker dtpDateSigned = null!;
        private ComboBox cmbStatus = null!;
        private ComboBox cmbClient = null!;
        private ComboBox cmbTariff = null!;

        // Флаг для временного отключения SelectionChanged
        private bool suppressSelectionChanged = false;

        // Блок изображения
        private CoverPictureBox pictureBox = null!;

        // Путь к картинке формы
        private readonly string bannerPath =
            Path.Combine(Application.StartupPath, "Images", "contracts_banner.png");

        public Contracts()
        {
            InitializeComponent();

            // Убираем элементы дизайнера и строим интерфейс кодом
            Controls.Clear();

            InitializeForm();
            BuildInterface();
            FillStatuses();
            LoadBannerIfExists();
            LoadLookupData();
            LoadContracts();
        }

        // Первичная настройка формы
        private void InitializeForm()
        {
            Text = "Contracts - Договоры";
            Name = "Contracts";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1500, 940);
            Size = new Size(1800, 1150);
            BackColor = formBackColor;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            DoubleBuffered = true;
        }

        // Сборка всей формы
        private void BuildInterface()
        {
            SuspendLayout();

            var root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = formBackColor,
                Padding = new Padding(24)
            };

            var header = BuildHeader();
            var body = BuildBody();

            root.Controls.Add(body);
            root.Controls.Add(header);

            Controls.Add(root);

            ResumeLayout(false);
        }

        // Верхняя панель формы
        private Control BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 104,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                AutoSize = false,
                Text = "Договоры",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 28f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(420, 48)
            };

            var lblSubtitle = new Label
            {
                AutoSize = false,
                Text = "Управление договорами интернет-провайдера",
                ForeColor = mutedTextColor,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                Location = new Point(2, 52),
                Size = new Size(680, 28)
            };

            var btnRefresh = CreateActionButton("Обновить", accentBlue, Color.White, (s, e) => RefreshAllData());
            btnRefresh.Size = new Size(140, 44);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnBack = CreateActionButton("Назад", Color.White, titleColor, (s, e) => Close(), borderColor);
            btnBack.Size = new Size(120, 44);
            btnBack.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);
            header.Controls.Add(btnRefresh);
            header.Controls.Add(btnBack);

            // Расположение кнопок справа
            void RepositionButtons()
            {
                btnRefresh.Location = new Point(header.Width - btnRefresh.Width, 16);
                btnBack.Location = new Point(btnRefresh.Left - btnBack.Width - 12, 16);
            }

            header.Resize += (s, e) => RepositionButtons();
            RepositionButtons();

            return header;
        }

        // Основная часть формы
        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };

            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

            var leftColumn = BuildGridCard();
            var rightColumn = BuildRightColumn();

            body.Controls.Add(leftColumn, 0, 0);
            body.Controls.Add(rightColumn, 1, 0);

            return body;
        }

        // Левая карточка со списком договоров
        private Control BuildGridCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                BackColor = cardBackColor,
                BorderColor = borderColor,
                Radius = 28,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(22)
            };

            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 86,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                AutoSize = false,
                Text = "Список договоров",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(320, 34)
            };

            lblTotal = new Label
            {
                AutoSize = false,
                Text = "Записей: 0",
                ForeColor = mutedTextColor,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Location = new Point(2, 38),
                Size = new Size(200, 24)
            };

            var lblSearch = new Label
            {
                AutoSize = false,
                Text = "Поиск",
                ForeColor = textColor,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Size = new Size(70, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            txtSearch = CreateTextBox();
            txtSearch.Size = new Size(320, 40);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            topBar.Controls.Add(lblTitle);
            topBar.Controls.Add(lblTotal);
            topBar.Controls.Add(lblSearch);
            topBar.Controls.Add(txtSearch);

            // Расположение поиска
            void RepositionSearch()
            {
                txtSearch.Location = new Point(topBar.Width - txtSearch.Width, 18);
                lblSearch.Location = new Point(txtSearch.Left - 72, 26);
            }

            topBar.Resize += (s, e) => RepositionSearch();
            RepositionSearch();

            dgvContracts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = cardBackColor,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(232, 238, 245),
                EnableHeadersVisualStyles = false
            };

            // Стиль заголовков таблицы
            dgvContracts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvContracts.ColumnHeadersDefaultCellStyle.ForeColor = titleColor;
            dgvContracts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
            dgvContracts.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvContracts.ColumnHeadersHeight = 46;
            dgvContracts.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            // Стиль строк таблицы
            dgvContracts.DefaultCellStyle.BackColor = Color.White;
            dgvContracts.DefaultCellStyle.ForeColor = textColor;
            dgvContracts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvContracts.DefaultCellStyle.SelectionForeColor = titleColor;
            dgvContracts.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            dgvContracts.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

            dgvContracts.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            dgvContracts.RowTemplate.Height = 42;

            dgvContracts.SelectionChanged += DgvContracts_SelectionChanged;
            dgvContracts.DataBindingComplete += (s, e) => ConfigureGridColumns();

            card.Controls.Add(dgvContracts);
            card.Controls.Add(topBar);

            return card;
        }

        // Правая колонка: картинка и карточка редактирования
        private Control BuildRightColumn()
        {
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(14, 0, 0, 0)
            };

            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var imageCard = BuildImageCard();
            var editorCard = BuildEditorCard();

            right.Controls.Add(imageCard, 0, 0);
            right.Controls.Add(editorCard, 0, 1);

            return right;
        }

        // Карточка с изображением
        private Control BuildImageCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                BackColor = cardBackColor,
                BorderColor = borderColor,
                Radius = 28,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(0)
            };

            pictureBox = new CoverPictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(232, 240, 247)
            };

            card.Controls.Add(pictureBox);
            return card;
        }

        // Карточка с полями договора
        private Control BuildEditorCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                BackColor = cardBackColor,
                BorderColor = borderColor,
                Radius = 28,
                Margin = new Padding(0),
                Padding = new Padding(22)
            };

            var lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "Карточка договора",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold)
            };

            var formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 13,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Поля ввода
            txtContractId = CreateTextBox(true);
            txtContractNumber = CreateTextBox();
            dtpDateSigned = CreateDatePicker();
            cmbStatus = CreateComboBox(true);
            cmbClient = CreateComboBox();
            cmbTariff = CreateComboBox();

            formPanel.Controls.Add(CreateFieldLabel("ID договора"), 0, 0);
            formPanel.Controls.Add(txtContractId, 0, 1);

            formPanel.Controls.Add(CreateFieldLabel("Номер договора"), 0, 2);
            formPanel.Controls.Add(txtContractNumber, 0, 3);

            formPanel.Controls.Add(CreateFieldLabel("Дата заключения"), 0, 4);
            formPanel.Controls.Add(dtpDateSigned, 0, 5);

            formPanel.Controls.Add(CreateFieldLabel("Статус договора"), 0, 6);
            formPanel.Controls.Add(cmbStatus, 0, 7);

            formPanel.Controls.Add(CreateFieldLabel("Клиент"), 0, 8);
            formPanel.Controls.Add(cmbClient, 0, 9);

            formPanel.Controls.Add(CreateFieldLabel("Тариф"), 0, 10);
            formPanel.Controls.Add(cmbTariff, 0, 11);

            // Панель кнопок
            var buttons = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 122,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 0, 0)
            };

            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var btnAdd = CreateActionButton("Добавить", accentBlue, Color.White, (s, e) => AddContract());
            var btnUpdate = CreateActionButton("Изменить", accentEmerald, Color.White, (s, e) => UpdateContract());
            var btnDelete = CreateActionButton("Удалить", accentRose, Color.White, (s, e) => DeleteContract());
            var btnClear = CreateActionButton("Очистить", Color.White, titleColor, (s, e) => ClearInputs(), borderColor);

            buttons.Controls.Add(WrapButton(btnAdd), 0, 0);
            buttons.Controls.Add(WrapButton(btnUpdate), 1, 0);
            buttons.Controls.Add(WrapButton(btnDelete), 0, 1);
            buttons.Controls.Add(WrapButton(btnClear), 1, 1);

            card.Controls.Add(buttons);
            card.Controls.Add(formPanel);
            card.Controls.Add(lblTitle);

            return card;
        }

        // Обёртка для красивых отступов вокруг кнопки
        private Control WrapButton(Control button)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                BackColor = Color.Transparent
            };

            button.Dock = DockStyle.Fill;
            panel.Controls.Add(button);

            return panel;
        }

        // Подпись для поля
        private Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = textColor,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft,
                Margin = new Padding(0)
            };
        }

        // Создание текстового поля
        private TextBox CreateTextBox(bool readOnly = false, bool multiline = false, int height = 40)
        {
            var box = new TextBox
            {
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = titleColor,
                BackColor = readOnly ? Color.FromArgb(248, 250, 252) : Color.White,
                ReadOnly = readOnly,
                Multiline = multiline,
                Height = height,
                Margin = new Padding(0, 2, 0, 10)
            };

            return box;
        }

        // Создание выпадающего списка
        private ComboBox CreateComboBox(bool allowTyping = false)
        {
            var box = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = allowTyping ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = titleColor,
                BackColor = Color.White,
                Height = 40,
                Margin = new Padding(0, 2, 0, 10),
                IntegralHeight = false,
                MaxDropDownItems = 12
            };

            if (allowTyping)
            {
                box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                box.AutoCompleteSource = AutoCompleteSource.ListItems;
            }

            return box;
        }

        // Создание поля выбора даты
        private DateTimePicker CreateDatePicker()
        {
            return new DateTimePicker
            {
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                CalendarForeColor = titleColor,
                CalendarMonthBackground = Color.White,
                Height = 40,
                Margin = new Padding(0, 2, 0, 10),
                Value = DateTime.Today
            };
        }

        // Создание кнопки действия
        private Button CreateActionButton(
            string text,
            Color backColor,
            Color foreColor,
            EventHandler onClick,
            Color? customBorderColor = null)
        {
            var border = customBorderColor ?? backColor;

            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Height = 44,
                TabStop = false
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = border;

            // Эффект наведения
            button.MouseEnter += (s, e) =>
            {
                if (backColor == Color.White)
                    button.BackColor = Color.FromArgb(248, 250, 252);
                else
                    button.BackColor = ControlPaint.Light(backColor, 0.08f);
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = backColor;
            };

            button.Click += onClick;

            return button;
        }

        // Заполнение списка статусов
        private void FillStatuses()
        {
            cmbStatus.Items.Clear();
            cmbStatus.Items.AddRange(new object[]
            {
                "Активен",
                "На подключении",
                "Приостановлен",
                "Расторгнут"
            });

            cmbStatus.Text = "Активен";
        }

        // Загрузка картинки, если файл существует
        private void LoadBannerIfExists()
        {
            try
            {
                if (!File.Exists(bannerPath))
                    return;

                using var tempImage = Image.FromFile(bannerPath);
                pictureBox.Image = new Bitmap(tempImage);
            }
            catch
            {
                pictureBox.Image = null;
            }
        }

        // Полное обновление данных формы
        private void RefreshAllData()
        {
            LoadLookupData();
            LoadContracts();
        }

        // Загрузка данных для выпадающих списков
        private void LoadLookupData()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                using (var clientsAdapter = new SqlDataAdapter(
                    @"SELECT
                        client_id,
                        CONCAT(
                            last_name,
                            N' ',
                            first_name,
                            CASE
                                WHEN middle_name IS NULL OR LTRIM(RTRIM(middle_name)) = N'' THEN N''
                                ELSE N' ' + middle_name
                            END
                        ) AS display_name
                      FROM dbo.Clients
                      ORDER BY last_name, first_name, middle_name", connection))
                {
                    clientsLookupTable.Clear();
                    clientsAdapter.Fill(clientsLookupTable);
                }

                using (var tariffsAdapter = new SqlDataAdapter(
                    @"SELECT
                        tariff_id,
                        CONCAT(
                            tariff_name,
                            N' — ',
                            internet_speed,
                            N' • ',
                            CONVERT(nvarchar(20), CAST(monthly_fee AS decimal(10,2))),
                            N' ₽'
                        ) AS display_name
                      FROM dbo.Tariffs
                      ORDER BY tariff_name", connection))
                {
                    tariffsLookupTable.Clear();
                    tariffsAdapter.Fill(tariffsLookupTable);
                }

                cmbClient.DataSource = null;
                cmbClient.DisplayMember = "display_name";
                cmbClient.ValueMember = "client_id";
                cmbClient.DataSource = clientsLookupTable;

                cmbTariff.DataSource = null;
                cmbTariff.DisplayMember = "display_name";
                cmbTariff.ValueMember = "tariff_id";
                cmbTariff.DataSource = tariffsLookupTable;

                if (clientsLookupTable.Rows.Count > 0)
                    cmbClient.SelectedIndex = 0;
                else
                    cmbClient.SelectedIndex = -1;

                if (tariffsLookupTable.Rows.Count > 0)
                    cmbTariff.SelectedIndex = 0;
                else
                    cmbTariff.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить списки клиентов и тарифов.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Загрузка договоров из базы
        private void LoadContracts()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                using var adapter = new SqlDataAdapter(
                    @"SELECT
                        c.contract_id,
                        c.contract_number,
                        c.date_signed,
                        c.contract_status,
                        c.client_id,
                        c.tariff_id,
                        CONCAT(
                            cl.last_name,
                            N' ',
                            cl.first_name,
                            CASE
                                WHEN cl.middle_name IS NULL OR LTRIM(RTRIM(cl.middle_name)) = N'' THEN N''
                                ELSE N' ' + cl.middle_name
                            END
                        ) AS client_full_name,
                        t.tariff_name,
                        t.internet_speed,
                        t.monthly_fee
                      FROM dbo.Contracts c
                      INNER JOIN dbo.Clients cl ON cl.client_id = c.client_id
                      INNER JOIN dbo.Tariffs t ON t.tariff_id = c.tariff_id
                      ORDER BY c.date_signed ASC, c.contract_id ASC", connection);

                contractsTable.Clear();
                adapter.Fill(contractsTable);

                bindingSource.DataSource = contractsTable;
                dgvContracts.DataSource = bindingSource;

                UpdateTotalLabel();
                ClearInputs();

                if (dgvContracts.Rows.Count > 0)
                    SelectFirstRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить список договоров.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Настройка заголовков и ширины колонок
        private void ConfigureGridColumns()
        {
            if (dgvContracts.Columns.Count == 0)
                return;

            DataGridViewColumn? colContractId = dgvContracts.Columns["contract_id"];
            DataGridViewColumn? colContractNumber = dgvContracts.Columns["contract_number"];
            DataGridViewColumn? colDateSigned = dgvContracts.Columns["date_signed"];
            DataGridViewColumn? colStatus = dgvContracts.Columns["contract_status"];
            DataGridViewColumn? colClientId = dgvContracts.Columns["client_id"];
            DataGridViewColumn? colTariffId = dgvContracts.Columns["tariff_id"];
            DataGridViewColumn? colClientName = dgvContracts.Columns["client_full_name"];
            DataGridViewColumn? colTariffName = dgvContracts.Columns["tariff_name"];
            DataGridViewColumn? colInternetSpeed = dgvContracts.Columns["internet_speed"];
            DataGridViewColumn? colMonthlyFee = dgvContracts.Columns["monthly_fee"];

            if (colContractId == null ||
                colContractNumber == null ||
                colDateSigned == null ||
                colStatus == null ||
                colClientId == null ||
                colTariffId == null ||
                colClientName == null ||
                colTariffName == null ||
                colInternetSpeed == null ||
                colMonthlyFee == null)
                return;

            colContractId.HeaderText = "ID";
            colContractNumber.HeaderText = "Номер договора";
            colDateSigned.HeaderText = "Дата заключения";
            colStatus.HeaderText = "Статус";
            colClientName.HeaderText = "Клиент";
            colTariffName.HeaderText = "Тариф";
            colInternetSpeed.HeaderText = "Скорость интернета";
            colMonthlyFee.HeaderText = "Абонентская плата";

            colClientId.Visible = false;
            colTariffId.Visible = false;

            colContractId.FillWeight = 45;
            colContractNumber.FillWeight = 95;
            colDateSigned.FillWeight = 80;
            colStatus.FillWeight = 85;
            colClientName.FillWeight = 150;
            colTariffName.FillWeight = 110;
            colInternetSpeed.FillWeight = 100;
            colMonthlyFee.FillWeight = 95;

            colDateSigned.DefaultCellStyle.Format = "dd.MM.yyyy";
            colDateSigned.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            colMonthlyFee.DefaultCellStyle.Format = "N2";
            colMonthlyFee.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        // Фильтрация списка по поиску
        private void ApplyFilter()
        {
            if (bindingSource.DataSource == null)
                return;

            string search = EscapeFilterValue(txtSearch.Text.Trim());

            if (string.IsNullOrWhiteSpace(search))
            {
                bindingSource.RemoveFilter();
            }
            else
            {
                bindingSource.Filter =
                    $"contract_number LIKE '%{search}%' " +
                    $"OR Convert(date_signed, 'System.String') LIKE '%{search}%' " +
                    $"OR contract_status LIKE '%{search}%' " +
                    $"OR client_full_name LIKE '%{search}%' " +
                    $"OR tariff_name LIKE '%{search}%' " +
                    $"OR internet_speed LIKE '%{search}%' " +
                    $"OR Convert(monthly_fee, 'System.String') LIKE '%{search}%'";
            }

            UpdateTotalLabel();
        }

        // Экранирование специальных символов для фильтра
        private string EscapeFilterValue(string value)
        {
            return value
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        // Обновление текста с количеством записей
        private void UpdateTotalLabel()
        {
            lblTotal.Text = $"Записей: {bindingSource.Count}";
        }

        // Выбор первой строки после загрузки
        private void SelectFirstRow()
        {
            if (dgvContracts.Rows.Count == 0)
                return;

            var row = dgvContracts.Rows[0];
            DataGridViewCell? firstVisibleCell = null;

            foreach (DataGridViewCell cell in row.Cells)
            {
                DataGridViewColumn? column = cell.OwningColumn;

                if (column != null && column.Visible)
                {
                    firstVisibleCell = cell;
                    break;
                }
            }

            if (firstVisibleCell != null)
                dgvContracts.CurrentCell = firstVisibleCell;

            row.Selected = true;
        }

        // Заполнение полей справа при выборе строки
        private void DgvContracts_SelectionChanged(object? sender, EventArgs e)
        {
            if (suppressSelectionChanged)
                return;

            if (dgvContracts.CurrentRow?.DataBoundItem is not DataRowView rowView)
                return;

            txtContractId.Text = rowView["contract_id"]?.ToString() ?? "";
            txtContractNumber.Text = rowView["contract_number"]?.ToString() ?? "";
            cmbStatus.Text = rowView["contract_status"]?.ToString() ?? "";

            if (rowView["date_signed"] != DBNull.Value &&
                DateTime.TryParse(rowView["date_signed"]?.ToString(), out DateTime signedDate))
            {
                dtpDateSigned.Value = signedDate;
            }
            else
            {
                dtpDateSigned.Value = DateTime.Today;
            }

            SetComboSelectedValueSafe(cmbClient, rowView["client_id"]);
            SetComboSelectedValueSafe(cmbTariff, rowView["tariff_id"]);
        }

        // Безопасная установка выбранного значения ComboBox
        private void SetComboSelectedValueSafe(ComboBox comboBox, object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                comboBox.SelectedIndex = -1;
                return;
            }

            try
            {
                comboBox.SelectedValue = Convert.ToInt32(value);
            }
            catch
            {
                comboBox.SelectedIndex = -1;
            }
        }

        // Получение выбранного int из ComboBox
        private bool TryGetSelectedInt(ComboBox comboBox, out int value)
        {
            value = 0;

            if (comboBox.SelectedValue == null || comboBox.SelectedValue == DBNull.Value)
                return false;

            if (comboBox.SelectedValue is int intValue)
            {
                value = intValue;
                return true;
            }

            return int.TryParse(comboBox.SelectedValue.ToString(), out value);
        }

        // Проверка введённых данных
        private bool ValidateInputs()
        {
            if (clientsLookupTable.Rows.Count == 0)
            {
                MessageBox.Show(
                    "В базе нет клиентов.\nСначала добавьте хотя бы одного клиента.",
                    "Проверка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (tariffsLookupTable.Rows.Count == 0)
            {
                MessageBox.Show(
                    "В базе нет тарифов.\nСначала добавьте хотя бы один тариф.",
                    "Проверка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtContractNumber.Text))
            {
                MessageBox.Show("Введите номер договора.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContractNumber.Focus();
                return false;
            }

            if (txtContractNumber.Text.Trim().Length > 30)
            {
                MessageBox.Show("Номер договора не должен превышать 30 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContractNumber.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbStatus.Text))
            {
                MessageBox.Show("Введите или выберите статус договора.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbStatus.Focus();
                return false;
            }

            if (cmbStatus.Text.Trim().Length > 50)
            {
                MessageBox.Show("Статус договора не должен превышать 50 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbStatus.Focus();
                return false;
            }

            if (!TryGetSelectedInt(cmbClient, out _))
            {
                MessageBox.Show("Выберите клиента.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbClient.Focus();
                return false;
            }

            if (!TryGetSelectedInt(cmbTariff, out _))
            {
                MessageBox.Show("Выберите тариф.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbTariff.Focus();
                return false;
            }

            return true;
        }

        // Добавление нового договора
        private void AddContract()
        {
            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Contracts
                      (
                          contract_number,
                          date_signed,
                          contract_status,
                          client_id,
                          tariff_id
                      )
                      VALUES
                      (
                          @contract_number,
                          @date_signed,
                          @contract_status,
                          @client_id,
                          @tariff_id
                      )", connection);

                FillContractParameters(command);

                connection.Open();
                command.ExecuteNonQuery();

                LoadContracts();
                MessageBox.Show("Договор успешно добавлен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось добавить договор.\n\n" +
                    "Проверь, чтобы номер договора был уникальным.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Изменение выбранного договора
        private void UpdateContract()
        {
            if (string.IsNullOrWhiteSpace(txtContractId.Text))
            {
                MessageBox.Show("Выберите договор для изменения.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"UPDATE dbo.Contracts
                      SET
                          contract_number = @contract_number,
                          date_signed = @date_signed,
                          contract_status = @contract_status,
                          client_id = @client_id,
                          tariff_id = @tariff_id
                      WHERE contract_id = @contract_id", connection);

                FillContractParameters(command);
                command.Parameters.Add("@contract_id", SqlDbType.Int).Value = int.Parse(txtContractId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadContracts();
                MessageBox.Show("Данные договора успешно обновлены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось изменить договор.\n\n" +
                    "Проверь, чтобы номер договора оставался уникальным.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Удаление выбранного договора
        private void DeleteContract()
        {
            if (string.IsNullOrWhiteSpace(txtContractId.Text))
            {
                MessageBox.Show("Выберите договор для удаления.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Удалить выбранный договор?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"DELETE FROM dbo.Contracts
                      WHERE contract_id = @contract_id", connection);

                command.Parameters.Add("@contract_id", SqlDbType.Int).Value = int.Parse(txtContractId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadContracts();
                MessageBox.Show("Договор успешно удалён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить договор.\n\n" +
                    "Возможно, на него уже ссылаются платежи, оборудование или заявки.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Заполнение параметров SQL-команды
        private void FillContractParameters(SqlCommand command)
        {
            command.Parameters.Add("@contract_number", SqlDbType.NVarChar, 30).Value = txtContractNumber.Text.Trim();
            command.Parameters.Add("@date_signed", SqlDbType.Date).Value = dtpDateSigned.Value.Date;
            command.Parameters.Add("@contract_status", SqlDbType.NVarChar, 50).Value = cmbStatus.Text.Trim();
            command.Parameters.Add("@client_id", SqlDbType.Int).Value = Convert.ToInt32(cmbClient.SelectedValue);
            command.Parameters.Add("@tariff_id", SqlDbType.Int).Value = Convert.ToInt32(cmbTariff.SelectedValue);
        }

        // Очистка полей формы
        private void ClearInputs()
        {
            suppressSelectionChanged = true;

            try
            {
                dgvContracts.ClearSelection();
                dgvContracts.CurrentCell = null;

                txtContractId.Text = "";
                txtContractNumber.Text = "";
                dtpDateSigned.Value = DateTime.Today;
                cmbStatus.Text = "Активен";

                if (clientsLookupTable.Rows.Count > 0)
                    cmbClient.SelectedIndex = 0;
                else
                    cmbClient.SelectedIndex = -1;

                if (tariffsLookupTable.Rows.Count > 0)
                    cmbTariff.SelectedIndex = 0;
                else
                    cmbTariff.SelectedIndex = -1;
            }
            finally
            {
                suppressSelectionChanged = false;
            }

            txtContractNumber.Focus();
        }

        // Освобождение картинки при закрытии формы
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (pictureBox != null && pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            base.OnFormClosed(e);
        }

        // Кастомная карточка с закруглёнными углами
        [DesignerCategory("Code")]
        private class ModernCard : Panel
        {
            private int radius = 24;
            private Color borderColor = Color.FromArgb(214, 223, 235);
            private GraphicsPath? cachedPath;

            [Browsable(true)]
            [Category("Appearance")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public int Radius
            {
                get => radius;
                set
                {
                    radius = value < 1 ? 1 : value;
                    RebuildRegion();
                    Invalidate();
                }
            }

            [Browsable(true)]
            [Category("Appearance")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public Color BorderColor
            {
                get => borderColor;
                set
                {
                    borderColor = value;
                    Invalidate();
                }
            }

            public ModernCard()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                BackColor = Color.White;
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                RebuildRegion();
            }

            // Перестроение формы карточки при изменении размера
            private void RebuildRegion()
            {
                cachedPath?.Dispose();
                cachedPath = null;

                if (Width <= 1 || Height <= 1)
                    return;

                cachedPath = GetRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);

                Region?.Dispose();
                Region = new Region(cachedPath);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using var path = cachedPath != null
                    ? (GraphicsPath)cachedPath.Clone()
                    : GetRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);

                using var brush = new SolidBrush(BackColor);
                using var pen = new Pen(BorderColor, 1.4f);

                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    cachedPath?.Dispose();
                    Region?.Dispose();
                }

                base.Dispose(disposing);
            }

            // Создание прямоугольника с закруглёнными углами
            private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
            {
                int d = radius * 2;
                var path = new GraphicsPath();

                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();

                return path;
            }
        }

        // Кастомный блок изображения с режимом cover
        [DesignerCategory("Code")]
        private class CoverPictureBox : Control
        {
            private Image? image;

            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Image? Image
            {
                get => image;
                set
                {
                    image = value;
                    Invalidate();
                }
            }

            public CoverPictureBox()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;

                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint,
                    true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.Clear(BackColor);
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                Image? img = Image;

                if (img == null || Width <= 0 || Height <= 0)
                    return;

                float imageRatio = (float)img.Width / img.Height;
                float controlRatio = (float)Width / Height;

                RectangleF srcRect;

                if (imageRatio > controlRatio)
                {
                    float srcWidth = img.Height * controlRatio;
                    float srcX = (img.Width - srcWidth) / 2f;
                    srcRect = new RectangleF(srcX, 0, srcWidth, img.Height);
                }
                else
                {
                    float srcHeight = img.Width / controlRatio;
                    float srcY = (img.Height - srcHeight) / 2f;
                    srcRect = new RectangleF(0, srcY, img.Width, srcHeight);
                }

                e.Graphics.DrawImage(
                    img,
                    new Rectangle(0, 0, Width, Height),
                    srcRect,
                    GraphicsUnit.Pixel);
            }
        }
    }
}