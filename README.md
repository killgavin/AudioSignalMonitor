# 音訊訊號監測器

使用 NAudio 擷取 16-bit、44.1kHz、單聲道音訊並即時顯示波形的 WinForms 輕量工具。

## 功能
- 選擇輸入裝置
- 啟動 / 停止監測
- 固定大小環狀緩衝儲存最新樣本
- 以計時器控制重繪，降低 CPU 使用量
- 即時計算 Peak（dBFS）與 RMS（dBFS）
- 錄音品質狀態判斷（Too Quiet / Optimal / Clipping）

## 建置與執行
- 建置：`dotnet build`
- 執行：`dotnet run`

## 訊號驗證
### 計算方式
- 16-bit PCM 轉 float：`normalized = sample / 32768f`，範圍約為 `-1.0 ~ 1.0`
- Peak（線性）：取每批樣本 `abs(normalized)` 的最大值
- RMS（線性）：`sqrt(sum(normalized^2) / N)`
- dBFS：`20 * log10(linear)`
- 避免 `log10(0)`：以極小值（`1e-9`）作為下限，再限制最低顯示為 `-120 dBFS`

### 品質區間與門檻
- **Clipping Risk**：`Peak >= -1 dBFS`
  - 理由：0 dBFS 是數位滿刻度，接近滿刻度時很容易因瞬間峰值或後續處理造成失真，`-1 dBFS` 提供保守頭部空間。
- **Too Quiet**：`RMS < -35 dBFS`（且未達 Clipping）
  - 理由：RMS 反映平均能量，低於約 `-35 dBFS` 常見於收音太小或 SNR 偏低情境，後續增益會放大底噪。
- **Optimal**：其餘範圍
  - 理由：代表平均能量與峰值都落在相對安全且可用的錄音區間。

### UI 顯示
- 顯示 `Peak: x.x dBFS`
- 顯示 `RMS: x.x dBFS`
- 顯示狀態：`Too Quiet` / `Optimal` / `Clipping`

### 即時效能說明
- 在音訊回呼中只做單次迴圈，同時計算 Peak 與 RMS 累積量，避免重複掃描樣本。
- UI 以計時器更新（250ms）而不是每個樣本重繪，降低 WinForms 執行緒負擔。
