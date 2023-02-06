namespace WallpaperFlux.Winform
{
    partial class WallpaperForm
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
            this.pictureBoxWallpaper = new System.Windows.Forms.PictureBox();
            this.panelWallpaper = new System.Windows.Forms.Panel();
            this.timerAudioFixer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWallpaper)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxWallpaper
            // 
            this.pictureBoxWallpaper.Location = new System.Drawing.Point(124, 71);
            this.pictureBoxWallpaper.Name = "pictureBoxWallpaper";
            this.pictureBoxWallpaper.Size = new System.Drawing.Size(169, 140);
            this.pictureBoxWallpaper.TabIndex = 1;
            this.pictureBoxWallpaper.TabStop = false;
            // 
            // panelWallpaper
            // 
            this.panelWallpaper.Location = new System.Drawing.Point(465, 245);
            this.panelWallpaper.Name = "panelWallpaper";
            this.panelWallpaper.Size = new System.Drawing.Size(200, 100);
            this.panelWallpaper.TabIndex = 2;
            // 
            // timerAudioFixer
            // 
            this.timerAudioFixer.Enabled = true;
            this.timerAudioFixer.Tick += new System.EventHandler(this.timerAudioFixer_Tick);
            // 
            // WallpaperForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelWallpaper);
            this.Controls.Add(this.pictureBoxWallpaper);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "WallpaperForm";
            this.Text = "WallpaperForm";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWallpaper)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxWallpaper;
        private System.Windows.Forms.Panel panelWallpaper;
        private System.Windows.Forms.Timer timerAudioFixer;
    }
}