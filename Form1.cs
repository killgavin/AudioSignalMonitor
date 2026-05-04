using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;

namespace AudioSignalMonitor;

/// <summary>
/// 以 NAudio 提供低延遲的波形監測，並使用固定大小的環狀緩衝區。
/// 將音訊擷取與 UI 繪製分離，可避免 UI 卡頓造成音訊回呼延遲或掉樣。
/// </summary>
public partial class Form1 : Form
{
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 1;
    private const int RingBufferSamples = 16384;
    private const float MinDbfs = -120f;
    private const float ClippingPeakDbfs = -1.0f;
    private const float TooQuietRmsDbfs = -35.0f;

    private readonly SampleRingBuffer ringBuffer = new(RingBufferSamples);
    private readonly short[] drawBuffer = new short[RingBufferSamples];
    private WaveInEvent? waveIn;
    private float lastPeakDbfs = MinDbfs;
    private float lastRmsDbfs = MinDbfs;
    private int lastQuality = (int)RecordingQuality.TooQuiet;

    private enum RecordingQuality
    {
        TooQuiet,
        Optimal,
        ClippingRisk
    }

    /// <summary>
    /// 初始化表單並將建構子內的工作量控制在最低，
    /// 避免在視窗建立時阻塞訊息迴圈而影響後續音訊擷取。
    /// </summary>
    public Form1()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 載入輸入裝置清單並確保 UI 完成初始化後再啟動音訊擷取。
    /// 這樣可避免開機即啟用裝置而造成不必要的資源占用或啟動延遲。
    /// </summary>
    private void Form1_Load(object sender, EventArgs e)
    {
        LoadInputDevices();
    }

