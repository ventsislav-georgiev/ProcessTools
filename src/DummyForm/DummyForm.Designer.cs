namespace DummyForm
{
    partial class DummyForm
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
            this.lbConsole = new System.Windows.Forms.ListBox();
            this.btnNextCycle = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbConsole
            // 
            this.lbConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbConsole.FormattingEnabled = true;
            this.lbConsole.Location = new System.Drawing.Point(0, 0);
            this.lbConsole.Name = "lbConsole";
            this.lbConsole.Size = new System.Drawing.Size(290, 192);
            this.lbConsole.TabIndex = 2;
            // 
            // btnNextCycle
            // 
            this.btnNextCycle.Location = new System.Drawing.Point(193, 157);
            this.btnNextCycle.Name = "btnNextCycle";
            this.btnNextCycle.Size = new System.Drawing.Size(75, 23);
            this.btnNextCycle.TabIndex = 3;
            this.btnNextCycle.Text = "Next Cycle";
            this.btnNextCycle.UseVisualStyleBackColor = true;
            this.btnNextCycle.Click += new System.EventHandler(this.btnNextCycle_Click);
            // 
            // DummyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(290, 192);
            this.Controls.Add(this.btnNextCycle);
            this.Controls.Add(this.lbConsole);
            this.Name = "DummyForm";
            this.ShowIcon = false;
            this.Text = "Dummy Form";
            this.Load += new System.EventHandler(this.DummyForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lbConsole;
        private System.Windows.Forms.Button btnNextCycle;

    }
}

