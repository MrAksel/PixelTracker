namespace PixelTracker
{
    partial class FormTrackOverlay
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
            this.components = new System.ComponentModel.Container();
            this.tmrRedraw = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tmrRedraw
            // 
            this.tmrRedraw.Enabled = true;
            this.tmrRedraw.Interval = 500;
            this.tmrRedraw.Tick += new System.EventHandler(this.tmrRedraw_Tick);
            // 
            // FormTrackOverlay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.ControlBox = false;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimizeBox = false;
            this.Name = "FormTrackOverlay";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Pixel tracker";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.White;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormTrackOverlay_FormClosing);
            this.SizeChanged += new System.EventHandler(this.FormTrackOverlay_SizeChanged);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer tmrRedraw;
    }
}