    /// <summary>
    /// 視窗關閉時停止擷取，避免背景音訊執行緒在 UI 結束後仍持續運作。
    /// </summary>
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        StopMonitoring();
    }

    /// <summary>
    /// 以固定格式啟動音訊擷取，並使用較小的緩衝以保持低延遲。
    /// 緩衝大小控制在中等範圍可兼顧 CPU 負擔與抖動容忍度，
    /// 避免過小導致頻繁回呼、過大造成可視化延遲。
    /// </summary>
    private void StartButton_Click(object sender, EventArgs e)
    {
        StartMonitoring();
    }

    /// <summary>
    /// 停止擷取並釋放裝置，讓其他應用程式可以使用相同的輸入來源。
    /// </summary>
    private void StopButton_Click(object sender, EventArgs e)
    {
        StopMonitoring();
    }

    /// <summary>
    /// 以固定頻率刷新波形與訊號指示，而非每個樣本都重繪。
    /// 透過計時器控制重繪頻率可降低 CPU 成本並避免 UI 執行緒過載。
    /// </summary>
    private void UiTimer_Tick(object sender, EventArgs e)
    {
        UpdateLevelIndicator();
        waveformPanel.Invalidate();
    }

    /// <summary>
    /// 將環狀緩衝中的最新樣本繪製成波形。
    /// 使用每像素取最小/最大值的方法，
    /// 即使取樣率高也能維持快速繪製與低 CPU 使用率。
    /// </summary>
    private void WaveformPanel_Paint(object sender, PaintEventArgs e)
    {
        DrawWaveform(e.Graphics, waveformPanel.ClientRectangle);
    }

    /// <summary>
    /// 載入可用的輸入裝置並以裝置索引建立對應。
    /// 將 SelectedIndex 作為 DeviceNumber 可避免額外的索引轉換成本。
    /// </summary>
    private void LoadInputDevices()
    {
        deviceComboBox.Items.Clear();

        int count = WaveInEvent.DeviceCount;
        if (count == 0)
        {
            deviceComboBox.Items.Add("找不到輸入裝置");
            deviceComboBox.SelectedIndex = 0;
            startButton.Enabled = false;
            statusLabel.Text = "狀態：無裝置";
            return;
        }

        for (int i = 0; i < count; i++)
        {
            WaveInCapabilities caps = WaveInEvent.GetCapabilities(i);
            deviceComboBox.Items.Add($"{i}: {caps.ProductName}");
        }

        deviceComboBox.SelectedIndex = 0;
        startButton.Enabled = true;
        statusLabel.Text = "狀態：待命";
        peakLabel.Text = $"Peak：{MinDbfs:F1} dBFS";
        rmsLabel.Text = $"RMS：{MinDbfs:F1} dBFS";
    }

    /// <summary>
    /// 以指定格式初始化並啟動 WaveIn 擷取。
    /// 音訊回呼內保持精簡，可降低緩衝溢位與掉樣風險。
    /// </summary>
    private void StartMonitoring()
    {
        if (waveIn != null)
        {
            return;
        }

        if (deviceComboBox.SelectedIndex < 0)
        {
            return;
        }

        waveIn = new WaveInEvent
        {
            DeviceNumber = deviceComboBox.SelectedIndex,
            WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
            BufferMilliseconds = 30,
            NumberOfBuffers = 3
        };

        waveIn.DataAvailable += OnDataAvailable;
        waveIn.RecordingStopped += OnRecordingStopped;

        ringBuffer.Clear();
        Volatile.Write(ref lastPeakDbfs, MinDbfs);
        Volatile.Write(ref lastRmsDbfs, MinDbfs);
        Volatile.Write(ref lastQuality, (int)RecordingQuality.TooQuiet);

        waveIn.StartRecording();
        uiTimer.Start();

        startButton.Enabled = false;
        stopButton.Enabled = true;
        deviceComboBox.Enabled = false;
        statusLabel.Text = "狀態：監測中";
    }

    /// <summary>
    /// 停止擷取、解除事件並釋放資源。
    /// 完整清理可避免音訊執行緒殘留或裝置鎖定造成的後續啟動失敗。
    /// </summary>
    private void StopMonitoring()
    {
        WaveInEvent? current = waveIn;
        if (current == null)
        {
            return;
        }

        waveIn = null;
        uiTimer.Stop();

        current.DataAvailable -= OnDataAvailable;
        current.RecordingStopped -= OnRecordingStopped;
        current.StopRecording();
        current.Dispose();

        startButton.Enabled = true;
        stopButton.Enabled = false;
        deviceComboBox.Enabled = true;
        statusLabel.Text = "狀態：已停止";
    }

    /// <summary>
    /// 將進來的樣本寫入環狀緩衝並計算峰值。
    /// 回呼內僅做必要計算，能降低慢速系統上的即時音訊中斷風險。
    /// </summary>
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        int sampleCount = e.BytesRecorded / 2;
        if (sampleCount <= 0)
        {
            return;
        }

        ReadOnlySpan<byte> bytes = e.Buffer.AsSpan(0, sampleCount * 2);
        ReadOnlySpan<short> samples = MemoryMarshal.Cast<byte, short>(bytes);

        ringBuffer.Write(samples);

        float peakLinear = 0f;
        double sumSquares = 0d;

        for (int i = 0; i < samples.Length; i++)
        {
            float normalized = samples[i] / 32768f;
            float abs = MathF.Abs(normalized);
            if (abs > peakLinear)
            {
                peakLinear = abs;
            }

            sumSquares += normalized * normalized;
        }

        float rmsLinear = MathF.Sqrt((float)(sumSquares / sampleCount));
        float peakDbfs = ToDbfs(peakLinear);
        float rmsDbfs = ToDbfs(rmsLinear);
        RecordingQuality quality = EvaluateQuality(peakDbfs, rmsDbfs);

        Volatile.Write(ref lastPeakDbfs, peakDbfs);
        Volatile.Write(ref lastRmsDbfs, rmsDbfs);
        Volatile.Write(ref lastQuality, (int)quality);
    }

    /// <summary>
    /// 當裝置異常停止時，確保 UI 狀態一致。
    /// 必須切回 UI 執行緒更新控制項，以避免跨執行緒例外。
    /// </summary>
    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnRecordingStopped(sender, e)));
            return;
        }

        StopMonitoring();
    }

    /// <summary>
    /// 更新 Peak / RMS 以及錄音品質狀態。
    /// 以 dBFS 呈現可直接反映與數位滿刻度的距離。
    /// </summary>
    private void UpdateLevelIndicator()
    {
        float peakDbfs = Volatile.Read(ref lastPeakDbfs);
        float rmsDbfs = Volatile.Read(ref lastRmsDbfs);
        RecordingQuality quality = (RecordingQuality)Volatile.Read(ref lastQuality);

        peakLabel.Text = $"Peak：{peakDbfs:F1} dBFS";
        rmsLabel.Text = $"RMS：{rmsDbfs:F1} dBFS";

        switch (quality)
        {
            case RecordingQuality.ClippingRisk:
                statusLabel.Text = "狀態：Clipping";
                statusLabel.ForeColor = Color.OrangeRed;
                break;
            case RecordingQuality.Optimal:
                statusLabel.Text = "狀態：Optimal";
                statusLabel.ForeColor = Color.LimeGreen;
                break;
            default:
                statusLabel.Text = "狀態：Too Quiet";
                statusLabel.ForeColor = Color.DarkRed;
                break;
        }
    }

    private static RecordingQuality EvaluateQuality(float peakDbfs, float rmsDbfs)
    {
        if (peakDbfs >= ClippingPeakDbfs)
        {
            return RecordingQuality.ClippingRisk;
        }

        if (rmsDbfs < TooQuietRmsDbfs)
        {
            return RecordingQuality.TooQuiet;
        }

        return RecordingQuality.Optimal;
    }

    private static float ToDbfs(float linear)
    {
        float safe = MathF.Max(linear, 1e-9f);
        return MathF.Max(20f * MathF.Log10(safe), MinDbfs);
    }

    /// <summary>
    /// 以每像素最小/最大值方式繪製近期樣本。
    /// 這種方式避免建立大量點列，可降低 GC 壓力並保持低 CPU 使用率。
    /// </summary>
    private void DrawWaveform(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);

        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        int sampleCount = ringBuffer.CopyLatest(drawBuffer);
        float centerY = bounds.Height / 2f;

        using Pen midlinePen = new(Color.FromArgb(64, 255, 255, 255));
        graphics.DrawLine(midlinePen, 0, centerY, bounds.Width, centerY);

        if (sampleCount == 0)
        {
            return;
        }

        int samplesPerPixel = Math.Max(1, sampleCount / bounds.Width);
        using Pen wavePen = new(Color.Lime, 1f);

        for (int x = 0; x < bounds.Width; x++)
        {
            int start = x * samplesPerPixel;
            if (start >= sampleCount)
            {
                break;
            }

            int end = Math.Min(start + samplesPerPixel, sampleCount);
            short min = short.MaxValue;
            short max = short.MinValue;

            for (int i = start; i < end; i++)
            {
                short sample = drawBuffer[i];
                if (sample < min)
                {
                    min = sample;
                }
                if (sample > max)
                {
                    max = sample;
                }
            }

            float minY = centerY - (min / 32768f) * centerY;
            float maxY = centerY - (max / 32768f) * centerY;

            graphics.DrawLine(wavePen, x, minY, x, maxY);
        }
    }
}

