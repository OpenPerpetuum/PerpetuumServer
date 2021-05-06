namespace Perpetuum.DataDumper {
    partial class DumperForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DumperForm));
            this.StartButton = new System.Windows.Forms.Button();
            this.mappingSelectList = new System.Windows.Forms.CheckedListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.allTypesButton = new System.Windows.Forms.Button();
            this.clearTypesButton = new System.Windows.Forms.Button();
            this.serverPathTextbox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dictionaryPathTextbox = new System.Windows.Forms.TextBox();
            this.saveSettingsButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(12, 12);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(65, 23);
            this.StartButton.TabIndex = 0;
            this.StartButton.Text = "Start";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // mappingSelectList
            // 
            this.mappingSelectList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.mappingSelectList.FormattingEnabled = true;
            this.mappingSelectList.Location = new System.Drawing.Point(12, 63);
            this.mappingSelectList.Name = "mappingSelectList";
            this.mappingSelectList.Size = new System.Drawing.Size(306, 379);
            this.mappingSelectList.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Data Types";
            // 
            // allTypesButton
            // 
            this.allTypesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.allTypesButton.Location = new System.Drawing.Point(12, 453);
            this.allTypesButton.Name = "allTypesButton";
            this.allTypesButton.Size = new System.Drawing.Size(75, 23);
            this.allTypesButton.TabIndex = 5;
            this.allTypesButton.Text = "All";
            this.allTypesButton.UseVisualStyleBackColor = true;
            this.allTypesButton.Click += new System.EventHandler(this.allTypesButton_Click);
            // 
            // clearTypesButton
            // 
            this.clearTypesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clearTypesButton.Location = new System.Drawing.Point(122, 453);
            this.clearTypesButton.Name = "clearTypesButton";
            this.clearTypesButton.Size = new System.Drawing.Size(75, 23);
            this.clearTypesButton.TabIndex = 6;
            this.clearTypesButton.Text = "Clear";
            this.clearTypesButton.UseVisualStyleBackColor = true;
            this.clearTypesButton.Click += new System.EventHandler(this.clearTypesButton_Click);
            // 
            // serverPathTextbox
            // 
            this.serverPathTextbox.Location = new System.Drawing.Point(109, 23);
            this.serverPathTextbox.Name = "serverPathTextbox";
            this.serverPathTextbox.Size = new System.Drawing.Size(167, 20);
            this.serverPathTextbox.TabIndex = 7;
            this.serverPathTextbox.TextChanged += new System.EventHandler(this.serverPathTextbox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Server Data Path:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.saveSettingsButton);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.dictionaryPathTextbox);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.serverPathTextbox);
            this.groupBox1.Location = new System.Drawing.Point(331, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(285, 100);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Settings";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Dictionary Path";
            // 
            // dictionaryPathTextbox
            // 
            this.dictionaryPathTextbox.Location = new System.Drawing.Point(111, 49);
            this.dictionaryPathTextbox.Name = "dictionaryPathTextbox";
            this.dictionaryPathTextbox.Size = new System.Drawing.Size(167, 20);
            this.dictionaryPathTextbox.TabIndex = 9;
            this.dictionaryPathTextbox.TextChanged += new System.EventHandler(this.dictionaryPathTextbox_TextChanged);
            // 
            // saveSettingsButton
            // 
            this.saveSettingsButton.Location = new System.Drawing.Point(7, 71);
            this.saveSettingsButton.Name = "saveSettingsButton";
            this.saveSettingsButton.Size = new System.Drawing.Size(58, 23);
            this.saveSettingsButton.TabIndex = 11;
            this.saveSettingsButton.Text = "Save";
            this.saveSettingsButton.UseVisualStyleBackColor = true;
            this.saveSettingsButton.Click += new System.EventHandler(this.saveSettingsButton_Click);
            // 
            // DumperForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 488);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.clearTypesButton);
            this.Controls.Add(this.allTypesButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.mappingSelectList);
            this.Controls.Add(this.StartButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DumperForm";
            this.Text = "Perpetuum Data Dumper";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.CheckedListBox mappingSelectList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button allTypesButton;
        private System.Windows.Forms.Button clearTypesButton;
        private System.Windows.Forms.TextBox serverPathTextbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox dictionaryPathTextbox;
        private System.Windows.Forms.Button saveSettingsButton;
    }
}

