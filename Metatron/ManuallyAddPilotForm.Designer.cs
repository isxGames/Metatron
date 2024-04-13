namespace Metatron
{
	partial class ManuallyAddPilotForm
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
			this.groupBoxEntryType = new System.Windows.Forms.GroupBox();
			this.radioButtonCorporation = new System.Windows.Forms.RadioButton();
			this.radioButtonPilot = new System.Windows.Forms.RadioButton();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxID = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.groupBoxEntryType.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBoxEntryType
			// 
			this.groupBoxEntryType.Controls.Add(this.radioButtonCorporation);
			this.groupBoxEntryType.Controls.Add(this.radioButtonPilot);
			this.groupBoxEntryType.Location = new System.Drawing.Point(8, 6);
			this.groupBoxEntryType.Name = "groupBoxEntryType";
			this.groupBoxEntryType.Size = new System.Drawing.Size(89, 67);
			this.groupBoxEntryType.TabIndex = 0;
			this.groupBoxEntryType.TabStop = false;
			this.groupBoxEntryType.Text = "Entry Type";
			// 
			// radioButtonCorporation
			// 
			this.radioButtonCorporation.AutoSize = true;
			this.radioButtonCorporation.Location = new System.Drawing.Point(6, 42);
			this.radioButtonCorporation.Name = "radioButtonCorporation";
			this.radioButtonCorporation.Size = new System.Drawing.Size(79, 17);
			this.radioButtonCorporation.TabIndex = 1;
			this.radioButtonCorporation.TabStop = true;
			this.radioButtonCorporation.Text = "Corporation";
			this.radioButtonCorporation.UseVisualStyleBackColor = true;
			this.radioButtonCorporation.CheckedChanged += new System.EventHandler(this.radioButtonPilot_CheckedChanged);
			// 
			// radioButtonPilot
			// 
			this.radioButtonPilot.AutoSize = true;
			this.radioButtonPilot.Location = new System.Drawing.Point(6, 19);
			this.radioButtonPilot.Name = "radioButtonPilot";
			this.radioButtonPilot.Size = new System.Drawing.Size(45, 17);
			this.radioButtonPilot.TabIndex = 0;
			this.radioButtonPilot.TabStop = true;
			this.radioButtonPilot.Text = "Pilot";
			this.radioButtonPilot.UseVisualStyleBackColor = true;
			this.radioButtonPilot.CheckedChanged += new System.EventHandler(this.radioButtonPilot_CheckedChanged);
			// 
			// textBoxName
			// 
			this.textBoxName.Location = new System.Drawing.Point(162, 38);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.Size = new System.Drawing.Size(100, 20);
			this.textBoxName.TabIndex = 1;
			// 
			// buttonAdd
			// 
			this.buttonAdd.Location = new System.Drawing.Point(8, 83);
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.Size = new System.Drawing.Size(75, 23);
			this.buttonAdd.TabIndex = 2;
			this.buttonAdd.Text = "Add";
			this.buttonAdd.UseVisualStyleBackColor = true;
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(103, 41);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Name";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(103, 15);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(18, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "ID";
			// 
			// textBoxID
			// 
			this.textBoxID.Location = new System.Drawing.Point(162, 12);
			this.textBoxID.Name = "textBoxID";
			this.textBoxID.Size = new System.Drawing.Size(100, 20);
			this.textBoxID.TabIndex = 5;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(103, 61);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(172, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Warning: Make sure both fields are";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(103, 74);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(159, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "entered correctly. Name is case-";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(103, 87);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(170, 13);
			this.label5.TabIndex = 8;
			this.label5.Text = "sensitive and must not have typos.";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(103, 100);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(159, 13);
			this.label6.TabIndex = 9;
			this.label6.Text = "ID must be an accurate number.";
			// 
			// ManuallyAddPilotForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(274, 118);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.textBoxID);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonAdd);
			this.Controls.Add(this.textBoxName);
			this.Controls.Add(this.groupBoxEntryType);
			this.Name = "ManuallyAddPilotForm";
			this.Text = "Manually Add Entry";
			this.groupBoxEntryType.ResumeLayout(false);
			this.groupBoxEntryType.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBoxEntryType;
		private System.Windows.Forms.RadioButton radioButtonCorporation;
		private System.Windows.Forms.RadioButton radioButtonPilot;
		private System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxID;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;

	}
}