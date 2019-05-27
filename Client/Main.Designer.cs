namespace API_Test_Client {
	partial class Main {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.RequestEditorField = new System.Windows.Forms.RichTextBox();
			this.RequestTypesList = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.SendRequest = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.ShowResponse = new System.Windows.Forms.Button();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// RequestEditorField
			// 
			this.RequestEditorField.AcceptsTab = true;
			this.RequestEditorField.Location = new System.Drawing.Point(405, 15);
			this.RequestEditorField.Margin = new System.Windows.Forms.Padding(4);
			this.RequestEditorField.Name = "RequestEditorField";
			this.RequestEditorField.Size = new System.Drawing.Size(649, 490);
			this.RequestEditorField.TabIndex = 0;
			this.RequestEditorField.Text = "";
			// 
			// RequestTypesList
			// 
			this.RequestTypesList.FormattingEnabled = true;
			this.RequestTypesList.Location = new System.Drawing.Point(115, 16);
			this.RequestTypesList.Margin = new System.Windows.Forms.Padding(4);
			this.RequestTypesList.Name = "RequestTypesList";
			this.RequestTypesList.Size = new System.Drawing.Size(252, 24);
			this.RequestTypesList.TabIndex = 1;
			this.RequestTypesList.SelectionChangeCommitted += new System.EventHandler(this.UpdateEditor);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 20);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(97, 17);
			this.label1.TabIndex = 2;
			this.label1.Text = "Request Type";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 53);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(106, 17);
			this.label2.TabIndex = 3;
			this.label2.Text = "Server Address";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(115, 49);
			this.textBox1.Margin = new System.Windows.Forms.Padding(4);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(252, 22);
			this.textBox1.TabIndex = 4;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.SendRequest);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.textBox1);
			this.groupBox1.Controls.Add(this.RequestTypesList);
			this.groupBox1.Location = new System.Drawing.Point(16, 15);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
			this.groupBox1.Size = new System.Drawing.Size(376, 123);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Requests";
			// 
			// SendRequest
			// 
			this.SendRequest.Location = new System.Drawing.Point(12, 87);
			this.SendRequest.Margin = new System.Windows.Forms.Padding(4);
			this.SendRequest.Name = "SendRequest";
			this.SendRequest.Size = new System.Drawing.Size(356, 28);
			this.SendRequest.TabIndex = 5;
			this.SendRequest.Text = "Send";
			this.SendRequest.UseVisualStyleBackColor = true;
			this.SendRequest.Click += new System.EventHandler(this.SendRequest_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.ShowResponse);
			this.groupBox2.Controls.Add(this.comboBox2);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Location = new System.Drawing.Point(16, 145);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
			this.groupBox2.Size = new System.Drawing.Size(368, 86);
			this.groupBox2.TabIndex = 6;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Responses";
			// 
			// ShowResponse
			// 
			this.ShowResponse.Location = new System.Drawing.Point(12, 49);
			this.ShowResponse.Margin = new System.Windows.Forms.Padding(4);
			this.ShowResponse.Name = "ShowResponse";
			this.ShowResponse.Size = new System.Drawing.Size(348, 28);
			this.ShowResponse.TabIndex = 7;
			this.ShowResponse.Text = "Show Response";
			this.ShowResponse.UseVisualStyleBackColor = true;
			// 
			// comboBox2
			// 
			this.comboBox2.FormattingEnabled = true;
			this.comboBox2.Location = new System.Drawing.Point(115, 16);
			this.comboBox2.Margin = new System.Windows.Forms.Padding(4);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(244, 24);
			this.comboBox2.TabIndex = 6;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 20);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(52, 17);
			this.label3.TabIndex = 6;
			this.label3.Text = "History";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(857, 513);
			this.button1.Margin = new System.Windows.Forms.Padding(4);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(197, 28);
			this.button1.TabIndex = 8;
			this.button1.Text = "Reload template";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(651, 513);
			this.button2.Margin = new System.Windows.Forms.Padding(4);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(198, 28);
			this.button2.TabIndex = 9;
			this.button2.Text = "Save as template";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1067, 554);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.RequestEditorField);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "Main";
			this.Text = "JSON Request Utility";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RichTextBox RequestEditorField;
		private System.Windows.Forms.ComboBox RequestTypesList;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button SendRequest;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button ShowResponse;
		private System.Windows.Forms.ComboBox comboBox2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
	}
}

