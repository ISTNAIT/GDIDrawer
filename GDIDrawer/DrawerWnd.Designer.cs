namespace GDIDrawer
{
    internal partial class DrawerWnd
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
            this.UI_TIM_RENDER = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // UI_TIM_RENDER
            // 
            this.UI_TIM_RENDER.Interval = 10;
            this.UI_TIM_RENDER.Tick += new System.EventHandler(this.UI_TIM_RENDER_Tick);
            // 
            // DrawerWnd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "DrawerWnd";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "GDI+ Drawer V1.0";
            this.Load += new System.EventHandler(this.DrawerWnd_Load);
            this.Shown += new System.EventHandler(this.DrawerWnd_Shown);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DrawerWnd_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DrawerWnd_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.DrawerWnd_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DrawerWnd_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.DrawerWnd_MouseMove);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer UI_TIM_RENDER;
    }
}