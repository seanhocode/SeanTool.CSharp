# WPFTool 使用說明

## 套件安裝與專案設定
### 安裝套件
- ToDo

### 套件依賴
-  `WPF-UI`（目前版本 4.3.0）
    - 套件內的控制項會自行載入 `WPFTool;component/Styles/Theme.xaml`

### XAML 命名空間
```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:wpftool="clr-namespace:SeanTool.CSharp.WPFTool;assembly=WPFTool">
```

### 共用使用注意事項
- `DataContext`、`ItemsSource`、`DataSource` 建議使用 ViewModel 屬性繫結，不要在每次畫面更新時重新建立來源集合。
- 控制項內部已載入 WPF-UI 深色主題資源；應用程式若需要一致的背景色，可在 `Window` 或根容器設定 `Background="{DynamicResource ApplicationBackgroundBrush}"`。
- `DataGrid`、下拉清單與樹狀控制項已啟用虛擬化，但大量資料仍應避免在 UI 執行緒同步做來源重建。

# 控制項說明
---
## DateTimePicker
- 將 WPF `DatePicker` 與時間文字輸入合併為一個日期時間控制項。日期與時間任一部分變更後，會回寫完整的 `DateTime?`。

### 範例
```xml
<wpftool:DateTimePicker
    SelectedDateTime="{Binding StartTime, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    IsReadOnly="{Binding IsReadOnly}" />
```

### 可用屬性
#### 控制項
| 屬性 | 型別 | 預設值 | 說明 |
|---|---|---:|---|
| `SelectedDateTime` | `DateTime?` | `null` | 完整日期時間；具雙向繫結預設行為。設為 `null` 時清空日期並將時間顯示為 `00:00:00`。 |
| `IsReadOnly` | `bool` | `false` | `true` 時停用日期選擇並將時間欄設為唯讀。 |

### 輸入格式
- 時間接受 `HH:mm` 或 `HH:mm:ss`，例如 `09:30`、`09:30:15`。
- 無效時間在失去焦點後恢復為 `00:00:00`。
- 日期接受 `yyyyMMdd`、`yyyy/M/d`、`yyyy-MM-dd`、`yyyy.MM.dd` 等格式，也會使用目前文化與不變文化嘗試解析。
- 公開靜態方法 `TryParseTime` 與 `TryParseDate` 可供外部共用解析。

---

## DropDownList
- 支援即時搜尋的單選/多選下拉清單。`ItemsSource` 可為任意 `IEnumerable`；若設定 `DisplayMemberPath`，控制項會使用指定屬性作為顯示文字，否則使用 `ToString()`。

### 範例
- 單選
    ```xml
    <wpftool:DropDownList
        ItemsSource="{Binding People}"
        DisplayMemberPath="Name"
        SelectedValue="{Binding SelectedPersonId, Mode=TwoWay}"
        PlaceholderText="請選擇人員" />
    ```

- 多選
    ```xml
    <wpftool:DropDownList
        ItemsSource="{Binding People}"
        DisplayMemberPath="Name"
        SelectionMode="Multiple"
        SelectedValues="{Binding SelectedPersonIds, Mode=TwoWay}"
        PlaceholderText="請選擇人員（可多選）" />
    ```

### 可用屬性
#### 控制項
| 屬性 | 型別 | 預設值 | 說明 |
|---|---|---:|---|
| `ItemsSource` | `IEnumerable` | `null` | 下拉清單來源；支援實作 `INotifyCollectionChanged` 的集合動態更新。 |
| `DisplayMemberPath` | `string` | `null` | 項目顯示屬性；未設定時使用項目的 `ToString()`。 |
| `SelectionMode` | `System.Windows.Controls.SelectionMode` | `Single` | `Single` 單選或 `Multiple`/`Extended` 多選。切回 `Single` 時只保留第一個已選項目。 |
| `SelectedValue` | `object` | `null` | 單選模式的原始值；雙向繫結。 |
| `SelectedValues` | `IEnumerable` | `null` | 多選模式的原始值集合；雙向繫結。 |
| `PlaceholderText` | `string` | `請選擇` | 沒有選取項目時顯示的文字。 |
| `IsDropDownOpen` | `bool` | `false` | 下拉面板是否開啟；雙向繫結。 |
| `IsMultiSelect` | `bool` | 依 `SelectionMode` | 唯讀 CLR 屬性，供程式判斷目前是否為多選。 |
| `ViewModel` | `DropDownListViewModel` | 自動建立 | 唯讀核心 ViewModel；需要以程式操作搜尋或選取時使用。 |

