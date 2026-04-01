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
    public partial class Clients : Form
    {
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InternetProviderDB;Integrated Security=True;Connect Timeout=30;Encrypt=False";

        // Палитра
        private readonly Color formBackColor = Color.FromArgb(241, 245, 249);
        private readonly Color cardBackColor = Color.White;
        private readonly Color borderColor = Color.FromArgb(214, 223, 235);
        private readonly Color titleColor = Color.FromArgb(15, 23, 42);
        private readonly Color textColor = Color.FromArgb(51, 65, 85);
        private readonly Color mutedTextColor = Color.FromArgb(100, 116, 139);

        private readonly Color accentBlue = Color.FromArgb(14, 165, 233);
        private readonly Color accentCyan = Color.FromArgb(6, 182, 212);
        private readonly Color accentEmerald = Color.FromArgb(16, 185, 129);
        private readonly Color accentAmber = Color.FromArgb(245, 158, 11);
        private readonly Color accentRose = Color.FromArgb(244, 63, 94);

        private readonly DataTable clientsTable = new DataTable();
        private readonly BindingSource bindingSource = new BindingSource();

        private DataGridView dgvClients = null!;
        private TextBox txtSearch = null!;
        private Label lblTotal = null!;

        private TextBox txtClientId = null!;
        private TextBox txtLastName = null!;
        private TextBox txtFirstName = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtPhone = null!;
        private TextBox txtEmail = null!;
        private TextBox txtAddress = null!;
        private bool suppressSelectionChanged = false;

        private CoverPictureBox pictureBox = null!;

        private readonly string bannerPath =
            Path.Combine(Application.StartupPath, "Images", "clients_banner.png");

        public Clients()
        {
            InitializeComponent();

            Controls.Clear();

            InitializeForm();
            BuildInterface();
            LoadBannerIfExists();
            LoadClients();
        }

        private void InitializeForm()
        {
            Text = "Clients - Клиенты";
            Name = "Clients";
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
                Text = "Клиенты",
                ForeColor = titleColor,
                Font = new Font("Segoe UI Semibold", 28f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(420, 48)
            };

            var lblSubtitle = new Label
            {
                AutoSize = false,
                Text = "Управление клиентской базой интернет-провайдера",
                ForeColor = mutedTextColor,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                Location = new Point(2, 52),
                Size = new Size(620, 28)
            };

            var btnRefresh = CreateActionButton("Обновить", accentBlue, Color.White, (s, e) => LoadClients());
            btnRefresh.Size = new Size(140, 44);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnBack = CreateActionButton("Назад", Color.White, titleColor, (s, e) => Close(), borderColor);
            btnBack.Size = new Size(120, 44);
            btnBack.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);
            header.Controls.Add(btnRefresh);
            header.Controls.Add(btnBack);

            void RepositionButtons()
            {
                btnRefresh.Location = new Point(header.Width - btnRefresh.Width, 16);
                btnBack.Location = new Point(btnRefresh.Left - btnBack.Width - 12, 16);
            }

            header.Resize += (s, e) => RepositionButtons();
            RepositionButtons();

            return header;
        }

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
                Text = "Список клиентов",
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

            void RepositionSearch()
            {
                txtSearch.Location = new Point(topBar.Width - txtSearch.Width, 18);
                lblSearch.Location = new Point(txtSearch.Left - 72, 26);
            }

            topBar.Resize += (s, e) => RepositionSearch();
            RepositionSearch();

            dgvClients = new DataGridView
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

            dgvClients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = titleColor;
            dgvClients.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
            dgvClients.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvClients.ColumnHeadersHeight = 46;
            dgvClients.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgvClients.DefaultCellStyle.BackColor = Color.White;
            dgvClients.DefaultCellStyle.ForeColor = textColor;
            dgvClients.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvClients.DefaultCellStyle.SelectionForeColor = titleColor;
            dgvClients.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            dgvClients.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

            dgvClients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            dgvClients.RowTemplate.Height = 42;

            dgvClients.SelectionChanged += DgvClients_SelectionChanged;
            dgvClients.DataBindingComplete += (s, e) => ConfigureGridColumns();

            card.Controls.Add(dgvClients);
            card.Controls.Add(topBar);

            return card;
        }

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
                Text = "Карточка клиента",
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

            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // ID label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // ID box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // Фамилия label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // Фамилия box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // Имя label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // Имя box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // Отчество label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // Отчество box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // Телефон label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // Телефон box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // Email label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // Email box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // Адрес label
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));  // Адрес box
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Остаток

            txtClientId = CreateTextBox(true);
            txtLastName = CreateTextBox();
            txtFirstName = CreateTextBox();
            txtMiddleName = CreateTextBox();
            txtPhone = CreateTextBox();
            txtEmail = CreateTextBox();
            txtAddress = CreateTextBox(multiline: true, height: 96);
            txtAddress.Dock = DockStyle.Fill;
            txtAddress.ScrollBars = ScrollBars.Vertical;

            formPanel.Controls.Add(CreateFieldLabel("ID клиента"), 0, 0);
            formPanel.Controls.Add(txtClientId, 0, 1);

            formPanel.Controls.Add(CreateFieldLabel("Фамилия"), 0, 2);
            formPanel.Controls.Add(txtLastName, 0, 3);

            formPanel.Controls.Add(CreateFieldLabel("Имя"), 0, 4);
            formPanel.Controls.Add(txtFirstName, 0, 5);

            formPanel.Controls.Add(CreateFieldLabel("Отчество"), 0, 6);
            formPanel.Controls.Add(txtMiddleName, 0, 7);

            formPanel.Controls.Add(CreateFieldLabel("Телефон"), 0, 8);
            formPanel.Controls.Add(txtPhone, 0, 9);

            formPanel.Controls.Add(CreateFieldLabel("Email"), 0, 10);
            formPanel.Controls.Add(txtEmail, 0, 11);

            formPanel.Controls.Add(CreateFieldLabel("Адрес подключения"), 0, 12);
            formPanel.Controls.Add(txtAddress, 0, 13);

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

            var btnAdd = CreateActionButton("Добавить", accentBlue, Color.White, (s, e) => AddClient());
            var btnUpdate = CreateActionButton("Изменить", accentEmerald, Color.White, (s, e) => UpdateClient());
            var btnDelete = CreateActionButton("Удалить", accentRose, Color.White, (s, e) => DeleteClient());
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

        private void LoadClients()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                using var adapter = new SqlDataAdapter(
                    @"SELECT 
                        client_id,
                        last_name,
                        first_name,
                        middle_name,
                        phone,
                        email,
                        connection_address
                      FROM dbo.Clients
                      ORDER BY last_name, first_name, middle_name", connection);

                clientsTable.Clear();
                adapter.Fill(clientsTable);

                bindingSource.DataSource = clientsTable;
                dgvClients.DataSource = bindingSource;

                UpdateTotalLabel();
                ClearInputs();

                if (dgvClients.Rows.Count > 0)
                    dgvClients.Rows[0].Selected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить список клиентов.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ConfigureGridColumns()
        {
            if (dgvClients.Columns.Count == 0)
                return;

            dgvClients.Columns["client_id"].HeaderText = "ID";
            dgvClients.Columns["last_name"].HeaderText = "Фамилия";
            dgvClients.Columns["first_name"].HeaderText = "Имя";
            dgvClients.Columns["middle_name"].HeaderText = "Отчество";
            dgvClients.Columns["phone"].HeaderText = "Телефон";
            dgvClients.Columns["email"].HeaderText = "Email";
            dgvClients.Columns["connection_address"].HeaderText = "Адрес подключения";

            dgvClients.Columns["client_id"].FillWeight = 50;
            dgvClients.Columns["last_name"].FillWeight = 95;
            dgvClients.Columns["first_name"].FillWeight = 85;
            dgvClients.Columns["middle_name"].FillWeight = 95;
            dgvClients.Columns["phone"].FillWeight = 90;
            dgvClients.Columns["email"].FillWeight = 120;
            dgvClients.Columns["connection_address"].FillWeight = 170;
        }

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
                    $"last_name LIKE '%{search}%' " +
                    $"OR first_name LIKE '%{search}%' " +
                    $"OR middle_name LIKE '%{search}%' " +
                    $"OR phone LIKE '%{search}%' " +
                    $"OR email LIKE '%{search}%' " +
                    $"OR connection_address LIKE '%{search}%'";
            }

            UpdateTotalLabel();
        }

        private string EscapeFilterValue(string value)
        {
            return value
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        private void UpdateTotalLabel()
        {
            lblTotal.Text = $"Записей: {bindingSource.Count}";
        }

        private void DgvClients_SelectionChanged(object? sender, EventArgs e)
        {
            if (suppressSelectionChanged)
                return;

            if (dgvClients.CurrentRow?.DataBoundItem is not DataRowView rowView)
                return;

            txtClientId.Text = rowView["client_id"]?.ToString() ?? "";
            txtLastName.Text = rowView["last_name"]?.ToString() ?? "";
            txtFirstName.Text = rowView["first_name"]?.ToString() ?? "";
            txtMiddleName.Text = rowView["middle_name"]?.ToString() ?? "";
            txtPhone.Text = rowView["phone"]?.ToString() ?? "";
            txtEmail.Text = rowView["email"]?.ToString() ?? "";
            txtAddress.Text = rowView["connection_address"]?.ToString() ?? "";
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Введите фамилию клиента.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLastName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Введите имя клиента.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Введите телефон клиента.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                MessageBox.Show("Введите адрес подключения.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAddress.Focus();
                return false;
            }

            return true;
        }

        private void AddClient()
        {
            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Clients
                      (
                          last_name,
                          first_name,
                          middle_name,
                          phone,
                          email,
                          connection_address
                      )
                      VALUES
                      (
                          @last_name,
                          @first_name,
                          @middle_name,
                          @phone,
                          @email,
                          @connection_address
                      )", connection);

                FillClientParameters(command);

                connection.Open();
                command.ExecuteNonQuery();

                LoadClients();
                MessageBox.Show("Клиент успешно добавлен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось добавить клиента.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UpdateClient()
        {
            if (string.IsNullOrWhiteSpace(txtClientId.Text))
            {
                MessageBox.Show("Выберите клиента для изменения.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"UPDATE dbo.Clients
                      SET
                          last_name = @last_name,
                          first_name = @first_name,
                          middle_name = @middle_name,
                          phone = @phone,
                          email = @email,
                          connection_address = @connection_address
                      WHERE client_id = @client_id", connection);

                FillClientParameters(command);
                command.Parameters.Add("@client_id", SqlDbType.Int).Value = int.Parse(txtClientId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadClients();
                MessageBox.Show("Данные клиента успешно обновлены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось изменить данные клиента.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void DeleteClient()
        {
            if (string.IsNullOrWhiteSpace(txtClientId.Text))
            {
                MessageBox.Show("Выберите клиента для удаления.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Удалить выбранного клиента?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(
                    @"DELETE FROM dbo.Clients
                      WHERE client_id = @client_id", connection);

                command.Parameters.Add("@client_id", SqlDbType.Int).Value = int.Parse(txtClientId.Text);

                connection.Open();
                command.ExecuteNonQuery();

                LoadClients();
                MessageBox.Show("Клиент успешно удалён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить клиента.\n\n" +
                    "Возможно, на него ссылаются договоры или заявки.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void FillClientParameters(SqlCommand command)
        {
            command.Parameters.Add("@last_name", SqlDbType.NVarChar, 50).Value = txtLastName.Text.Trim();
            command.Parameters.Add("@first_name", SqlDbType.NVarChar, 50).Value = txtFirstName.Text.Trim();
            command.Parameters.Add("@middle_name", SqlDbType.NVarChar, 50).Value =
                string.IsNullOrWhiteSpace(txtMiddleName.Text) ? DBNull.Value : txtMiddleName.Text.Trim();
            command.Parameters.Add("@phone", SqlDbType.NVarChar, 20).Value = txtPhone.Text.Trim();
            command.Parameters.Add("@email", SqlDbType.NVarChar, 100).Value =
                string.IsNullOrWhiteSpace(txtEmail.Text) ? DBNull.Value : txtEmail.Text.Trim();
            command.Parameters.Add("@connection_address", SqlDbType.NVarChar, 200).Value = txtAddress.Text.Trim();
        }

        private void ClearInputs()
        {
            suppressSelectionChanged = true;

            try
            {
                dgvClients.ClearSelection();
                dgvClients.CurrentCell = null;

                txtClientId.Text = "";
                txtLastName.Text = "";
                txtFirstName.Text = "";
                txtMiddleName.Text = "";
                txtPhone.Text = "";
                txtEmail.Text = "";
                txtAddress.Text = "";
            }
            finally
            {
                suppressSelectionChanged = false;
            }

            txtLastName.Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (pictureBox != null && pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            base.OnFormClosed(e);
        }

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

                if (Image == null || Width <= 0 || Height <= 0)
                    return;

                float imageRatio = (float)Image.Width / Image.Height;
                float controlRatio = (float)Width / Height;

                RectangleF srcRect;

                if (imageRatio > controlRatio)
                {
                    float srcWidth = Image.Height * controlRatio;
                    float srcX = (Image.Width - srcWidth) / 2f;
                    srcRect = new RectangleF(srcX, 0, srcWidth, Image.Height);
                }
                else
                {
                    float srcHeight = Image.Width / controlRatio;
                    float srcY = (Image.Height - srcHeight) / 2f;
                    srcRect = new RectangleF(0, srcY, Image.Width, srcHeight);
                }

                e.Graphics.DrawImage(
                    Image,
                    new Rectangle(0, 0, Width, Height),
                    srcRect,
                    GraphicsUnit.Pixel);
            }
        }
    }
}