namespace AudioSignalMonitor;

/// <summary>
/// 定義應用程式的進入點，並將啟動流程保持精簡，
/// 以降低 UI 與即時音訊執行緒在啟動階段被阻塞的風險。
/// </summary>
static class Program
{
    /// <summary>
    /// 初始化 WinForms 設定並啟動主視窗。
    /// 採用輕量啟動流程可避免延後音訊擷取的初始化，
    /// 以維持即時性與避免啟動時的音訊緩衝累積。
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}