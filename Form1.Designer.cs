namespace AudioSignalMonitor;

partial class Form1
{
    /// <summary>
    ///  設計工具需要的變數。
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private ComboBox deviceComboBox;
    private Button startButton;
    private Button stopButton;
    private Label statusLabel;
    private BufferedPanel waveformPanel;
    private System.Windows.Forms.Timer uiTimer;

    /// <summary>
    ///  清理使用中的資源。
    /// </summary>
    /// <param name="disposing">如果需要釋放受控資源則為 true，否則為 false。</param>
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
    ///  設計工具需要的方法，請勿使用程式碼編輯器修改其內容。
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.deviceComboBox = new System.Windows.Forms.ComboBox();
        this.startButton = new System.Windows.Forms.Button();
        this.stopButton = new System.Windows.Forms.Button();
        this.statusLabel = new System.Windows.Forms.Label();
        this.waveformPanel = new AudioSignalMonitor.BufferedPanel();
        this.uiTimer = new System.Windows.Forms.Timer(this.components);
        this.SuspendLayout();
        // 
        // deviceComboBox
        // 
        this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.deviceComboBox.FormattingEnabled = true;
        this.deviceComboBox.Location = new System.Drawing.Point(12, 12);
        this.deviceComboBox.Name = "deviceComboBox";
        this.deviceComboBox.Size = new System.Drawing.Size(320, 23);
        this.deviceComboBox.TabIndex = 0;
        // 
        // startButton
        // 
        this.startButton.Location = new System.Drawing.Point(348, 12);
        this.startButton.Name = "startButton";
        this.startButton.Size = new System.Drawing.Size(90, 23);
        this.startButton.TabIndex = 1;
        this.startButton.Text = "開始";
        this.startButton.UseVisualStyleBackColor = true;
        this.startButton.Click += new System.EventHandler(this.StartButton_Click);
        // 
        // stopButton
        // 
        this.stopButton.Enabled = false;
        this.stopButton.Location = new System.Drawing.Point(444, 12);
        this.stopButton.Name = "stopButton";
        this.stopButton.Size = new System.Drawing.Size(90, 23);
        this.stopButton.TabIndex = 2;
        this.stopButton.Text = "停止";
        this.stopButton.UseVisualStyleBackColor = true;
        this.stopButton.Click += new System.EventHandler(this.StopButton_Click);
        // 
        // statusLabel
        // 
        this.statusLabel.AutoSize = true;
        this.statusLabel.Location = new System.Drawing.Point(556, 15);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new System.Drawing.Size(101, 15);
        this.statusLabel.TabIndex = 3;
        this.statusLabel.Text = "訊號：未知";
        // 
        // waveformPanel
        // 
        this.waveformPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
        | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.waveformPanel.BackColor = System.Drawing.Color.Black;
        this.waveformPanel.Location = new System.Drawing.Point(12, 48);
        this.waveformPanel.Name = "waveformPanel";
        this.waveformPanel.Size = new System.Drawing.Size(876, 390);
        this.waveformPanel.TabIndex = 4;
        this.waveformPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.WaveformPanel_Paint);
        // 
        // uiTimer
        // 
        this.uiTimer.Interval = 250; // 約 4 FPS 更新頻率
        this.uiTimer.Tick += new System.EventHandler(this.UiTimer_Tick);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(900, 450);
        this.Controls.Add(this.waveformPanel);
        this.Controls.Add(this.statusLabel);
        this.Controls.Add(this.stopButton);
        this.Controls.Add(this.startButton);
        this.Controls.Add(this.deviceComboBox);
        this.MinimumSize = new System.Drawing.Size(700, 380);
        this.Name = "Form1";
        this.Text = "音訊訊號監測";
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        this.Load += new System.EventHandler(this.Form1_Load);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