### 備註
- 多選模式顯示「已選擇 N 項」，完整選取內容放在 ToolTip。搜尋會在停止輸入後套用，來源集合更新時會依原始 `Value` 嘗試保留已選狀態。

---

## DynamicDataGrid
- 依資料來源自動建立欄位的 `DataGrid`。支援一般 CLR 物件、`DataTable`/`DataView`、欄位篩選、列編輯、勾選與操作按鈕。

### 範例
- 自動產生欄位範例
    ```xml
    <wpftool:DynamicDataGrid
        DataSource="{Binding People}"
        ShowCheckBox="True"
        SelectedItem="{Binding SelectedPerson, Mode=TwoWay}"
        ActionDefinitions="{Binding Actions}" />
    ```

- 手動定義欄位範例
     ```xml
    <wpftool:DynamicDataGrid DataSource="{Binding People}">
        <wpftool:DynamicDataGrid.ColumnDefinitions>
            <x:Array Type="{x:Type wpftool:DynamicDataGridColumnDefinition}">
                <wpftool:DynamicDataGridColumnDefinition
                    Header="姓名"
                    BindingPath="Name"
                    Width="150" />
                <wpftool:DynamicDataGridColumnDefinition
                    Header="出生日期"
                    BindingPath="BirthDate"
                    StringFormat="yyyy-MM-dd"
                    IsReadOnly="True" />
            </x:Array>
        </wpftool:DynamicDataGrid.ColumnDefinitions>
    </wpftool:DynamicDataGrid>
    ```
    - 在程式碼建立欄位通常更簡單：
        ```csharp
        var columns = new[]
        {
            new DynamicDataGridColumnDefinition
            {
                Header = "姓名",
                BindingPath = "Name",
                Width = new DataGridLength(150)
            }
        };
        ```

### 可用屬性
#### 控制項
| 屬性 | 型別 | 預設值 | 說明 |
|---|---|---:|---|
| `DataSource` | `object` | `null` | 資料來源。可直接指定 `IEnumerable`、`DataTable` 或 `DataView`。 |
| `ColumnDefinitions` | `IEnumerable<DynamicDataGridColumnDefinition>` | `null` | 手動欄位定義；未設定時依資料型別自動產生。 |
| `ActionDefinitions` | `IEnumerable<DynamicDataGridActionDefinition>` | `null` | 操作欄按鈕定義。 |
| `ShowCheckBox` | `bool` | `false` | 是否顯示勾選欄與表頭全選按鈕。 |
| `SelectedItem` | `object` | `null` | 目前單一選取項目；雙向繫結。 |
| `SelectedItems` | `IReadOnlyList<object>` | 空集合 | 目前勾選的項目；唯讀。 |

#### `DynamicDataGridColumnDefinition`
| 屬性 | 型別 | 說明 |
|---|---|---|
| `Header` | `string` | 欄位標題。 |
| `BindingPath` | `string` | 資料項目的屬性名稱，例如 `Name`、`BirthDate`。 |
| `Width` | `DataGridLength` | 欄寬，例如 `Auto`、固定寬度或星號寬度。 |
| `StringFormat` | `string` | 顯示格式，例如 `C0`、`yyyy-MM-dd`。 |
| `IsReadOnly` | `bool` | 是否禁止列內編輯。 |
| `FilterValueType` | `FilterValueType?` | 欄位篩選型別；未指定時依欄位型別推斷。 |

