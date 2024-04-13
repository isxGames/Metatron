namespace Metatron
{
    partial class MiniStatusForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.startButton = new System.Windows.Forms.Button();
            this.pauseButton = new System.Windows.Forms.Button();
            this.listBoxLogMessages = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.shipNameLabel = new System.Windows.Forms.Label();
            this.shieldLabel = new System.Windows.Forms.Label();
            this.armorLabel = new System.Windows.Forms.Label();
            this.structureLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36.36364F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27.27273F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36.36364F));
            this.tableLayoutPanel1.Controls.Add(this.startButton, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.pauseButton, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.listBoxLogMessages, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(671, 81);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // startButton
            // 
            this.startButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.startButton.Location = new System.Drawing.Point(3, 3);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(54, 34);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // pauseButton
            // 
            this.pauseButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pauseButton.Location = new System.Drawing.Point(3, 43);
            this.pauseButton.Name = "pauseButton";
            this.pauseButton.Size = new System.Drawing.Size(54, 35);
            this.pauseButton.TabIndex = 1;
            this.pauseButton.Text = "Pause";
            this.pauseButton.UseVisualStyleBackColor = true;
            this.pauseButton.Click += new System.EventHandler(this.pauseButton_Click);
            // 
            // listBoxLogMessages
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.listBoxLogMessages, 3);
            this.listBoxLogMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxLogMessages.FormattingEnabled = true;
            this.listBoxLogMessages.Location = new System.Drawing.Point(63, 43);
            this.listBoxLogMessages.Name = "listBoxLogMessages";
            this.listBoxLogMessages.Size = new System.Drawing.Size(605, 35);
            this.listBoxLogMessages.TabIndex = 2;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 2);
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.shipNameLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.shieldLabel, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.armorLabel, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.structureLabel, 3, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(63, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(382, 34);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // shipNameLabel
            // 
            this.shipNameLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.shipNameLabel.AutoSize = true;
            this.shipNameLabel.Location = new System.Drawing.Point(18, 10);
            this.shipNameLabel.Name = "shipNameLabel";
            this.shipNameLabel.Size = new System.Drawing.Size(59, 13);
            this.shipNameLabel.TabIndex = 0;
            this.shipNameLabel.Text = "Ship Name";
            // 
            // shieldLabel
            // 
            this.shieldLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.shieldLabel.AutoSize = true;
            this.shieldLabel.Location = new System.Drawing.Point(122, 10);
            this.shieldLabel.Name = "shieldLabel";
            this.shieldLabel.Size = new System.Drawing.Size(41, 13);
            this.shieldLabel.TabIndex = 1;
            this.shieldLabel.Text = "Shields";
            // 
            // armorLabel
            // 
            this.armorLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.armorLabel.AutoSize = true;
            this.armorLabel.Location = new System.Drawing.Point(220, 10);
            this.armorLabel.Name = "armorLabel";
            this.armorLabel.Size = new System.Drawing.Size(34, 13);
            this.armorLabel.TabIndex = 2;
            this.armorLabel.Text = "Armor";
            // 
            // structureLabel
            // 
            this.structureLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.structureLabel.AutoSize = true;
            this.structureLabel.Location = new System.Drawing.Point(308, 10);
            this.structureLabel.Name = "structureLabel";
            this.structureLabel.Size = new System.Drawing.Size(50, 13);
            this.structureLabel.TabIndex = 3;
            this.structureLabel.Text = "Structure";
            // 
            // MiniStatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(671, 81);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MiniStatusForm";
            this.Opacity = 0.75D;
            this.Text = "MiniStatusForm";
            this.TopMost = true;
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button pauseButton;
        private System.Windows.Forms.ListBox listBoxLogMessages;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label shipNameLabel;
        private System.Windows.Forms.Label shieldLabel;
        private System.Windows.Forms.Label armorLabel;
        private System.Windows.Forms.Label structureLabel;
    }
}