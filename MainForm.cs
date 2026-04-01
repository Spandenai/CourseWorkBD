using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CourseWork
{
    public partial class MainForm : Form
    {
        // Палитра
        private readonly Color appBackColor = Color.FromArgb(241, 245, 249);
        private readonly Color sidebarBackColor = Color.FromArgb(15, 23, 42);
        private readonly Color sidebarCardColor = Color.FromArgb(30, 41, 59);
        private readonly Color cardBackColor = Color.White;
        private readonly Color borderColor = Color.FromArgb(226, 232, 240);
        private readonly Color titleColor = Color.FromArgb(15, 23, 42);
        private readonly Color mutedTextColor = Color.FromArgb(100, 116, 139);

        private readonly Color accentBlue = Color.FromArgb(14, 165, 233);
        private readonly Color accentCyan = Color.FromArgb(6, 182, 212);
        private readonly Color accentAmber = Color.FromArgb(245, 158, 11);
        private readonly Color accentViolet = Color.FromArgb(99, 102, 241);
        private readonly Color accentRose = Color.FromArgb(244, 63, 94);
        private readonly Color accentEmerald = Color.FromArgb(16, 185, 129);

        private Label lblDateTime;
        private PictureBox pictureBox;

        private readonly System.Windows.Forms.Timer clockTimer = new System.Windows.Forms.Timer();

        private readonly string bannerPath =
            Path.Combine(Application.StartupPath, "Images", "provider_main.png");

        public MainForm()
        {
            InitializeComponent();

            Controls.Clear();

            InitializeForm();
            BuildInterface();
            StartClock();
            LoadBannerIfExists();
        }

        private void InitializeForm()
        {
            Text = "Internet Provider - Главное меню";
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1280, 760);
            Size = new Size(1400, 820);
            BackColor = appBackColor;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            DoubleBuffered = true;
        }

        private void BuildInterface()
        {
            SuspendLayout();

            var sidebar = BuildSidebar();
            var content = BuildContent();

            Controls.Add(content);
            Controls.Add(sidebar);

            ResumeLayout(false);
        }

        private Control BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = sidebarBackColor,
                Padding = new Padding(24)
            };

            var logoCircle = new Panel
            {
                Size = new Size(74, 74),
                BackColor = accentBlue,
                Location = new Point(24, 24)
            };

            logoCircle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(accentBlue);
                e.Graphics.FillEllipse(brush, 0, 0, logoCircle.Width - 1, logoCircle.Height - 1);

                using var font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
                TextRenderer.DrawText(
                    e.Graphics,
                    "IP",
                    font,
                    new Rectangle(0, 0, logoCircle.Width, logoCircle.Height),
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            var lblAppTitle = new Label
            {
                AutoSize = false,
                Text = "Internet Provider",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                Location = new Point(24, 115),
                Size = new Size(240, 36)
            };

            var lblAppSubtitle = new Label
            {
                AutoSize = false,
                Text = "Главное меню системы управления\nбазой данных провайдера",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                Location = new Point(24, 156),
                Size = new Size(240, 50)
            };

            var dbCard = new ModernCard
            {
                BackColor = sidebarCardColor,
                BorderColor = Color.FromArgb(51, 65, 85),
                Radius = 24,
                Size = new Size(252, 132),
                Location = new Point(24, 230)
            };

            var lblDbCaption = new Label
            {
                AutoSize = false,
                Text = "Активная база данных",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(16, 14, 16, 0)
            };

            var lblDbName = new Label
            {
                AutoSize = false,
                Text = "InternetProviderDB",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(16, 2, 16, 0)
            };

            var lblDbHint = new Label
            {
                AutoSize = false,
                Text = "Таблицы:\nClients, Contracts, Equipment,\nPayments, Requests, Tariffs",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 2, 16, 12)
            };

            dbCard.Controls.Add(lblDbHint);
            dbCard.Controls.Add(lblDbName);
            dbCard.Controls.Add(lblDbCaption);

            var lblMenuCaption = new Label
            {
                AutoSize = false,
                Text = "Быстрые действия",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Location = new Point(24, 392),
                Size = new Size(240, 24)
            };

            var btnAbout = CreateSidebarButton("О программе", accentCyan, (s, e) =>
            {
                MessageBox.Show(
                    "Система предназначена для работы с базой данных интернет-провайдера.\n\n" +
                    "Разделы главного меню:\n" +
                    "• Клиенты\n" +
                    "• Тарифы\n" +
                    "• Договоры\n" +
                    "• Платежи\n" +
                    "• Оборудование\n" +
                    "• Заявки",
                    "О программе",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });
            btnAbout.Location = new Point(24, 425);

            var btnExit = CreateSidebarButton("Выход", accentAmber, (s, e) => Close());
            btnExit.Location = new Point(24, 483);

            var lblFooter = new Label
            {
                AutoSize = false,
                Text = "Курсовой проект • Главное меню",
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Location = new Point(24, 675),
                Size = new Size(240, 24)
            };

            sidebar.Controls.Add(logoCircle);
            sidebar.Controls.Add(lblAppTitle);
            sidebar.Controls.Add(lblAppSubtitle);
            sidebar.Controls.Add(dbCard);
            sidebar.Controls.Add(lblMenuCaption);
            sidebar.Controls.Add(btnAbout);
            sidebar.Controls.Add(btnExit);
            sidebar.Controls.Add(lblFooter);

            return sidebar;
        }

        private Control BuildContent()
        {
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = appBackColor,
                Padding = new Padding(28)
            };

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.Transparent
            };

            var lblMainTitle = new Label
            {
                AutoSize = false,
                Text = "Главное меню",
                Font = new Font("Segoe UI Semibold", 28f, FontStyle.Bold),
                ForeColor = titleColor,
                Location = new Point(0, 0),
                Size = new Size(450, 48)
            };

            var lblMainSubtitle = new Label
            {
                AutoSize = false,
                Text = "Выберите раздел для работы с данными интернет-провайдера",
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                ForeColor = mutedTextColor,
                Location = new Point(2, 52),
                Size = new Size(560, 28)
            };

            lblDateTime = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = titleColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(340, 32),
                Location = new Point(730, 18)
            };

            var lblDateHint = new Label
            {
                AutoSize = false,
                Text = "Текущая дата и время",
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = mutedTextColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(340, 24),
                Location = new Point(730, 50)
            };

            topPanel.Resize += (s, e) =>
            {
                lblDateTime.Left = topPanel.Width - lblDateTime.Width;
                lblDateHint.Left = topPanel.Width - lblDateHint.Width;
            };

            topPanel.Controls.Add(lblMainTitle);
            topPanel.Controls.Add(lblMainSubtitle);
            topPanel.Controls.Add(lblDateTime);
            topPanel.Controls.Add(lblDateHint);

            var bodyTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61f));
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39f));

            var quickAccessCard = BuildQuickAccessCard();
            var imageCard = BuildImageCard();

            bodyTable.Controls.Add(quickAccessCard, 0, 0);
            bodyTable.Controls.Add(imageCard, 1, 0);

            content.Controls.Add(bodyTable);
            content.Controls.Add(topPanel);

            return content;
        }

        private Control BuildQuickAccessCard()
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

            var lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Разделы системы",
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                ForeColor = titleColor
            };

            var lblSub = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Переход к основным формам работы с БД",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = mutedTextColor
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(0, 14, 0, 0),
                BackColor = Color.Transparent
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            var btnClients = CreateSectionButton(
                "Клиенты",
                "База абонентов и контактные данные",
                accentBlue,
                (s, e) => OpenSection("Клиенты", "ClientsForm", "Clients"));

            var btnTariffs = CreateSectionButton(
                "Тарифы",
                "Скорость, стоимость и описание планов",
                accentCyan,
                (s, e) => OpenSection("Тарифы", "TariffsForm", "Tariffs"));

            var btnContracts = CreateSectionButton(
                "Договоры",
                "Заключённые договоры и статусы",
                accentViolet,
                (s, e) => OpenSection("Договоры", "ContractsForm", "Contracts"));

            var btnPayments = CreateSectionButton(
                "Платежи",
                "История оплат и способы оплаты",
                accentEmerald,
                (s, e) => OpenSection("Платежи", "PaymentsForm", "Payments"));

            var btnEquipment = CreateSectionButton(
                "Оборудование",
                "Выданное оборудование и серийные номера",
                accentAmber,
                (s, e) => OpenSection("Оборудование", "EquipmentForm", "Equipment"));

            var btnRequests = CreateSectionButton(
                "Заявки",
                "Обращения клиентов и текущие статусы",
                accentRose,
                (s, e) => OpenSection("Заявки", "RequestsForm", "Requests"));

            grid.Controls.Add(WrapWithMargin(btnClients), 0, 0);
            grid.Controls.Add(WrapWithMargin(btnTariffs), 1, 0);
            grid.Controls.Add(WrapWithMargin(btnContracts), 0, 1);
            grid.Controls.Add(WrapWithMargin(btnPayments), 1, 1);
            grid.Controls.Add(WrapWithMargin(btnEquipment), 0, 2);
            grid.Controls.Add(WrapWithMargin(btnRequests), 1, 2);

            card.Controls.Add(grid);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblTitle);

            return card;
        }

        private Control BuildImageCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                BackColor = cardBackColor,
                BorderColor = borderColor,
                Radius = 28,
                Margin = new Padding(14, 0, 0, 0),
                Padding = new Padding(22)
            };

            var lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Визуальный блок",
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                ForeColor = titleColor
            };

            var imageHolder = new ModernCard
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderColor = borderColor,
                Radius = 24,
                Margin = new Padding(0, 14, 0, 0),
                Padding = new Padding(0)
            };

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            imageHolder.Controls.Add(pictureBox);

            card.Controls.Add(imageHolder);
            card.Controls.Add(lblTitle);

            return card;
        }

        private Panel WrapWithMargin(Control control)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };

            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control);
            return panel;
        }

        private Button CreateSidebarButton(string text, Color accent, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(252, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = sidebarCardColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);

            button.MouseEnter += (s, e) =>
            {
                button.BackColor = ControlPaint.Light(sidebarCardColor, 0.08f);
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = sidebarCardColor;
            };

            button.Paint += (s, e) =>
            {
                using var pen = new Pen(accent, 4);
                e.Graphics.DrawLine(pen, 0, 0, 0, button.Height);
            };

            button.Click += onClick;
            return button;
        }

        private SectionButton CreateSectionButton(
            string title,
            string subtitle,
            Color accent,
            EventHandler onClick)
        {
            var button = new SectionButton
            {
                Title = title,
                Subtitle = subtitle,
                AccentColor = accent
            };

            button.Click += onClick;
            return button;
        }

        private void StartClock()
        {
            UpdateClock();
            clockTimer.Interval = 1000;
            clockTimer.Tick += (s, e) => UpdateClock();
            clockTimer.Start();
        }

        private void UpdateClock()
        {
            if (lblDateTime != null)
                lblDateTime.Text = DateTime.Now.ToString("dd.MM.yyyy  •  HH:mm:ss");
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

        private void OpenSection(string sectionTitle, params string[] candidateTypeNames)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                var formType = assembly
                    .GetTypes()
                    .Where(t => typeof(Form).IsAssignableFrom(t) && !t.IsAbstract)
                    .FirstOrDefault(t =>
                        candidateTypeNames.Any(name =>
                            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase) ||
                            (t.FullName != null && t.FullName.EndsWith("." + name, StringComparison.OrdinalIgnoreCase))));

                if (formType == null)
                {
                    MessageBox.Show(
                        $"Форма раздела «{sectionTitle}» пока не найдена.\n\n" +
                        $"Когда создашь нужный файл формы, кнопка начнёт открывать этот раздел автоматически.",
                        "Раздел пока не создан",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (Activator.CreateInstance(formType) is not Form form)
                {
                    MessageBox.Show(
                        $"Не удалось создать форму для раздела «{sectionTitle}».",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                form.StartPosition = FormStartPosition.CenterScreen;
                form.ShowDialog(this);
                form.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Произошла ошибка при открытии раздела.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            clockTimer.Stop();
            clockTimer.Dispose();

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
            private Color borderColor = Color.FromArgb(226, 232, 240);

            [Browsable(true)]
            [Category("Appearance")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public int Radius
            {
                get => radius;
                set
                {
                    radius = value < 1 ? 1 : value;
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

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = GetRoundedPath(rect, Radius);
                using var brush = new SolidBrush(BackColor);
                using var pen = new Pen(BorderColor, 1);

                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);

                Region = new Region(path);
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
        private class SectionButton : Control
        {
            private string title = "Раздел";
            private string subtitle = "Описание раздела";
            private Color accentColor = Color.FromArgb(14, 165, 233);

            private bool isHovered;
            private bool isPressed;

            [Browsable(true)]
            [Category("Appearance")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public string Title
            {
                get => title;
                set
                {
                    title = value;
                    Invalidate();
                }
            }

            [Browsable(true)]
            [Category("Appearance")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public string Subtitle
            {
                get => subtitle;
                set
                {
                    subtitle = value;
                    Invalidate();
                }
            }

            [Browsable(true)]
            [Category("Appearance")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public Color AccentColor
            {
                get => accentColor;
                set
                {
                    accentColor = value;
                    Invalidate();
                }
            }

            public SectionButton()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Hand;
                Size = new Size(320, 160);
                Font = new Font("Segoe UI", 10f);

                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint,
                    true);
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                isHovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                isHovered = false;
                isPressed = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                isPressed = true;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                isPressed = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Color back = Color.White;
                Color border = Color.FromArgb(226, 232, 240);

                if (isHovered)
                {
                    back = Blend(Color.White, AccentColor, 0.07);
                    border = Blend(Color.FromArgb(226, 232, 240), AccentColor, 0.45);
                }

                if (isPressed)
                {
                    back = Blend(Color.White, AccentColor, 0.13);
                    border = Blend(Color.FromArgb(226, 232, 240), AccentColor, 0.60);
                }

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);

                using var path = GetRoundedPath(rect, 24);
                using var brush = new SolidBrush(back);
                using var pen = new Pen(border, 1);

                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);

                using var accentBrush = new SolidBrush(AccentColor);
                e.Graphics.FillEllipse(accentBrush, 18, 18, 14, 14);

                using var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
                using var subtitleBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
                using var arrowBrush = new SolidBrush(AccentColor);
                using var titleFont = new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 10f, FontStyle.Regular);
                using var arrowFont = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);

                e.Graphics.DrawString(
                    Title,
                    titleFont,
                    titleBrush,
                    new RectangleF(18, 42, Width - 70, 36));

                e.Graphics.DrawString(
                    Subtitle,
                    subFont,
                    subtitleBrush,
                    new RectangleF(18, 82, Width - 70, 48));

                e.Graphics.DrawString(
                    "→",
                    arrowFont,
                    arrowBrush,
                    new RectangleF(Width - 48, Height - 46, 24, 24));

                Region = new Region(path);
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

            private static Color Blend(Color from, Color to, double amount)
            {
                int r = (int)(from.R + (to.R - from.R) * amount);
                int g = (int)(from.G + (to.G - from.G) * amount);
                int b = (int)(from.B + (to.B - from.B) * amount);

                return Color.FromArgb(r, g, b);
            }
        }
    }
}