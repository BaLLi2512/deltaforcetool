namespace ItsATrap
{
    partial class GUI
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.mobIdTextBox = new System.Windows.Forms.TextBox();
            this.mobNameTextBox = new System.Windows.Forms.TextBox();
            this.addChangeButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.ListBox = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
			
            this.pictureBox1.Image = new System.Drawing.Bitmap("Plugins\\ItsATrap\\Resources\\akbar2.gif");
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(299, 392);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(489, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Active Config";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(488, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Mob Id";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(489, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "(optional) Name";
            // 
            // mobIdTextBox
            // 
            this.mobIdTextBox.Location = new System.Drawing.Point(576, 31);
            this.mobIdTextBox.Name = "mobIdTextBox";
            this.mobIdTextBox.Size = new System.Drawing.Size(100, 20);
            this.mobIdTextBox.TabIndex = 4;
            // 
            // mobNameTextBox
            // 
            this.mobNameTextBox.Location = new System.Drawing.Point(576, 57);
            this.mobNameTextBox.Name = "mobNameTextBox";
            this.mobNameTextBox.Size = new System.Drawing.Size(100, 20);
            this.mobNameTextBox.TabIndex = 5;
            // 
            // addChangeButton
            // 
            this.addChangeButton.Location = new System.Drawing.Point(585, 84);
            this.addChangeButton.Name = "addChangeButton";
            this.addChangeButton.Size = new System.Drawing.Size(90, 23);
            this.addChangeButton.TabIndex = 6;
            this.addChangeButton.Text = "Add/Change";
            this.addChangeButton.UseVisualStyleBackColor = true;
            this.addChangeButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // deleteButton
            // 
            this.deleteButton.Location = new System.Drawing.Point(492, 84);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(87, 23);
            this.deleteButton.TabIndex = 7;
            this.deleteButton.Text = "Delete";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.button2_Click);
            // 
            // ListBox
            // 
            this.ListBox.FormattingEnabled = true;
            this.ListBox.Location = new System.Drawing.Point(306, 13);
            this.ListBox.Name = "ListBox";
            this.ListBox.Size = new System.Drawing.Size(177, 368);
            this.ListBox.TabIndex = 8;
            this.ListBox.SelectedIndexChanged += new System.EventHandler(this.ListBox_SelectedIndexChanged);
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 392);
            this.Controls.Add(this.ListBox);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.addChangeButton);
            this.Controls.Add(this.mobNameTextBox);
            this.Controls.Add(this.mobIdTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "GUI";
            this.Text = "GUI";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox mobIdTextBox;
        private System.Windows.Forms.TextBox mobNameTextBox;
        private System.Windows.Forms.Button addChangeButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.ListBox ListBox;
    }
}