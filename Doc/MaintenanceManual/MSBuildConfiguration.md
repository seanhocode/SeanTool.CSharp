# MSBuild 設定檔說明
## .props
- 適合放共用預設值
- 在專案檔（.csproj）的最上方載入
- .csproj 裡面的設定可以覆蓋 .props

## .targets
- 適合用來設定最終建置階段的共用行為與強制規定
- 在專案檔（.csproj）的最下方載入
- 可以讀取 .csproj 裡面設定好的值、強制覆 .csproj 裡的設定

## 載入順序
- MSBuild 會依下列方式載入本專案的設定：
    1. `Directory.Build.props`：SDK-style 專案自動載入，通常早於 `.csproj`
    2. `.csproj` 內的設定與明確 `<Import>`
    3. `Directory.Build.targets`：SDK-style 專案自動載入，通常晚於 `.csproj`
    4. `Directory.Packages.props`：NuGet Central Package Management 自動讀取套件版本

## 備註
- 後載入的設定通常可以覆蓋先前設定，但仍應避免在多處重複定義同一屬性

---

## Directory.Build.props
### 設定檔功能
- 集中管理並自動套用共通的編譯設定到資料夾下的所有專案
### 使用時機
- 需要對所有專案套用共通編譯設定時，將設定放在此檔案

---

## Directory.Build.targets
### 設定檔功能
- 定義強制套用的全域屬性
    - 因此階段可以覆蓋 .csproj 裡的設定
- 根據專案屬性執行條件邏輯
    - 因此階段可以讀到 .csproj 裡面設定好的值
- 定義全域的建置步驟
### 使用時機
- 要增加所有專案都需要的建置行為
- 執行 `dotnet build` 時可用這些訊息確認實際使用的 SDK 與 Framework

---

## Directory.Packages.props
### 設定檔功能
- 統一管理專案中 NuGet 套件的版本
    - 專案中不再需要在 `<PackageReference>` 指定版本
### 使用時機
- 多個專案使用同一個 NuGet 套件時，將設定放在此檔案統一管理版本
### 備註
- 專案還是需要於 `.csproj` 新增不含版本的 `<PackageReference>`

## SharedFrameworks.props
### 設定檔功能
- 集中決定各專案支援的 Target Framework
### 備註
- 專案設定檔需要匯入 `SharedFrameworks.props` 以使用預設的 Target Framework 設定
    ```xml
    <Import Project="$([MSBuild]::GetPathOfFileAbove('SharedFrameworks.props'))" />
    <TargetFrameworks>$(DefaultTfms)</TargetFrameworks>
    ```
- Windows Forms 或 WPF 專案改用 `$(WindowsTfms)`，並在專案中設定相應的 `UseWindowsForms` 或 `UseWPF`
    - `UseWindowsForms` 與 `UseWPF` 為 Microsoft.NET.Sdk.WindowsDesktop 定義的標準 MSBuild 屬性

---

## Packaging.props
### 設定檔功能
- 定義此專案 NuGet 套件的共用 metadata
### 使用時機
- 需要對所有要發布的工具專案統一管理 NuGet 套件的共用 metadata 時，將設定放在此檔案
### 備註
- 需要發布的工具專案在專案設定檔需 `.csproj` 匯入
    ```xml
    <Import Project="$([MSBuild]::GetPathOfFileAbove('Packaging.props'))" />
    ```
- 測試專案不應匯入此檔案；`Testing.props` 會將測試專案的 `GeneratePackageOnBuild` 設為 `false`

---

## Testing.props
### 設定檔功能
- 定義測試專案的共用設定
    - 專案名稱以 `.Test` 結尾時，自動套用此規則
### 備註
- 測試專案在 `.csproj` 匯入：
    ```xml
    <Import Project="$([MSBuild]::GetPathOfFileAbove('Testing.props'))" />
    ```

---