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
            this.panelWallpaper = new System.Windows.Forms.Panel();
            this.timerAudioFixer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // panelWallpaper
            // 
            this.panelWallpaper.Location = new System.Drawing.Point(542, 283);
            this.panelWallpaper.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelWallpaper.Name = "panelWallpaper";
            this.panelWallpaper.Size = new System.Drawing.Size(233, 115);
            this.panelWallpaper.TabIndex = 2;
            // 
            // timerAudioFixer
            // 
            this.timerAudioFixer.Enabled = true;
            this.timerAudioFixer.Tick += new System.EventHandler(this.timerAudioFixer_Tick);
            // 
            // WallpaperForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(933, 519);
            this.Controls.Add(this.panelWallpaper);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "WallpaperForm";
            this.Text = "WallpaperForm";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelWallpaper;
        private System.Windows.Forms.Timer timerAudioFixer;
    }
}