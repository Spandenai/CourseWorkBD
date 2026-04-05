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
    public partial class Requests : Form
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

        // Таблица с заявками и таблицы для выпадающих списков
        private readonly DataTable requestsTable = new DataTable();
        private readonly DataTable clientsLookupTable = new DataTable();
        private readonly DataTable contractsLookupTable = new DataTable();
        private readonly BindingSource bindingSource = new BindingSource();

        // Элементы таблицы и поиска
        private DataGridView dgvRequests = null!;
        private TextBox txtSearch = null!;
        private Label lblTotal = null!;

        // Поля карточки заявки
        private TextBox txtRequestId = null!;
        private ComboBox cmbClient = null!;
        private ComboBox cmbContract = null!;
        private DateTimePicker dtpRequestDate = null!;
        private ComboBox cmbRequestType = null!;
        private ComboBox cmbRequestStatus = null!;
        private TextBox txtDescription = null!;

        // Флаг для временного отключения SelectionChanged
        private bool suppressSelectionChanged = false;

        // Блок изображения
        private CoverPictureBox pictureBox = null!;

        // Путь к картинке формы
        private readonly string bannerPath =
            Path.Combine(Application.StartupPath, "Images", "requests_banner.png");

        public Requests()
        {
            InitializeComponent();

            // Убираем элементы дизайнера и строим интерфейс кодом
            Controls.Clear();

            InitializeForm();
            BuildInterface();
            FillRequestTypes();
            FillRequestStatuses();
            LoadBannerIfExists();
            LoadLookupData();
            LoadRequests();
        }

        // Первичная настройка формы
        private void InitializeForm()
        {
            Text = "Requests - Заявки";
            Name = "Requests";
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
                Text = "Заявки",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 28f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(420, 48)
            };

            var lblSubtitle = new Label
            {
                AutoSize = false,
                Text = "Управление обращениями клиентов интернет-провайдера",
                ForeColor = mutedTextColor,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                Location = new Point(2, 52),
                Size = new Size(760, 28)
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

        // Левая карточка со списком заявок
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
                Text = "Список заявок",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(280, 34)
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

            dgvRequests = new DataGridView
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
            dgvRequests.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvRequests.ColumnHeadersDefaultCellStyle.ForeColor = titleColor;
            dgvRequests.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
            dgvRequests.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvRequests.ColumnHeadersHeight = 46;
            dgvRequests.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            // Стиль строк таблицы
            dgvRequests.DefaultCellStyle.BackColor = Color.White;
            dgvRequests.DefaultCellStyle.ForeColor = textColor;
            dgvRequests.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvRequests.DefaultCellStyle.SelectionForeColor = titleColor;
            dgvRequests.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            dgvRequests.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

            dgvRequests.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            dgvRequests.RowTemplate.Height = 42;

            dgvRequests.SelectionChanged += DgvRequests_SelectionChanged;
            dgvRequests.DataBindingComplete += (s, e) => ConfigureGridColumns();

            card.Controls.Add(dgvRequests);
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

        // Карточка с полями заявки
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
                Text = "Карточка заявки",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold)
            };

            var formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 15,
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
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Поля ввода
            txtRequestId = CreateTextBox(true);
            cmbClient = CreateComboBox();
            cmbContract = CreateComboBox();
            dtpRequestDate = CreateDatePicker();
            cmbRequestType = CreateComboBox(true);
            cmbRequestStatus = CreateComboBox(true);
            txtDescription = CreateTextBox(multiline: true, height: 110);
            txtDescription.Dock = DockStyle.Fill;
            txtDescription.ScrollBars = ScrollBars.Vertical;

            formPanel.Controls.Add(CreateFieldLabel("ID заявки"), 0, 0);
            formPanel.Controls.Add(txtRequestId, 0, 1);

            formPanel.Controls.Add(CreateFieldLabel("Клиент"), 0, 2);
            formPanel.Controls.Add(cmbClient, 0, 3);

            formPanel.Controls.Add(CreateFieldLabel("Договор"), 0, 4);
            formPanel.Controls.Add(cmbContract, 0, 5);

            formPanel.Controls.Add(CreateFieldLabel("Дата заявки"), 0, 6);
            formPanel.Controls.Add(dtpRequestDate, 0, 7);

            formPanel.Controls.Add(CreateFieldLabel("Тип заявки"), 0, 8);
            formPanel.Controls.Add(cmbRequestType, 0, 9);

            formPanel.Controls.Add(CreateFieldLabel("Статус заявки"), 0, 10);
            formPanel.Controls.Add(cmbRequestStatus, 0, 11);

            formPanel.Controls.Add(CreateFieldLabel("Описание"), 0, 12);
            formPanel.Controls.Add(txtDescription, 0, 13);

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

            var btnAdd = CreateActionButton("Добавить", accentBlue, Color.White, (s, e) => AddRequest());
            var btnUpdate = CreateActionButton("Изменить", accentEmerald, Color.White, (s, e) => UpdateRequest());
            var btnDelete = CreateActionButton("Удалить", accentRose, Color.White, (s, e) => DeleteRequest());
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

        // Заполнение списка типов заявок
        private void FillRequestTypes()
        {
            cmbRequestType.Items.Clear();
            cmbRequestType.Items.AddRange(new object[]
            {
                "Подключение",
                "Техническая проблема",
                "Смена тарифа",
                "Оплата",
                "Оборудование",
                "Консультация",
                "Прочее"
            });

            cmbRequestType.Text = "Техническая проблема";
        }

        // Заполнение списка статусов заявок
        private void FillRequestStatuses()
        {
            cmbRequestStatus.Items.Clear();
            cmbRequestStatus.Items.AddRange(new object[]
            {
                "Новая",
                "В работе",
                "Выполнена",
                "Отменена"
            });

            cmbRequestStatus.Text = "Новая";
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
            LoadRequests();
        }

        // Подготовка структуры таблицы для списка договоров
        private void EnsureContractsLookupSchema()
        {
            if (contractsLookupTable.Columns.Count > 0)
                return;

            contractsLookupTable.Columns.Add("contract_id_text", typeof(string));
            contractsLookupTable.Columns.Add("display_name", typeof(string));
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

                var contractsTempTable = new DataTable();

                using (var contractsAdapter = new SqlDataAdapter(
                    @"SELECT
                        CAST(c.contract_id AS nvarchar(20)) AS contract_id_text,
                        CONCAT(
                            c.contract_number,
                            N' — ',
                            cl.last_name,
                            N' ',
                            cl.first_name,
                            CASE
                                WHEN cl.middle_name IS NULL OR LTRIM(RTRIM(cl.middle_name)) = N'' THEN N''
                                ELSE N' ' + cl.middle_name
                            END
                        ) AS display_name
                      FROM dbo.Contracts c
                      INNER JOIN dbo.Clients cl ON cl.client_id = c.client_id
                      ORDER BY c.contract_number", connection))
                {
                    contractsAdapter.Fill(contractsTempTable);
                }

                EnsureContractsLookupSchema();
                contractsLookupTable.Clear();
                contractsLookupTable.Rows.Add("", "Без договора");

                foreach (DataRow row in contractsTempTable.Rows)
                {
                    contractsLookupTable.Rows.Add(
                        row["contract_id_text"]?.ToString() ?? "",
                        row["display_name"]?.ToString() ?? "");
                }

                cmbClient.DataSource = null;
                cmbClient.DisplayMember = "display_name";
                cmbClient.ValueMember = "client_id";
                cmbClient.DataSource = clientsLookupTable;

                cmbContract.DataSource = null;
                cmbContract.DisplayMember = "display_name";
                cmbContract.ValueMember = "contract_id_text";
                cmbContract.DataSource = contractsLookupTable;

                if (clientsLookupTable.Rows.Count > 0)
                    cmbClient.SelectedIndex = 0;
                else
                    cmbClient.SelectedIndex = -1;

                cmbContract.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить списки клиентов и договоров.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Загрузка заявок из базы
        private void LoadRequests()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                using var adapter = new SqlDataAdapter(
                    @"SELECT
                        r.request_id,
                        r.client_id,
                        r.contract_id,
                        r.request_date,
                        r.request_type,
                        r.description,
                        r.request_status,
                        CONCAT(
                            cl.last_name,
                            N' ',
                            cl.first_name,
                            CASE
                                WHEN cl.middle_name IS NULL OR LTRIM(RTRIM(cl.middle_name)) = N'' THEN N''
                                ELSE N' ' + cl.middle_name
                            END
                        ) AS client_full_name,
                        ISNULL(c.contract_number, N'—') AS contract_number
                      FROM dbo.Requests r
                      INNER JOIN dbo.Clients cl ON cl.client_id = r.client_id
                      LEFT JOIN dbo.Contracts c ON c.contract_id = r.contract_id
                      ORDER BY r.request_date ASC, r.request_id ASC", connection);

                requestsTable.Clear();
                adapter.Fill(requestsTable);

                bindingSource.DataSource = requestsTable;
                dgvRequests.DataSource = bindingSource;

                UpdateTotalLabel();
                ClearInputs();

                if (dgvRequests.Rows.Count > 0)
                    SelectFirstRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить список заявок.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Настройка заголовков и ширины колонок
        private void ConfigureGridColumns()
        {
            if (dgvRequests.Columns.Count == 0)
                return;

            DataGridViewColumn? colRequestId = dgvRequests.Columns["request_id"];
            DataGridViewColumn? colClientId = dgvRequests.Columns["client_id"];
            DataGridViewColumn? colContractId = dgvRequests.Columns["contract_id"];
            DataGridViewColumn? colRequestDate = dgvRequests.Columns["request_date"];
            DataGridViewColumn? colRequestType = dgvRequests.Columns["request_type"];
            DataGridViewColumn? colDescription = dgvRequests.Columns["description"];
            DataGridViewColumn? colRequestStatus = dgvRequests.Columns["request_status"];
            DataGridViewColumn? colClientName = dgvRequests.Columns["client_full_name"];
            DataGridViewColumn? colContractNumber = dgvRequests.Columns["contract_number"];

            if (colRequestId == null ||
                colClientId == null ||
                colContractId == null ||
                colRequestDate == null ||
                colRequestType == null ||
                colDescription == null ||
                colRequestStatus == null ||
                colClientName == null ||
                colContractNumber == null)
                return;

            colRequestId.HeaderText = "ID";
            colRequestDate.HeaderText = "Дата заявки";
            colRequestType.HeaderText = "Тип заявки";
            colDescription.HeaderText = "Описание";
            colRequestStatus.HeaderText = "Статус";
            colClientName.HeaderText = "Клиент";
            colContractNumber.HeaderText = "Номер договора";

            colClientId.Visible = false;
            colContractId.Visible = false;

            colRequestId.FillWeight = 45;
            colRequestDate.FillWeight = 80;
            colRequestType.FillWeight = 110;
            colDescription.FillWeight = 180;
            colRequestStatus.FillWeight = 90;
            colClientName.FillWeight = 130;
            colContractNumber.FillWeight = 90;

            colRequestDate.DefaultCellStyle.Format = "dd.MM.yyyy";
            colRequestDate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
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
                    $"Convert(request_date, 'System.String') LIKE '%{search}%' " +
                    $"OR request_type LIKE '%{search}%' " +
                    $"OR description LIKE '%{search}%' " +
                    $"OR request_status LIKE '%{search}%' " +
                    $"OR client_full_name LIKE '%{search}%' " +
                    $"OR contract_number LIKE '%{search}%'";
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
            if (dgvRequests.Rows.Count == 0)
                return;

            var row = dgvRequests.Rows[0];
            DataGridViewCell? firstVisibleCell = null;

            foreach (DataGridViewColumn column in dgvRequests.Columns)
            {
                if (!column.Visible)
                    continue;

                firstVisibleCell = row.Cells[column.Index];
                break;
            }

            if (firstVisibleCell != null)
                dgvRequests.CurrentCell = firstVisibleCell;

            row.Selected = true;
        }

        // Заполнение полей справа при выборе строки
        private void DgvRequests_SelectionChanged(object? sender, EventArgs e)
        {
            if (suppressSelectionChanged)
                return;

            if (dgvRequests.CurrentRow?.DataBoundItem is not DataRowView rowView)
                return;

            txtRequestId.Text = rowView["request_id"]?.ToString() ?? "";
            cmbRequestType.Text = rowView["request_type"]?.ToString() ?? "";
            cmbRequestStatus.Text = rowView["request_status"]?.ToString() ?? "";
            txtDescription.Text = rowView["description"]?.ToString() ?? "";

            if (rowView["request_date"] != DBNull.Value &&
                DateTime.TryParse(rowView["request_date"]?.ToString(), out DateTime requestDate))
            {
                dtpRequestDate.Value = requestDate;
            }
            else
            {
                dtpRequestDate.Value = DateTime.Today;
            }

            SetComboSelectedValueSafe(cmbClient, rowView["client_id"]);
            SetContractComboSelectedValueSafe(rowView["contract_id"]);
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

        // Безопасная установка значения договора
        private void SetContractComboSelectedValueSafe(object? value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                {
                    cmbContract.SelectedValue = "";
                    return;
                }

                cmbContract.SelectedValue = value.ToString() ?? "";
            }
            catch
            {
                cmbContract.SelectedIndex = 0;
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

        // Получение необязательного ID договора
        private bool TryGetOptionalContractId(out int? contractId)
        {
            contractId = null;

            if (cmbContract.SelectedValue == null || cmbContract.SelectedValue == DBNull.Value)
                return true;

            string value = cmbContract.SelectedValue.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(value))
            {
                contractId = null;
                return true;
            }

            if (int.TryParse(value, out int parsedId))
            {
                contractId = parsedId;
                return true;
            }

            return false;
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

            if (!TryGetSelectedInt(cmbClient, out _))
            {
                MessageBox.Show("Выберите клиента.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbClient.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbRequestType.Text))
            {
                MessageBox.Show("Введите или выберите тип заявки.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRequestType.Focus();
                return false;
            }

            if (cmbRequestType.Text.Trim().Length > 100)
            {
                MessageBox.Show("Тип заявки не должен превышать 100 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRequestType.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbRequestStatus.Text))
            {
                MessageBox.Show("Введите или выберите статус заявки.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRequestStatus.Focus();
                return false;
            }

            if (cmbRequestStatus.Text.Trim().Length > 50)
            {
                MessageBox.Show("Статус заявки не должен превышать 50 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRequestStatus.Focus();
                return false;
            }

            if (!TryGetOptionalContractId(out _))
            {
                MessageBox.Show("Некорректно выбран договор.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbContract.Focus();
                return false;
            }

            return true;
        }

        // Добавление новой заявки
        private void AddRequest()
        {
            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Requests
                      (
                          client_id,
                          contract_id,
                          request_date,
                          request_type,
                          description,
                          request_status
                      )
                      VALUES
                      (
                          @client_id,
                          @contract_id,
                          @request_date,
                          @request_type,
                          @description,
                          @request_status
                      )", connection);

                FillRequestParameters(command);

                connection.Open();
                command.ExecuteNonQuery();

                LoadRequests();
                MessageBox.Show("Заявка успешно добавлена.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось добавить заявку.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Изменение выбранной заявки
        private void UpdateRequest()
        {
            if (string.IsNullOrWhiteSpace(txtRequestId.Text))
            {
                MessageBox.Show("Выберите заявку для изменения.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"UPDATE dbo.Requests
                      SET
                          client_id = @client_id,
                          contract_id = @contract_id,
                          request_date = @request_date,
                          request_type = @request_type,
                          description = @description,
                          request_status = @request_status
                      WHERE request_id = @request_id", connection);

                FillRequestParameters(command);
                command.Parameters.Add("@request_id", SqlDbType.Int).Value = int.Parse(txtRequestId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadRequests();
                MessageBox.Show("Данные заявки успешно обновлены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось изменить заявку.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Удаление выбранной заявки
        private void DeleteRequest()
        {
            if (string.IsNullOrWhiteSpace(txtRequestId.Text))
            {
                MessageBox.Show("Выберите заявку для удаления.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Удалить выбранную заявку?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"DELETE FROM dbo.Requests
                      WHERE request_id = @request_id", connection);

                command.Parameters.Add("@request_id", SqlDbType.Int).Value = int.Parse(txtRequestId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadRequests();
                MessageBox.Show("Заявка успешно удалена.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить заявку.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Заполнение параметров SQL-команды
        private void FillRequestParameters(SqlCommand command)
        {
            _ = TryGetOptionalContractId(out int? contractId);

            command.Parameters.Add("@client_id", SqlDbType.Int).Value = Convert.ToInt32(cmbClient.SelectedValue);

            var contractParameter = command.Parameters.Add("@contract_id", SqlDbType.Int);
            contractParameter.Value = contractId.HasValue ? contractId.Value : DBNull.Value;

            command.Parameters.Add("@request_date", SqlDbType.Date).Value = dtpRequestDate.Value.Date;
            command.Parameters.Add("@request_type", SqlDbType.NVarChar, 100).Value = cmbRequestType.Text.Trim();
            command.Parameters.Add("@description", SqlDbType.NVarChar).Value =
                string.IsNullOrWhiteSpace(txtDescription.Text) ? DBNull.Value : txtDescription.Text.Trim();
            command.Parameters.Add("@request_status", SqlDbType.NVarChar, 50).Value = cmbRequestStatus.Text.Trim();
        }

        // Очистка полей формы
        private void ClearInputs()
        {
            suppressSelectionChanged = true;

            try
            {
                dgvRequests.ClearSelection();
                dgvRequests.CurrentCell = null;

                txtRequestId.Text = "";
                dtpRequestDate.Value = DateTime.Today;
                cmbRequestType.Text = "Техническая проблема";
                cmbRequestStatus.Text = "Новая";
                txtDescription.Text = "";

                if (clientsLookupTable.Rows.Count > 0)
                    cmbClient.SelectedIndex = 0;
                else
                    cmbClient.SelectedIndex = -1;

                cmbContract.SelectedIndex = 0;
            }
            finally
            {
                suppressSelectionChanged = false;
            }

            cmbClient.Focus();
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