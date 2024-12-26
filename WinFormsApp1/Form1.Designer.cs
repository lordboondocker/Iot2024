namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnTogglePump = new Button();
            btnToggleMode = new Button();
            SoilMoisture = new Label();
            PumpStatus = new Label();
            ModeStatus = new Label();
            SuspendLayout();
            // 
            // btnTogglePump
            // 
            btnTogglePump.Location = new Point(92, 178);
            btnTogglePump.Name = "btnTogglePump";
            btnTogglePump.Size = new Size(152, 23);
            btnTogglePump.TabIndex = 0;
            btnTogglePump.Text = "Включить/Выключить насос";
            btnTogglePump.UseVisualStyleBackColor = true;
            btnTogglePump.Click += btnTogglePump_Click;
            // 
            // btnToggleMode
            // 
            btnToggleMode.AutoSize = true;
            btnToggleMode.Location = new Point(253, 176);
            btnToggleMode.Name = "btnToggleMode";
            btnToggleMode.Size = new Size(134, 25);
            btnToggleMode.TabIndex = 4;
            btnToggleMode.Text = "Переключить режим";
            btnToggleMode.UseVisualStyleBackColor = true;
            btnToggleMode.Click += btnToggleMode_Click;
            // 
            // SoilMoisture
            // 
            SoilMoisture.AutoSize = true;
            SoilMoisture.Location = new Point(135, 74);
            SoilMoisture.Name = "SoilMoisture";
            SoilMoisture.Size = new Size(134, 15);
            SoilMoisture.TabIndex = 2;
            SoilMoisture.Text = "Влажность почвы: 40%";
            // 
            // PumpStatus
            // 
            PumpStatus.AutoSize = true;
            PumpStatus.Location = new Point(135, 102);
            PumpStatus.Name = "PumpStatus";
            PumpStatus.Size = new Size(109, 15);
            PumpStatus.TabIndex = 3;
            PumpStatus.Text = "Насос не работает";
            // 
            // ModeStatus
            // 
            ModeStatus.AutoSize = true;
            ModeStatus.Location = new Point(135, 130);
            ModeStatus.Name = "ModeStatus";
            ModeStatus.Size = new Size(92, 15);
            ModeStatus.TabIndex = 5;
            ModeStatus.Text = "Режим: Ручной";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ModeStatus);
            Controls.Add(PumpStatus);
            Controls.Add(SoilMoisture);
            Controls.Add(btnToggleMode);
            Controls.Add(btnTogglePump);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnTogglePump;
        private Button btnToggleMode;
        private Label SoilMoisture;
        private Label PumpStatus;
        private Label ModeStatus;
    }
}