#### `DynamicDataGridActionDefinition`
| 屬性 | 型別 | 說明 |
|---|---|---|
| `Header` | `string` | 操作欄標題。 |
| `Content` | `string` | 按鈕文字，預設為 `執行`。 |
| `Width` | `DataGridLength` | 操作欄寬度。 |
| `Action` | `Action<object>` | 按鈕按下時執行，參數是該列資料項目。 |

---

## FilterControl
- 可獨立使用，也可作為 `DynamicDataGrid` 欄位標題或 `VirtualTreeView` 的內部篩選器。它的 `DataContext` 必須是 `FilterViewModel`。

### 範例
- 獨立使用
    ```xml
    <!-- xaml 範例 -->
    <wpftool:FilterControl
        DataContext="{Binding NameFilter}"
        AlwaysShowFilterOptions="True" />
    ```
    ```csharp
    // C# 範例
    NameFilter = new FilterViewModel(
        propertyName: "Name",
        header: "姓名",
        valueType: FilterValueType.Text);
    ```

### 可用屬性
#### 控制項
| 屬性 | 型別 | 預設值 | 說明 |
|---|---|---:|---|
| `AlwaysShowFilterOptions` | `bool` | `false` | `true` 時直接顯示篩選面板並隱藏漏斗按鈕。 |

#### `FilterViewModel`
| 屬性 | 說明 |
|---|---|
| `PropertyName` | 要篩選的屬性名稱。 |
| `Header` | UI 顯示標題。 |
| `FilterDefinition` | 篩選型別與自訂運算子定義。 |
| `Operator` | 暫存中的運算子。 |
| `Value` | 文字篩選值。 |
| `DateTimeValue` / `DateTimeValueTo` | 日期時間篩選起點/終點；`Between` 使用兩者。 |
| `AvailableOperators` | 目前型別可使用的運算子。 |
| `AppliedFilter` | 按下「查詢」後的實際篩選條件；沒有有效值時為 `null`。 |
| `ApplyCommand` | 套用暫存條件。 |
| `ClearCommand` | 清除已套用條件。 |

### 篩選型別與運算子
| `FilterValueType` | 預設可用運算子 |
|---|---|
| `Text` | `Contains`、`StartsWith`、`Equals`、`GreaterThan`、`LessThan`、`IsNull`、`IsNotNull` |
| `DateTime` | `IsNull`、`IsNotNull`、`LessThanOrEqual`、`GreaterThanOrEqual`、`Between` |
| `TreeNode` | `Contains`、`StartsWith`、`Equals` |

### 備註
- 可在 `FilterCondition.CustomizeOperators` 指定允許的子集合。篩選條件為多條件 AND，實際執行可透過 `FilterQuery.Apply`；樹狀資料則使用 `TreeFilterQuery.Apply`。
---

## ModelEditor
- 依反射自動產生物件編輯表單，支援 CLR 物件、`DataTable` 第一筆資料列、基本型別、列舉、日期時間、路徑、多行文字與巢狀物件。

### 範例
```xml
<wpftool:ModelEditor
    TargetObject="{Binding EditingPerson}"
    IsEditing="{Binding CanEdit}" />
```
- `TargetObject` 變更時會重新分析屬性。`DataTable`/`IListSource` 會自動取 `DefaultView` 的第一筆 `DataRowView` 作為編輯目標。

### 可用屬性
#### 控制項
| 成員 | 型別 | 說明 |
|---|---|---|
| `TargetObject` | `object` | 要編輯的物件；變更時重建欄位。 |
| `IsEditing` | `bool` | 編輯模式；`false` 時整體唯讀。 |
| `ViewModel` | `ModelEditorViewModel` | 唯讀公開檢視模型，供進階情境繫結。 |

### 事件
| 事件 | 型別 | 說明 |
|---|---|---|
| `Saved` | `EventHandler` | 儲存成功後觸發。 |
| `Canceled` | `EventHandler` | 按取消後觸發。 |

