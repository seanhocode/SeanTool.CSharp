# WPFTool DataGrid 與 ModelEditor 執行規劃

## 1. ModelEditor 的 Review 結論、目標及處理事項

### Review 結論

目前 `ModelEditor` 已具備以 Reflection 掃描 Model property、依型別產生基本編輯器、支援 Enum、DateTime、檔案路徑、資料夾路徑與巢狀物件編輯的基礎能力。

目前主要問題如下：

- `PropertyItem` 將 `IEnumerable` 排除在複合物件之外，尚未具備集合屬性的編輯或檢視能力。
- 型別轉換失敗目前只寫入 Debug，使用者無法知道哪個欄位輸入錯誤。
- Nullable、空值、數字格式與文化格式的處理仍不完整。
- 目前只處理同時具備 getter 與 setter 的 property，尚未區分編輯模式與檢視模式下的唯讀 property。
- `Save` 會直接將暫存值寫回 Model，缺少明確的驗證、取消與錯誤回報流程。
- `TargetObject` 改變或設為 null 時，DataContext 與畫面狀態需要更完整的生命週期處理。
- 外層使用 `ScrollViewer` 搭配 `ItemsControl`，property 數量增加時沒有 virtualization。
- metadata 掃描、InputType 判斷與型別轉換尚未抽離，測試與後續擴充的成本較高。

### 目標

- 根據 Model 的 property metadata 自動產生穩定且可預期的檢視或編輯畫面。
- 正確支援基本型別、Nullable、Enum、DateTime、檔案/資料夾路徑、巢狀物件與唯讀 property。
- 將輸入轉換、驗證、暫存、取消及保存責任分離。
- 讓錯誤能回到對應的 property，並可由 UI 顯示。
- 保留以 attribute 或設定覆寫預設 editor 的擴充能力。
- 在不預先加入 DataGrid 整合邏輯的前提下，先完成一個可獨立使用、可測試的 ModelEditor。

### 處理事項

1. 定義 property metadata 規則。
   - 支援 `DisplayNameAttribute`。
   - 定義 property 順序與排除規則。
   - 定義只讀 property 在檢視模式下的顯示方式。
   - 保留 `EditorPathAttribute` 等自訂 editor metadata。

2. 重整 `PropertyItem` 的型別處理。
   - 支援 Nullable 與 null 值。
   - 補齊數字、日期與 Enum 的安全轉換。
   - 對轉換失敗保留錯誤狀態，不靜默吞掉錯誤。
   - 讓 UI 可取得欄位錯誤訊息。

3. 重整編輯生命週期。
   - 載入 Model 時建立暫存值。
   - 編輯時只修改暫存值。
   - 巢狀物件編輯也必須使用獨立暫存值，避免子編輯器在父層 Save 前寫回原始 Model。
   - Save 前執行完整驗證。
   - 所有欄位驗證與回寫皆成功後才寫回 Model；任一欄位失敗時不得部分寫回或顯示儲存成功。
   - 支援 Cancel 並丟棄暫存修改。
   - 明確處理 `TargetObject` 變更與 null。

4. 改善 UI 呈現。
   - 將 property metadata 與 UI template 判斷分離。
   - 評估使用可 virtualization 的清單控制項。
   - 補齊檢視模式與編輯模式的唯讀行為。
   - 保持基本 editor 與巢狀物件 editor 的責任清楚。

5. 補充測試。
   - property 掃描與顯示名稱。
   - Enum、DateTime、Nullable、數字與空值。
   - 無效輸入與錯誤回報。
   - Save、Cancel 與 Model 回寫。
   - 唯讀 property 與 null `TargetObject`。
   - 現有檔案與資料夾路徑 editor。

### 完成條件

- 單一 Model 的檢視、編輯、驗證、保存與取消流程可獨立運作。
- 型別轉換失敗時，UI 能定位並顯示錯誤欄位。
- 儲存失敗時，Model 不得處於部分寫回狀態，且 UI 不得顯示儲存成功。
- Cancel 能丟棄基本欄位與巢狀物件的暫存修改。
- 既有 ModelEditor demo 行為不退化。
- 純邏輯部分具備自動化測試，UI 部分具備人工測試案例。
- 完成前不加入 DynamicDataGrid 專用程式碼。

## 2. DataGrid 的 Review 結論、目標及處理事項

### Review 結論

目前 `DynamicDataGrid` 已使用 WPF `DataGrid`，並明確開啟 row virtualization、recycling 與 pixel scrolling；column virtualization 尚未明確啟用。控制項具備基本資料繫結、手動欄位定義、格式化及編輯能力。

目前主要問題如下：

- `DataSource` 只是直接指派給 `ItemsSource`，尚未有篩選、排序或查詢模型。
- 欄位必須透過 `ColumnDefinitions` 手動建立，尚未根據 Model property 自動產生。
- virtualization 只減少畫面上的 row control 數量，無法避免一次列舉、篩選或排序全部資料。
- 一般 `IEnumerable` 不適合直接承載千萬筆資料；若要支援此規模，必須採用分頁與資料來源端查詢。
- `DynamicDataGridViewModel` 尚未實際承擔狀態與邏輯。
- 目前測試只有小型 UI demo，尚未驗證大量資料、篩選效能與記憶體使用量。

### 目標

- 使用者在不額外設定的情況下，能依 Model property 自動產生合理欄位。
- 依欄位型別提供接近 Excel 的篩選體驗。
- 支援欄位排序、清除篩選、欄位格式與手動設定覆寫。
- 對本地集合提供良好的 virtualization 與可接受的操作效能。
- 對百萬至千萬筆資料提供可替換的分頁資料來源模式，而不是一次載入全部資料。
- 將欄位 metadata、篩選條件與資料查詢責任拆開，讓純邏輯可測試。

