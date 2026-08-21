# C# Avalonia Skia JC.Waveform.Core

Github:https://github.com/Jasson-Chou/MyAvaloniaUI.Example/tree/master/JC.Waveform.Core  
重點摘要: WaveformCore 為純計算的波形建置核心：將原始 float 值陣列依繪製區域、值域與變換參數轉為螢幕座標點陣列，點數過多時自動以 min-max 下採樣，並透過 AsPoints<T> 零複製轉型供 Skia 等繪圖引擎使用。

## 概述

`WaveformCore.cs` 是 `JC.Waveform.Core` 的核心運算模組，**不含任何繪圖邏輯**。職責單一：把原始資料值陣列（`ReadOnlySpan<float>`）依繪製區域、值域與變換參數，轉換成可直接繪製的螢幕座標點陣列。渲染端（Skia、Avalonia 等）只需將結果連線繪出。

<aside>
💡

設計重點：計算與繪製分離。`WaveformCore` 可獨立做單元測試，也可搭配任何繪圖引擎重複使用。

</aside>

## 核心型別

| 型別 | 性質 | 用途 |
| --- | --- | --- |
| `WaveformTransform` | `readonly record struct` | 平移與縮放參數：`XOffset`、`YOffset`、`XScale`、`YScale`，預設值為 `Identity`（不變換） |
| `WaveformPoint` | `readonly record struct`，`[StructLayout(Sequential)]` | 螢幕座標點 `(X, Y)`；Sequential 佈局是為了讓 `AsPoints<T>` 能安全做記憶體轉型 |
| `ValueRange` | `readonly record struct` | 值域 `(Min, Max)`，提供 `Span`（範圍寬度）與 `Contains` |
| `WaveformBuildResult` | `readonly record struct` | `Build` 的回傳結果，見下表 |

### WaveformBuildResult 欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| `Points` | `WaveformPoint[]` | 實際要繪製的螢幕座標點 |
| `ActualIndexes` | `int[]` | 每個點對應回原始資料的索引（供 hover/游標查值用） |
| `XStep` | `float` | X 軸每筆資料的像素步長 |
| `IsDownSampled` | `bool` | 是否有進行下採樣 |

## WaveformCore.Build 演算法流程

```csharp
public static WaveformBuildResult Build(
    ReadOnlySpan<float> values,   // 原始資料值
    in RectangleF rect,           // 繪製區域
    in ValueRange valueRange,     // 預期值域（決定 Y 軸映射）
    in WaveformTransform transform,
    int? fixedPointCount = null)  // 固定點數；null 則以 values.Length 計算
```

1. **空值防呆**：`values` 為空時直接回傳 `WaveformBuildResult.Empty`。
2. **Y 軸映射**：`yScaleFactor = height * YScale / valueRange.Span`；`y = top + (Max - value) * yScaleFactor - YOffset`。值越大 Y 越小（螢幕座標向下遞增），`Span == 0` 時因子退化為 `1.0`。
3. **X 軸步長**：有 `fixedPointCount` 時 `xStep = width * XScale / (fixedPointCount - 1)`，否則以 `values.Length - 1` 計算。
4. **可視範圍裁切**：由 `XOffset / xStep` 推得 `startIndex`，再依可視寬度算出 `endIndex`，只處理視窗內的資料（平移/縮放時不重算全部）。
5. **兩種輸出模式**（以 `destCount` 與可視欄寬 `canShowColumns * 2` 比較）：
    - **直接模式**（點數少）：逐點轉換成座標，`IsDownSampled = false`。
    - **Min-Max 下採樣**（點數多）：每個像素欄取該區間的最小值與最大值各一點（依索引先後排序輸出），輸出固定 `canShowColumns * 2` 個點，`IsDownSampled = true`。此法保留波形峰谷外觀，避免繪製數十萬點造成效能問題。

## WaveformCoreExtensions.AsPoints

```csharp
public static ReadOnlySpan<T> AsPoints<T>(this WaveformPoint[] pts) where T : struct
    => MemoryMarshal.Cast<WaveformPoint, T>(pts);
```

- 以 `MemoryMarshal.Cast` 將 `WaveformPoint[]` **零複製**轉為目標點結構（如 `SKPoint`）的 `ReadOnlySpan<T>`。
- 前提：目標型別的記憶體佈局須與 `WaveformPoint`（兩個 `float`）相同。
- ⚠️ 回傳的 Span 仍指向原陣列，原陣列被修改或釋放後即失效，不可長期保存。

## 使用方式

```csharp
// 1. 準備參數
float[] values = LoadSamples();
var rect = new RectangleF(0, 0, canvasWidth, canvasHeight);
var range = new ValueRange(-1f, 1f);
var transform = WaveformTransform.Identity; // 或指定平移/縮放

// 2. 建置波形點
var result = WaveformCore.Build(values, rect, range, transform);
if (result.Points.Length == 0) return;

// 3. 零複製轉為 SKPoint 後交給 Skia 繪製
var skPoints = result.Points.AsPoints<SKPoint>();
canvas.DrawPoints(SKPointMode.Polygon, skPoints, paint);

// 4. 需要 hover 查值時，用 ActualIndexes 找回原始資料索引
int dataIndex = result.ActualIndexes[nearestPointIndex];
```

## 維護注意事項

- **座標系**：Y 軸以 `valueRange.Max` 對應區域頂端，維護 Y 映射時注意方向相反。
- **下採樣門檻**：`destCount <= canShowColumns * 2` 是模式切換條件，改動時需同步確認兩條路徑的 `ActualIndexes` 一致性。
- **邊界情況**：`values.Length == 1` 時 `xStep` 分母為 0 會得到 `Infinity`，若日後支援單點資料需另行處理。
- **效能**：全程使用 `ReadOnlySpan`、`in` 參數與 struct，無多餘配置；下採樣內層迴圈為 O(destCount)，整體與可視資料量成線性。
- **相依性**：僅依賴 `System.Drawing`（`RectangleF`）與 `System.Runtime.InteropServices`，無 UI 框架相依。