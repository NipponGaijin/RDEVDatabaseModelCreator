namespace RDEVDatabaseModelCreator
{
    partial class MainForm
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.файлToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.открытьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.сохранитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.infoTxt = new System.Windows.Forms.TextBox();
            this.openedFolderTxt = new System.Windows.Forms.TextBox();
            this.buildObjectModelBtn = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.файлToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // файлToolStripMenuItem
            // 
            this.файлToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.открытьToolStripMenuItem,
            this.сохранитьToolStripMenuItem});
            this.файлToolStripMenuItem.Name = "файлToolStripMenuItem";
            this.файлToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.файлToolStripMenuItem.Text = "Файл";
            // 
            // открытьToolStripMenuItem
            // 
            this.открытьToolStripMenuItem.Name = "открытьToolStripMenuItem";
            this.открытьToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.открытьToolStripMenuItem.Text = "Открыть";
            this.открытьToolStripMenuItem.Click += new System.EventHandler(this.OpenFileMenuItem_Click);
            // 
            // сохранитьToolStripMenuItem
            // 
            this.сохранитьToolStripMenuItem.Name = "сохранитьToolStripMenuItem";
            this.сохранитьToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.сохранитьToolStripMenuItem.Text = "Сохранить";
            this.сохранитьToolStripMenuItem.Click += new System.EventHandler(this.SaveFileMenuItem_Click);
            // 
            // infoTxt
            // 
            this.infoTxt.Enabled = false;
            this.infoTxt.Location = new System.Drawing.Point(12, 322);
            this.infoTxt.Multiline = true;
            this.infoTxt.Name = "infoTxt";
            this.infoTxt.ReadOnly = true;
            this.infoTxt.Size = new System.Drawing.Size(776, 116);
            this.infoTxt.TabIndex = 1;
            // 
            // openedFolderTxt
            // 
            this.openedFolderTxt.Enabled = false;
            this.openedFolderTxt.Location = new System.Drawing.Point(12, 27);
            this.openedFolderTxt.Multiline = true;
            this.openedFolderTxt.Name = "openedFolderTxt";
            this.openedFolderTxt.ReadOnly = true;
            this.openedFolderTxt.Size = new System.Drawing.Size(776, 37);
            this.openedFolderTxt.TabIndex = 2;
            // 
            // buildObjectModelBtn
            // 
            this.buildObjectModelBtn.Location = new System.Drawing.Point(12, 71);
            this.buildObjectModelBtn.Name = "buildObjectModelBtn";
            this.buildObjectModelBtn.Size = new System.Drawing.Size(776, 245);
            this.buildObjectModelBtn.TabIndex = 3;
            this.buildObjectModelBtn.TabStop = false;
            this.buildObjectModelBtn.Text = "Сформировать объектную модель";
            this.buildObjectModelBtn.UseVisualStyleBackColor = true;
            this.buildObjectModelBtn.Click += new System.EventHandler(this.buildObjectModelBtn_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buildObjectModelBtn);
            this.Controls.Add(this.openedFolderTxt);
            this.Controls.Add(this.infoTxt);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "RDEVDatabaseModelCreator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem файлToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem открытьToolStripMenuItem;
        private System.Windows.Forms.TextBox infoTxt;
        private System.Windows.Forms.ToolStripMenuItem сохранитьToolStripMenuItem;
        private System.Windows.Forms.TextBox openedFolderTxt;
        private System.Windows.Forms.Button buildObjectModelBtn;
    }
}

