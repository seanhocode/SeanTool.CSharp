# SeanTool.CSharp Src 功能盤點與保留策略

## 文件範圍
- 本文件只盤點 Src 目前功能，且依你指示排除 SingleFileSetting。
- 盤點目標工具：ApiTool、FileTool、FormControlTool、JsonTool、ProcessTool、SqlTool、WPFTool、XmlTool、ZipTool。

## 功能清單

| 功能名稱 | 用途 | 所屬工具 | 保留與否 | 刪除原因 | 修改方向 |
|---|---|---|---|---|---|
| AddApiTool 服務註冊 | 將 IApiTool 與具名 HttpClient 註冊到 DI | ApiTool | 保留 |  | 補充可調 Timeout 與重試策略設定入口 |
| GetAsync | 非同步呼叫 GET API，支援 Header、Query、BearerToken | ApiTool | 保留 |  | 新增失敗分類測試與錯誤訊息標準化 |
| PostAsync | 非同步呼叫 POST API，支援 JSON payload | ApiTool | 保留 |  | 新增 4xx/5xx 與 Json 解析失敗測試 |
| CheckFolderExist | 檢查資料夾是否存在，可選擇自動建立 | FileTool | 保留 |  | 維持現狀 |
| CheckFileExist | 檢查檔案是否存在 | FileTool | 保留 |  | 維持現狀 |
| GetAllFileInFolder | 取得資料夾檔案清單，支援子層與深度限制 | FileTool | 保留 |  | 維持現狀 |
| DeleteFolder | 刪除資料夾與內容 | FileTool | 保留 |  | 目前用 Parallel.ForEach，建議補強錯誤回報或改用更可控策略 |
| ReadFile | 同步逐行讀檔 | FileTool | 保留 |  | 測試改為功能正確性優先，避免硬性效能門檻 |
| ReadFileAsync | 非同步逐行讀檔 | FileTool | 保留 |  | 補上大檔與取消權杖測試 |
| ReadFileAsyncByBuffer | 私有大檔 buffer 讀取邏輯 | FileTool | 待評估 | 私有功能未被公開 API 使用，維護成本高 | 若無外部需求可刪除，或改成公開且補測 |
| ProcessLinesAsync | 私有 channel 管線處理 | FileTool | 待評估 | 私有功能未被公開 API 使用 | 同上 |
| ReadFileToChannelAsync | 私有 channel 寫入邏輯 | FileTool | 待評估 | 私有功能未被公開 API 使用 | 同上 |
| WriteAsync | 非同步寫檔 | FileTool | 保留 |  | 補上覆蓋（含 append 與例外） |
| GetSelectFolderPath | 開啟資料夾選擇視窗 | FormControlTool | 保留 |  | 維持現狀 |
| GetSelectFilePath | 開啟檔案選擇視窗 | FormControlTool | 保留 |  | 目前回傳 FileName，建議改回傳完整路徑 |
| GenDataGridViewActionColumn | 產生 DataGridView 按鈕欄並綁定 callback | FormControlTool | 保留 |  | 加入防重複註冊與空值保護測試 |
| GridColumnDefinition | 定義 Grid 欄位屬性 | FormControlTool | 保留 |  | 維持現狀 |
| GenerateDefaultColumns | 由型別反射產生預設欄位 | FormControlTool | 保留 |  | 屬於純邏輯，建議補完整單元測試 |
| Grid 控制項 | 顯示與編輯集合資料 | FormControlTool | 保留 |  | 補齊 Sort/Filter/Paging 待辦功能或明確標註不支援 |
| SelectForm | 下拉選取視窗 | FormControlTool | 保留 |  | 先以人工 UI 測試 |
| TextForm | 文字輸入視窗 | FormControlTool | 保留 |  | 先以人工 UI 測試 |
| ModelEditor | 反射式模型編輯器（WinForms） | FormControlTool | 保留 |  | 將可純邏輯部分拆測（屬性映射、型別轉換） |
| ModelEditorForm | ModelEditor 容器表單 | FormControlTool | 保留 |  | 先以人工 UI 測試 |
| GetJsonSubPropertyList | 讀取 JSON 某 root 下的子屬性清單 | JsonTool | 保留 |  | 補上檔案不存在與 root 不存在案例 |
| GetSinglePropertyByListJson | 反序列化單一子屬性到 Model | JsonTool | 保留 |  | 補上型別不符案例 |
| SaveSinglePropertyToListJson | 寫入或更新單一子屬性到 JSON | JsonTool | 保留 |  | 補上併發寫入與格式保存策略說明 |
| DataTableConverter Read/Write | DataTable 與 JSON 互轉 | JsonTool | 保留 |  | namespace 建議調整為非 Test 命名空間 |
| NewProcess(兩個 overload) | 建立 Process 啟動設定 | ProcessTool | 保留 |  | 補上路徑不存在與參數編碼案例 |
| 連線與交易控制 | OpenSharedConnection/Begin/Commit/Rollback/Scope | SqlTool | 保留 |  | 補上可重入、衝突模式與清理策略測試 |
| ExecuteNonQuery/ExecuteScalar/ExecuteSQL | 通用 SQL 執行 API | SqlTool | 保留 |  | 維持現狀 |
| ExecuteSQL<T> 映射 | DataTable 轉 Model 清單 | SqlTool | 保留 |  | 目前轉型例外僅 Debug，建議提供可配置錯誤策略 |
| SingleInsert/SingleUpdate/Delete | 單筆 ORM 操作 | SqlTool | 保留 |  | 維持現狀 |
| BulkInsert/BulkUpdate | 批次寫入與更新 | SqlTool | 保留 |  | finally 清理例外處理需可觀測，不建議靜默吞掉 |
| Stored Procedure APIs | 執行 SP，回傳 DataTable/Model/影響筆數 | SqlTool | 保留 |  | 補上參數方向（Output/InputOutput）測試 |
| RelayCommand<T> | WPF 命令綁定 | WPFTool | 保留 |  | 可增補非泛型 RelayCommand 以提升使用彈性 |
| ViewModelBase | INotifyPropertyChanged 基礎類 | WPFTool | 保留 |  | 補上 PropertyChanged 單元測試 |
| PropertyItem | 屬性編輯模型與型別轉換 | WPFTool | 保留 |  | 轉型失敗目前以 Debug 記錄，建議加入可觀測錯誤回報 |
| ModelEditorViewModel | WPF 編輯器 ViewModel | WPFTool | 保留 |  | 補上命令觸發流程測試 |
| DynamicDataGrid | 動態欄位 DataGrid 控制項 | WPFTool | 保留 |  | UI 先人工測試，邏輯層補單元測試 |
| ModelEditor UserControl/Window | WPF 模型編輯 UI | WPFTool | 保留 |  | UI 先人工測試 |
| GetXDocumentFromBytes | 由 byte 載入 XML | XmlTool | 保留 |  | 補上空輸入/非法 XML 測試 |
| GetXDocument | 由路徑載入 XML | XmlTool | 保留 |  | 例外型別可改為更具體（FileNotFoundException） |
| SaveXML | 儲存 XML | XmlTool | 保留 |  | 補上覆寫與編碼場景測試 |
| GetFileNameInZip | 列出 zip 內檔案與資料夾名稱 | ZipTool | 保留 |  | 維持現狀 |
| ExtractSingleFileToMemory | 解出 zip 單一檔案內容 | ZipTool | 保留 |  | 補上多層路徑與找不到檔案案例 |
| CheckZipFile | 驗證 zip 合法性（私有） | ZipTool | 保留 |  | catch 內連續 throw 造成死碼，需修正為保留原始例外資訊 |

## 補充決策註記
- SingleFileSetting 依需求暫不分析、暫不產生文件。
- UI 工具目前先採人工測試，後續將純邏輯拆出單元測試。
- SqlTool 以可獨立執行測試為目標，測試期間可自建資料庫並於結束後刪除。