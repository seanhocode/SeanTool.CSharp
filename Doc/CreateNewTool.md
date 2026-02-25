# 建立類別庫專案
1. 於``Src``資料夾中建立C#``類別庫``專案
    - ![](./image/CreateNewTool/CreateNewToolProjectLocate.png)
    - ![](./image/CreateNewTool/CreateNewToolProjectSetting.png)
2. 於Tests資料夾中建立``xUnit測試專案``
    - 測試專案命名XXXTool.Test
    - ![](./image/CreateNewTool/CreateNewTestProjectLocate.png)
    - ![](./image/CreateNewTool/CreateNewTestProjectSetting.png)
3. 於該Tool專案的.csproj新增以下metadata設定
    - 
    ```csproj=
        <PropertyGroup>
            <TargetFrameworks>$(DefaultTfms)</TargetFrameworks>
            <PackageId>SeanTool.CSharp.SqlTool</PackageId>
            <Version>0.0.3</Version>
            <Description>SqlTool</Description>
            <PackageTags>tool;sql;net8;net10</PackageTags>
        </PropertyGroup>
    ```
    - ![](./image/CreateNewTool/csprojSetting.png)
4. 設定自動部屬
    - 將新Tool名稱加入``.github\workflows\config.env``的``INCLUDE_PROJECTS``
    - ![](./image/CreateNewTool/envSetting.png)
5. 命名空間設定為``SeanTool.CSharp``
    - ![](./image/CreateNewTool/NameSpaceSetting.png)