### 處理事項

1. 定義 DataGrid API 與資料模式。
   - 保留 `IEnumerable` 作為本地資料來源入口。
   - 區分本地集合模式與分頁資料提供者模式。
   - 依資料來源是否支援新增、刪除與寫入，決定是否開放對應的 DataGrid 編輯操作；不可變或唯讀 `IEnumerable` 僅提供檢視。
   - 定義欄位自動產生與 `ColumnDefinitions` 覆寫規則。
   - 定義欄位排除、標題、順序、寬度、格式與唯讀規則。

2. 完成本地資料模式。
   - 從資料項目型別自動掃描 public property。
   - 建立並快取 property metadata，避免每列反射。
   - 自動建立文字、數字、日期、Boolean、Enum 等欄位。
   - 支援 `DisplayNameAttribute` 與自訂欄位 metadata。
   - 保留手動欄位定義，且手動設定優先於自動預設。

3. 實作本地篩選與排序。
   - 文字欄位支援 Contains、StartsWith、Equals。
   - 數字與日期支援等於、大於、小於與區間。
   - Enum 與 Boolean 支援選項式篩選。
   - 支援 null/空值篩選與清除所有篩選。
   - 篩選狀態不得破壞原始資料集合。
   - 避免每次輸入都同步執行昂貴的全量操作，必要時加入 debounce。

4. 完成大量資料模式設計。
   - 定義可由使用者實作的 `IPagedDataProvider<T>` 或等價抽象。
   - provider 負責總筆數、分頁資料、篩選與排序查詢。
   - 支援非同步載入與取消過期查詢。
   - 顯示載入中、查詢失敗、無資料與總筆數狀態。
   - 避免建立千萬筆 Model instance 或將全量資料放入 UI collection。

5. 補充效能與功能測試。
   - 自動欄位產生與 metadata cache。
   - 各欄位型別的篩選與排序。
   - ObservableCollection 的新增、刪除與更新同步。
   - 10 萬筆以上資料的載入、篩選與記憶體基準。
   - 分頁 provider 的查詢取消、查詢競態與錯誤狀態。
   - 人工確認 virtualization、編輯、欄位寬度與 UX。

### 完成條件

- 不提供欄位設定時，能依 Model property 產生可用欄位。
- 本地資料可完成型別相符的篩選、排序與清除篩選。
- 手動欄位設定仍可覆寫自動設定。
- 資料來源不支援寫入時，控制項不得提供會失敗的新增、刪除或編輯操作。
- 大量資料不以「單純開啟 virtualization」作為千萬筆支援的保證。
- 需要千萬筆資料時，可透過分頁資料提供者完成查詢，而不必修改控制項核心 UI。
- 完成前不加入 ModelEditor 專用程式碼。

## 3. ModelEditor 與 DataGrid 完成後的合併事項

> 本分類只在 ModelEditor 與 DataGrid 各自完成並通過各自驗收後執行。前兩個控制項的 Review、邏輯調整與測試不得依賴本分類，避免在基礎能力尚未穩定前產生耦合。

### 合併目標

- 當 ModelEditor 遇到 `IEnumerable<T>` 或集合屬性時，自動使用已完成的 DataGrid 呈現集合內容。
- 使用者不需額外撰寫集合欄位設定，即可檢視或編輯集合項目。
- 集合項目可沿用 DataGrid 的自動欄位、篩選、排序與大量資料模式。
- 使用者可從 DataGrid 選取集合項目，再以已完成的 ModelEditor 編輯單筆資料。
- 保存時能正確處理集合項目的新增、修改與刪除。

### 合併處理事項

1. 定義集合 editor 邊界。
   - `IEnumerable<T>` 預設至少支援檢視。
   - 只有可變集合才開放新增與刪除。
   - 明確處理 `IList<T>`、`ObservableCollection<T>`、`IReadOnlyList<T>` 與唯讀 `IEnumerable<T>`。
   - 明確處理 null collection 與無法建立實體集合的介面型別。

2. 建立集合屬性 editor。
   - 在 ModelEditor metadata 中新增 Collection input type。
   - 使用已完成的 DynamicDataGrid 顯示集合。
   - 集合元素為基本型別時提供簡化顯示與編輯方式。
   - 集合元素為複合 Model 時支援選取後開啟既有 ModelEditor。

3. 定義集合變更與保存策略。
   - 決定集合變更即時寫回或延後至 Save。
   - 保存前追蹤新增、修改與刪除項目。
   - Cancel 時還原集合變更。
   - 對不可變集合或只讀集合提供檢視模式，不強行寫回。

4. 串接大量資料能力。
   - 集合若使用分頁 provider，ModelEditor 不得自行列舉完整集合。
   - 延續 DataGrid 的非同步載入、篩選、排序與取消查詢機制。
   - 定義 ModelEditor 關閉或保存時的非同步保存流程。

5. 補充整合測試與人工測試。
   - 基本型別集合。
   - 複合 Model 集合。
   - null、空集合、唯讀集合與可變集合。
   - 集合項目新增、刪除、編輯、保存與取消。
   - 集合篩選後選取項目並開啟 ModelEditor。
   - 大型集合或分頁 provider 的載入與查詢競態。

### 合併完成條件

- ModelEditor 可自動辨識集合 property，並使用已完成的 DataGrid。
- ModelEditor 與 DataGrid 可各自獨立使用，既有 API 不因整合而失效。
- 集合的唯讀、可編輯與保存行為符合其實際型別能力。
- 基礎控制項的測試與整合測試分開，問題可定位到單一控制項或整合層。
- 完成整合後才補充統一的使用文件與完整 demo。
