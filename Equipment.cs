using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace CourseWork
{
    public partial class Equipment : Form
    {
        // Строка подключения к базе данных
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InternetProviderDB;Integrated Security=True;Connect Timeout=30;Encrypt=False";

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

        // Таблица с оборудованием и таблица для выпадающего списка договоров
        private readonly DataTable equipmentTable = new DataTable();
        private readonly DataTable contractsLookupTable = new DataTable();
        private readonly BindingSource bindingSource = new BindingSource();

        // Элементы таблицы и поиска
        private DataGridView dgvEquipment = null!;
        private TextBox txtSearch = null!;
        private Label lblTotal = null!;

        // Поля карточки оборудования
        private TextBox txtEquipmentId = null!;
        private TextBox txtEquipmentName = null!;
        private ComboBox cmbEquipmentType = null!;
        private TextBox txtSerialNumber = null!;
        private TextBox txtCost = null!;
        private ComboBox cmbEquipmentStatus = null!;
        private ComboBox cmbContract = null!;

        // Флаг для временного отключения SelectionChanged
        private bool suppressSelectionChanged = false;

        // Блок изображения
        private CoverPictureBox pictureBox = null!;

        // Путь к картинке формы
        private readonly string bannerPath =
            Path.Combine(Application.StartupPath, "Images", "equipment_banner.png");

        public Equipment()
        {
            InitializeComponent();

            // Убираем элементы дизайнера и строим интерфейс кодом
            Controls.Clear();

            InitializeForm();
            BuildInterface();
            FillEquipmentTypes();
            FillEquipmentStatuses();
            LoadBannerIfExists();
            LoadLookupData();
            LoadEquipment();
        }

        // Первичная настройка формы
        private void InitializeForm()
        {
            Text = "Equipment - Оборудование";
            Name = "Equipment";
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
                Text = "Оборудование",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 28f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(460, 48)
            };

            var lblSubtitle = new Label
            {
                AutoSize = false,
                Text = "Управление оборудованием, закреплённым за договорами",
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

        // Левая карточка со списком оборудования
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
                Text = "Список оборудования",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(340, 34)
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

            dgvEquipment = new DataGridView
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
            dgvEquipment.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvEquipment.ColumnHeadersDefaultCellStyle.ForeColor = titleColor;
            dgvEquipment.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
            dgvEquipment.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvEquipment.ColumnHeadersHeight = 46;
            dgvEquipment.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            // Стиль строк таблицы
            dgvEquipment.DefaultCellStyle.BackColor = Color.White;
            dgvEquipment.DefaultCellStyle.ForeColor = textColor;
            dgvEquipment.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvEquipment.DefaultCellStyle.SelectionForeColor = titleColor;
            dgvEquipment.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            dgvEquipment.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

            dgvEquipment.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            dgvEquipment.RowTemplate.Height = 42;

            dgvEquipment.SelectionChanged += DgvEquipment_SelectionChanged;
            dgvEquipment.DataBindingComplete += (s, e) => ConfigureGridColumns();

            card.Controls.Add(dgvEquipment);
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

        // Карточка с полями оборудования
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
                Text = "Карточка оборудования",
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
            txtEquipmentId = CreateTextBox(true);
            txtEquipmentName = CreateTextBox();
            cmbEquipmentType = CreateComboBox(true);
            txtSerialNumber = CreateTextBox();
            txtCost = CreateTextBox();
            cmbEquipmentStatus = CreateComboBox(true);
            cmbContract = CreateComboBox();

            formPanel.Controls.Add(CreateFieldLabel("ID оборудования"), 0, 0);
            formPanel.Controls.Add(txtEquipmentId, 0, 1);

            formPanel.Controls.Add(CreateFieldLabel("Название оборудования"), 0, 2);
            formPanel.Controls.Add(txtEquipmentName, 0, 3);

            formPanel.Controls.Add(CreateFieldLabel("Тип оборудования"), 0, 4);
            formPanel.Controls.Add(cmbEquipmentType, 0, 5);

            formPanel.Controls.Add(CreateFieldLabel("Серийный номер"), 0, 6);
            formPanel.Controls.Add(txtSerialNumber, 0, 7);

            formPanel.Controls.Add(CreateFieldLabel("Стоимость"), 0, 8);
            formPanel.Controls.Add(txtCost, 0, 9);

            formPanel.Controls.Add(CreateFieldLabel("Статус оборудования"), 0, 10);
            formPanel.Controls.Add(cmbEquipmentStatus, 0, 11);

            formPanel.Controls.Add(CreateFieldLabel("Договор"), 0, 12);
            formPanel.Controls.Add(cmbContract, 0, 13);

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

            var btnAdd = CreateActionButton("Добавить", accentBlue, Color.White, (s, e) => AddEquipment());
            var btnUpdate = CreateActionButton("Изменить", accentEmerald, Color.White, (s, e) => UpdateEquipment());
            var btnDelete = CreateActionButton("Удалить", accentRose, Color.White, (s, e) => DeleteEquipment());
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

        // Заполнение списка типов оборудования
        private void FillEquipmentTypes()
        {
            cmbEquipmentType.Items.Clear();
            cmbEquipmentType.Items.AddRange(new object[]
            {
                "Роутер",
                "Wi-Fi роутер",
                "Модем",
                "ONT-терминал",
                "TV-приставка",
                "Коммутатор"
            });

            cmbEquipmentType.Text = "Роутер";
        }

        // Заполнение списка статусов оборудования
        private void FillEquipmentStatuses()
        {
            cmbEquipmentStatus.Items.Clear();
            cmbEquipmentStatus.Items.AddRange(new object[]
            {
                "Выдано",
                "На складе",
                "На обслуживании",
                "Возвращено",
                "Списано"
            });

            cmbEquipmentStatus.Text = "Выдано";
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
            LoadEquipment();
        }

        // Загрузка данных для выпадающего списка договоров
        private void LoadLookupData()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                using var adapter = new SqlDataAdapter(
                    @"SELECT
                        c.contract_id,
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
                      ORDER BY c.contract_number", connection);

                contractsLookupTable.Clear();
                adapter.Fill(contractsLookupTable);

                cmbContract.DataSource = null;
                cmbContract.DisplayMember = "display_name";
                cmbContract.ValueMember = "contract_id";
                cmbContract.DataSource = contractsLookupTable;

                if (contractsLookupTable.Rows.Count > 0)
                    cmbContract.SelectedIndex = 0;
                else
                    cmbContract.SelectedIndex = -1;
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

        // Загрузка оборудования из базы
        private void LoadEquipment()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                using var adapter = new SqlDataAdapter(
                    @"SELECT
                        e.equipment_id,
                        e.equipment_name,
                        e.equipment_type,
                        e.serial_number,
                        e.cost,
                        e.equipment_status,
                        e.contract_id,
                        c.contract_number,
                        CONCAT(
                            cl.last_name,
                            N' ',
                            cl.first_name,
                            CASE
                                WHEN cl.middle_name IS NULL OR LTRIM(RTRIM(cl.middle_name)) = N'' THEN N''
                                ELSE N' ' + cl.middle_name
                            END
                        ) AS client_full_name
                      FROM dbo.Equipment e
                      INNER JOIN dbo.Contracts c ON c.contract_id = e.contract_id
                      INNER JOIN dbo.Clients cl ON cl.client_id = c.client_id
                      ORDER BY e.equipment_id ASC", connection);

                equipmentTable.Clear();
                adapter.Fill(equipmentTable);

                bindingSource.DataSource = equipmentTable;
                dgvEquipment.DataSource = bindingSource;

                UpdateTotalLabel();
                ClearInputs();

                if (dgvEquipment.Rows.Count > 0)
                    SelectFirstRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить список оборудования.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Настройка заголовков и ширины колонок
        private void ConfigureGridColumns()
        {
            if (dgvEquipment.Columns.Count == 0)
                return;

            DataGridViewColumn? colEquipmentId = dgvEquipment.Columns["equipment_id"];
            DataGridViewColumn? colEquipmentName = dgvEquipment.Columns["equipment_name"];
            DataGridViewColumn? colEquipmentType = dgvEquipment.Columns["equipment_type"];
            DataGridViewColumn? colSerialNumber = dgvEquipment.Columns["serial_number"];
            DataGridViewColumn? colCost = dgvEquipment.Columns["cost"];
            DataGridViewColumn? colEquipmentStatus = dgvEquipment.Columns["equipment_status"];
            DataGridViewColumn? colContractId = dgvEquipment.Columns["contract_id"];
            DataGridViewColumn? colContractNumber = dgvEquipment.Columns["contract_number"];
            DataGridViewColumn? colClientName = dgvEquipment.Columns["client_full_name"];

            if (colEquipmentId == null ||
                colEquipmentName == null ||
                colEquipmentType == null ||
                colSerialNumber == null ||
                colCost == null ||
                colEquipmentStatus == null ||
                colContractId == null ||
                colContractNumber == null ||
                colClientName == null)
                return;

            colEquipmentId.HeaderText = "ID";
            colEquipmentName.HeaderText = "Название";
            colEquipmentType.HeaderText = "Тип";
            colSerialNumber.HeaderText = "Серийный номер";
            colCost.HeaderText = "Стоимость";
            colEquipmentStatus.HeaderText = "Статус";
            colContractNumber.HeaderText = "Номер договора";
            colClientName.HeaderText = "Клиент";

            colContractId.Visible = false;

            colEquipmentId.FillWeight = 45;
            colEquipmentName.FillWeight = 110;
            colEquipmentType.FillWeight = 90;
            colSerialNumber.FillWeight = 120;
            colCost.FillWeight = 80;
            colEquipmentStatus.FillWeight = 95;
            colContractNumber.FillWeight = 95;
            colClientName.FillWeight = 140;

            colCost.DefaultCellStyle.Format = "N2";
            colCost.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
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
                    $"equipment_name LIKE '%{search}%' " +
                    $"OR equipment_type LIKE '%{search}%' " +
                    $"OR serial_number LIKE '%{search}%' " +
                    $"OR Convert(cost, 'System.String') LIKE '%{search}%' " +
                    $"OR equipment_status LIKE '%{search}%' " +
                    $"OR contract_number LIKE '%{search}%' " +
                    $"OR client_full_name LIKE '%{search}%'";
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
            if (dgvEquipment.Rows.Count == 0)
                return;

            var row = dgvEquipment.Rows[0];
            DataGridViewCell? firstVisibleCell = null;

            foreach (DataGridViewColumn column in dgvEquipment.Columns)
            {
                if (!column.Visible)
                    continue;

                firstVisibleCell = row.Cells[column.Index];
                break;
            }

            if (firstVisibleCell != null)
                dgvEquipment.CurrentCell = firstVisibleCell;

            row.Selected = true;
        }

        // Заполнение полей справа при выборе строки
        private void DgvEquipment_SelectionChanged(object? sender, EventArgs e)
        {
            if (suppressSelectionChanged)
                return;

            if (dgvEquipment.CurrentRow?.DataBoundItem is not DataRowView rowView)
                return;

            txtEquipmentId.Text = rowView["equipment_id"]?.ToString() ?? "";
            txtEquipmentName.Text = rowView["equipment_name"]?.ToString() ?? "";
            cmbEquipmentType.Text = rowView["equipment_type"]?.ToString() ?? "";
            txtSerialNumber.Text = rowView["serial_number"]?.ToString() ?? "";
            txtCost.Text = rowView["cost"]?.ToString() ?? "";
            cmbEquipmentStatus.Text = rowView["equipment_status"]?.ToString() ?? "";

            SetComboSelectedValueSafe(cmbContract, rowView["contract_id"]);
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

        // Преобразование текста в decimal
        private bool TryParseCost(string value, out decimal result)
        {
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
                return true;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
                return true;

            return false;
        }

        // Проверка введённых данных
        private bool ValidateInputs()
        {
            if (contractsLookupTable.Rows.Count == 0)
            {
                MessageBox.Show(
                    "В базе нет договоров.\nСначала добавьте хотя бы один договор.",
                    "Проверка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEquipmentName.Text))
            {
                MessageBox.Show("Введите название оборудования.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEquipmentName.Focus();
                return false;
            }

            if (txtEquipmentName.Text.Trim().Length > 100)
            {
                MessageBox.Show("Название оборудования не должно превышать 100 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEquipmentName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbEquipmentType.Text))
            {
                MessageBox.Show("Введите или выберите тип оборудования.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbEquipmentType.Focus();
                return false;
            }

            if (cmbEquipmentType.Text.Trim().Length > 50)
            {
                MessageBox.Show("Тип оборудования не должен превышать 50 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbEquipmentType.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSerialNumber.Text))
            {
                MessageBox.Show("Введите серийный номер.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSerialNumber.Focus();
                return false;
            }

            if (txtSerialNumber.Text.Trim().Length > 50)
            {
                MessageBox.Show("Серийный номер не должен превышать 50 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSerialNumber.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCost.Text))
            {
                MessageBox.Show("Введите стоимость оборудования.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCost.Focus();
                return false;
            }

            if (!TryParseCost(txtCost.Text.Trim(), out _))
            {
                MessageBox.Show("Стоимость оборудования должна быть числом.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCost.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbEquipmentStatus.Text))
            {
                MessageBox.Show("Введите или выберите статус оборудования.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbEquipmentStatus.Focus();
                return false;
            }

            if (cmbEquipmentStatus.Text.Trim().Length > 50)
            {
                MessageBox.Show("Статус оборудования не должен превышать 50 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbEquipmentStatus.Focus();
                return false;
            }

            if (!TryGetSelectedInt(cmbContract, out _))
            {
                MessageBox.Show("Выберите договор.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbContract.Focus();
                return false;
            }

            return true;
        }

        // Добавление нового оборудования
        private void AddEquipment()
        {
            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Equipment
                      (
                          equipment_name,
                          equipment_type,
                          serial_number,
                          cost,
                          equipment_status,
                          contract_id
                      )
                      VALUES
                      (
                          @equipment_name,
                          @equipment_type,
                          @serial_number,
                          @cost,
                          @equipment_status,
                          @contract_id
                      )", connection);

                FillEquipmentParameters(command);

                connection.Open();
                command.ExecuteNonQuery();

                LoadEquipment();
                MessageBox.Show("Оборудование успешно добавлено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось добавить оборудование.\n\n" +
                    "Проверь, чтобы серийный номер был уникальным.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Изменение выбранного оборудования
        private void UpdateEquipment()
        {
            if (string.IsNullOrWhiteSpace(txtEquipmentId.Text))
            {
                MessageBox.Show("Выберите оборудование для изменения.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"UPDATE dbo.Equipment
                      SET
                          equipment_name = @equipment_name,
                          equipment_type = @equipment_type,
                          serial_number = @serial_number,
                          cost = @cost,
                          equipment_status = @equipment_status,
                          contract_id = @contract_id
                      WHERE equipment_id = @equipment_id", connection);

                FillEquipmentParameters(command);
                command.Parameters.Add("@equipment_id", SqlDbType.Int).Value = int.Parse(txtEquipmentId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadEquipment();
                MessageBox.Show("Данные оборудования успешно обновлены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось изменить оборудование.\n\n" +
                    "Проверь, чтобы серийный номер оставался уникальным.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Удаление выбранного оборудования
        private void DeleteEquipment()
        {
            if (string.IsNullOrWhiteSpace(txtEquipmentId.Text))
            {
                MessageBox.Show("Выберите оборудование для удаления.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Удалить выбранное оборудование?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"DELETE FROM dbo.Equipment
                      WHERE equipment_id = @equipment_id", connection);

                command.Parameters.Add("@equipment_id", SqlDbType.Int).Value = int.Parse(txtEquipmentId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadEquipment();
                MessageBox.Show("Оборудование успешно удалено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить оборудование.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Заполнение параметров SQL-команды
        private void FillEquipmentParameters(SqlCommand command)
        {
            _ = TryParseCost(txtCost.Text.Trim(), out decimal cost);

            command.Parameters.Add("@equipment_name", SqlDbType.NVarChar, 100).Value = txtEquipmentName.Text.Trim();
            command.Parameters.Add("@equipment_type", SqlDbType.NVarChar, 50).Value = cmbEquipmentType.Text.Trim();
            command.Parameters.Add("@serial_number", SqlDbType.NVarChar, 50).Value = txtSerialNumber.Text.Trim();
            command.Parameters.Add("@cost", SqlDbType.Decimal).Value = cost;
            command.Parameters["@cost"].Precision = 10;
            command.Parameters["@cost"].Scale = 2;
            command.Parameters.Add("@equipment_status", SqlDbType.NVarChar, 50).Value = cmbEquipmentStatus.Text.Trim();
            command.Parameters.Add("@contract_id", SqlDbType.Int).Value = Convert.ToInt32(cmbContract.SelectedValue);
        }

        // Очистка полей формы
        private void ClearInputs()
        {
            suppressSelectionChanged = true;

            try
            {
                dgvEquipment.ClearSelection();
                dgvEquipment.CurrentCell = null;

                txtEquipmentId.Text = "";
                txtEquipmentName.Text = "";
                cmbEquipmentType.Text = "Роутер";
                txtSerialNumber.Text = "";
                txtCost.Text = "";
                cmbEquipmentStatus.Text = "Выдано";

                if (contractsLookupTable.Rows.Count > 0)
                    cmbContract.SelectedIndex = 0;
                else
                    cmbContract.SelectedIndex = -1;
            }
            finally
            {
                suppressSelectionChanged = false;
            }

            txtEquipmentName.Focus();
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