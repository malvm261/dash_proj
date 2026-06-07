namespace PiClient
{
    partial class MainForm
    {
        /// <summary>Обязательная переменная конструктора.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Освобождение используемых ресурсов.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows Forms

        /// <summary>
        /// Обязательный метод для поддержки конструктора — не изменяйте
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblAddresses = new System.Windows.Forms.Label();
            this.txtAddresses = new System.Windows.Forms.TextBox();
            this.lblIterations = new System.Windows.Forms.Label();
            this.numIterations = new System.Windows.Forms.NumericUpDown();
            this.lblInterval = new System.Windows.Forms.Label();
            this.numInterval = new System.Windows.Forms.NumericUpDown();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblCores = new System.Windows.Forms.Label();
            this.progressOverall = new System.Windows.Forms.ProgressBar();
            this.lblAveragePi = new System.Windows.Forms.Label();
            this.lblReference = new System.Windows.Forms.Label();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.colAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFormula = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProgress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.numIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.SuspendLayout();
            //
            // lblAddresses
            //
            this.lblAddresses.AutoSize = true;
            this.lblAddresses.Location = new System.Drawing.Point(12, 15);
            this.lblAddresses.Name = "lblAddresses";
            this.lblAddresses.Size = new System.Drawing.Size(218, 15);
            this.lblAddresses.TabIndex = 0;
            this.lblAddresses.Text = "Адреса серверов (по одному в строке):";
            //
            // txtAddresses
            //
            this.txtAddresses.Location = new System.Drawing.Point(12, 33);
            this.txtAddresses.Multiline = true;
            this.txtAddresses.Name = "txtAddresses";
            this.txtAddresses.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtAddresses.Size = new System.Drawing.Size(258, 84);
            this.txtAddresses.TabIndex = 1;
            this.txtAddresses.Text = "http://localhost:5050";
            //
            // lblIterations
            //
            this.lblIterations.AutoSize = true;
            this.lblIterations.Location = new System.Drawing.Point(288, 15);
            this.lblIterations.Name = "lblIterations";
            this.lblIterations.Size = new System.Drawing.Size(96, 15);
            this.lblIterations.TabIndex = 2;
            this.lblIterations.Text = "Число итераций:";
            //
            // numIterations
            //
            this.numIterations.Location = new System.Drawing.Point(288, 33);
            this.numIterations.Maximum = new decimal(new int[] {
            1410065408,
            2,
            0,
            0});
            this.numIterations.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numIterations.Name = "numIterations";
            this.numIterations.Size = new System.Drawing.Size(150, 23);
            this.numIterations.TabIndex = 3;
            this.numIterations.ThousandsSeparator = true;
            this.numIterations.Value = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            //
            // lblInterval
            //
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(288, 65);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(135, 15);
            this.lblInterval.TabIndex = 4;
            this.lblInterval.Text = "Интервал опроса, сек:";
            //
            // numInterval
            //
            this.numInterval.Location = new System.Drawing.Point(288, 83);
            this.numInterval.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInterval.Name = "numInterval";
            this.numInterval.Size = new System.Drawing.Size(150, 23);
            this.numInterval.TabIndex = 5;
            this.numInterval.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(458, 33);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(100, 32);
            this.btnStart.TabIndex = 6;
            this.btnStart.Text = "Старт";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);
            //
            // btnStop
            //
            this.btnStop.Enabled = false;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Location = new System.Drawing.Point(458, 71);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(100, 32);
            this.btnStop.TabIndex = 7;
            this.btnStop.Text = "Стоп";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.BtnStop_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(576, 41);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(40, 15);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "Готов";
            //
            // lblCores
            //
            this.lblCores.AutoSize = true;
            this.lblCores.ForeColor = System.Drawing.Color.Gray;
            this.lblCores.Location = new System.Drawing.Point(576, 80);
            this.lblCores.Name = "lblCores";
            this.lblCores.Size = new System.Drawing.Size(70, 15);
            this.lblCores.TabIndex = 9;
            this.lblCores.Text = "Ядер CPU: —";
            //
            // progressOverall
            //
            this.progressOverall.Location = new System.Drawing.Point(12, 130);
            this.progressOverall.Name = "progressOverall";
            this.progressOverall.Size = new System.Drawing.Size(546, 23);
            this.progressOverall.TabIndex = 10;
            //
            // lblAveragePi
            //
            this.lblAveragePi.AutoSize = true;
            this.lblAveragePi.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this.lblAveragePi.Location = new System.Drawing.Point(12, 163);
            this.lblAveragePi.Name = "lblAveragePi";
            this.lblAveragePi.Size = new System.Drawing.Size(186, 20);
            this.lblAveragePi.TabIndex = 11;
            this.lblAveragePi.Text = "Среднее значение π:  —";
            //
            // lblReference
            //
            this.lblReference.AutoSize = true;
            this.lblReference.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblReference.ForeColor = System.Drawing.Color.Gray;
            this.lblReference.Location = new System.Drawing.Point(12, 188);
            this.lblReference.Name = "lblReference";
            this.lblReference.Size = new System.Drawing.Size(295, 15);
            this.lblReference.TabIndex = 12;
            this.lblReference.Text = "π (эталон):          3.14159265358979323846...";
            //
            // dgvResults
            //
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAddress,
            this.colFormula,
            this.colPi,
            this.colProgress});
            this.dgvResults.Location = new System.Drawing.Point(12, 213);
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.Size = new System.Drawing.Size(660, 300);
            this.dgvResults.TabIndex = 13;
            //
            // colAddress
            //
            this.colAddress.HeaderText = "Адрес узла";
            this.colAddress.Name = "colAddress";
            this.colAddress.ReadOnly = true;
            this.colAddress.Width = 180;
            //
            // colFormula
            //
            this.colFormula.HeaderText = "Формула";
            this.colFormula.Name = "colFormula";
            this.colFormula.ReadOnly = true;
            this.colFormula.Width = 150;
            //
            // colPi
            //
            this.colPi.HeaderText = "Текущее значение π";
            this.colPi.Name = "colPi";
            this.colPi.ReadOnly = true;
            this.colPi.Width = 220;
            //
            // colProgress
            //
            this.colProgress.HeaderText = "Прогресс, %";
            this.colProgress.Name = "colProgress";
            this.colProgress.ReadOnly = true;
            this.colProgress.Width = 90;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 525);
            this.Controls.Add(this.dgvResults);
            this.Controls.Add(this.lblReference);
            this.Controls.Add(this.lblAveragePi);
            this.Controls.Add(this.progressOverall);
            this.Controls.Add(this.lblCores);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.numInterval);
            this.Controls.Add(this.lblInterval);
            this.Controls.Add(this.numIterations);
            this.Controls.Add(this.lblIterations);
            this.Controls.Add(this.txtAddresses);
            this.Controls.Add(this.lblAddresses);
            this.MinimumSize = new System.Drawing.Size(700, 560);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Клиент распределённого расчёта числа π";
            ((System.ComponentModel.ISupportInitialize)(this.numIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblAddresses;
        private System.Windows.Forms.TextBox txtAddresses;
        private System.Windows.Forms.Label lblIterations;
        private System.Windows.Forms.NumericUpDown numIterations;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.NumericUpDown numInterval;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblCores;
        private System.Windows.Forms.ProgressBar progressOverall;
        private System.Windows.Forms.Label lblAveragePi;
        private System.Windows.Forms.Label lblReference;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFormula;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPi;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProgress;
    }
}