/// <summary>
/// 提供固定大小的 16-bit PCM 環狀緩衝。
/// 以單一鎖確保寫入與複製一致性，避免資料撕裂造成波形異常。
/// </summary>
internal sealed class SampleRingBuffer
{
    private readonly short[] buffer;
    private readonly object gate = new();
    private int writeIndex;
    private bool wrapped;

    /// <summary>
    /// 建立固定容量的環狀緩衝。
    /// 固定大小可避免長時間監測導致記憶體無限制成長。
    /// </summary>
    /// <param name="capacity">可容納的樣本數量。</param>
    public SampleRingBuffer(int capacity)
    {
        buffer = new short[capacity];
    }

    /// <summary>
    /// 清除緩衝區狀態，讓後續讀取以新資料為準。
    /// </summary>
    public void Clear()
    {
        lock (gate)
        {
            Array.Clear(buffer, 0, buffer.Length);
            writeIndex = 0;
            wrapped = false;
        }
    }

    /// <summary>
    /// 寫入樣本到環狀緩衝並覆寫最舊資料。
    /// 覆寫行為可維持固定延遲並避免額外配置。
    /// </summary>
    /// <param name="samples">要寫入的樣本。</param>
    public void Write(ReadOnlySpan<short> samples)
    {
        lock (gate)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                buffer[writeIndex] = samples[i];
                writeIndex++;
                if (writeIndex >= buffer.Length)
                {
                    writeIndex = 0;
                    wrapped = true;
                }
            }
        }
    }

    /// <summary>
    /// 依時間順序複製最新樣本到目的陣列。
    /// 這個讀取流程專為 UI 繪製最佳化，以縮短鎖定時間。
    /// </summary>
    /// <param name="destination">接收樣本的緩衝區。</param>
    /// <returns>實際複製的樣本數。</returns>
    public int CopyLatest(Span<short> destination)
    {
        lock (gate)
        {
            int available = wrapped ? buffer.Length : writeIndex;
            if (available == 0)
            {
                return 0;
            }

            int count = Math.Min(destination.Length, available);
            int start = writeIndex - count;
            if (start < 0)
            {
                start += buffer.Length;
            }

            int firstPart = Math.Min(count, buffer.Length - start);
            buffer.AsSpan(start, firstPart).CopyTo(destination);

            int remaining = count - firstPart;
            if (remaining > 0)
            {
                buffer.AsSpan(0, remaining).CopyTo(destination.Slice(firstPart));
            }

            return count;
        }
    }
}

/// <summary>
/// 啟用雙重緩衝的 Panel，可在高頻重繪時降低閃爍。
/// </summary>
internal sealed class BufferedPanel : Panel
{
    /// <summary>
    /// 啟用雙重緩衝並在尺寸變更時重繪，以避免高頻繪製的撕裂感。
    /// </summary>
    public BufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}