### 自訂屬性-編輯標記
```csharp
public class EditModel
{
    [DisplayName("備註")]
    [EditorTextArea(4)]
    public string Remark { get; set; } = string.Empty;

    [EditorPath(PathType.File, "圖片 (*.png)|*.png")]
    public string ImagePath { get; set; } = string.Empty;

    [EditorPath(PathType.Folder)]
    public string OutputFolder { get; set; } = string.Empty;
}
```
| 標記 | 說明 |
|---|---|
| `DisplayNameAttribute` | 指定欄位顯示名稱。 |
| `EditorTextAreaAttribute(int minLines = 4)` | 將字串欄位顯示為可換行文字區，`MinLines` 指定最小行數。 |
| `EditorPathAttribute(PathType.File, string filter)` | 將欄位標記為檔案或資料夾路徑，顯示瀏覽按鈕；檔案可指定篩選字串。 |
| `[ReadOnly(true)]` | 顯示唯讀欄位。 |

### 備註
- `enum` 屬性會自動產生下拉選項；`DateTime` 會分別編輯日期與時間；複雜物件會以子視窗編輯，確認後才寫回父物件。
- 編輯值先暫存在欄位項目中，按「儲存」且所有欄位驗證通過後才寫回原物件；按「取消」會捨棄暫存變更。唯讀屬性（沒有 setter 或標記 `[ReadOnly(true)]`）會顯示但不能修改。

---

## VirtualTreeView
- 提供可搜尋、可虛擬化的樹狀清單，支援單選與父子節點三態勾選。

### 範例
```csharp
RootNodes = new[]
{
    new TreeNodeViewModel(new TreeNode(
        "產品",
        new[]
        {
            new TreeNode("A", value: 1),
            new TreeNode("B", value: 2)
        }))
};
```

```xml
<wpftool:VirtualTreeView
    ItemsSource="{Binding RootNodes}"
    SelectedItem="{Binding SelectedNode, Mode=TwoWay}"
    IsCheckVisible="True" />
```

### 可用屬性
#### 控制項
| 屬性 | 型別 | 預設值 | 說明 |
|---|---|---:|---|
| `ItemsSource` | `IEnumerable<TreeNodeViewModel>` | `null` | 頂層樹節點來源。 |
| `IsCheckVisible` | `bool` | `true` | 是否顯示節點 CheckBox。 |
| `SelectedItem` | `TreeNodeViewModel` | `null` | TreeView 單選節點；雙向繫結。 |
| `FilteredItems` | `ObservableCollection<TreeNodeViewModel>` | 空集合 | 篩選後顯示的頂層節點，唯讀。 |
| `FilterViewModel` | `FilterViewModel` | 自動建立 | 內部名稱篩選器，唯讀。 |
| `CheckedItems` | `ObservableCollection<TreeNodeViewModel>` | 空集合 | 已勾選節點，唯讀。 |
| `CheckedValues` | `IReadOnlyList<object?>` | 空集合 | 已勾選節點的 `Value`，唯讀。 |

#### `TreeNode` 與 `TreeNodeViewModel`
| 成員 | 說明 |
|---|---|
| `TreeNode.Name` | 節點顯示文字。 |
| `TreeNode.Value` | 節點對應的任意業務值。 |
| `TreeNode.Children` | 子節點集合。 |
| `TreeNodeViewModel.CheckType` | 真實勾選狀態：`None`、`All`、`HasValue`。 |
| `TreeNodeViewModel.VisibleCheckType` | 篩選後僅依可見子節點計算的勾選狀態。 |
| `TreeNodeViewModel.IsExpanded` | 是否展開。 |
| `TreeNodeViewModel.AddChild` | 動態新增子節點。 |
| `TreeNodeViewModel.RemoveFromParent` | 從父節點移除目前節點。 |

### 備註
- 勾選父節點會套用至子孫；子節點部分勾選時父節點顯示 `HasValue`。篩選只建立顯示用視圖，不修改原始樹資料，清除篩選後會回到原本狀態。

---