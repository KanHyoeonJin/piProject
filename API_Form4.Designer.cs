namespace Drive
{
    partial class API_Form4
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
            API_button_start = new System.Windows.Forms.Button();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            btnShowTotalDistanceClick = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // API_button_start
            // 
            API_button_start.Enabled = false;
            API_button_start.Location = new System.Drawing.Point(24, 45);
            API_button_start.Name = "API_button_start";
            API_button_start.Size = new System.Drawing.Size(126, 52);
            API_button_start.TabIndex = 0;
            API_button_start.Text = "얼굴비교";
            API_button_start.UseVisualStyleBackColor = true;
            API_button_start.Click += API_button_start_Click_1;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new System.Drawing.Point(214, 45);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(476, 304);
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // btnShowTotalDistanceClick
            // 
            btnShowTotalDistanceClick.Location = new System.Drawing.Point(24, 172);
            btnShowTotalDistanceClick.Name = "btnShowTotalDistanceClick";
            btnShowTotalDistanceClick.Size = new System.Drawing.Size(126, 64);
            btnShowTotalDistanceClick.TabIndex = 3;
            btnShowTotalDistanceClick.Text = "총 이동거리 확인";
            btnShowTotalDistanceClick.UseVisualStyleBackColor = true;
            btnShowTotalDistanceClick.Click += btnShowTotalDistanceClick_Click;
            // 
            // API_Form4
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(btnShowTotalDistanceClick);
            Controls.Add(pictureBox1);
            Controls.Add(API_button_start);
            Name = "API_Form4";
            Text = "API_Form4";
            Load += API_Form2_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button API_button_start;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnShowTotalDistanceClick;
    }
}