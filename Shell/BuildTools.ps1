<#
.SYNOPSIS
    批次將專案編譯並封裝為 NuGet 套件 (.nupkg)

.DESCRIPTION
    此函式會遍歷指定的專案清單，執行 Clean (清理)、Build (編譯) 以及 Pack (打包) 動作
    專案預設位於 $RootPath\Src\ 目錄下，打包後的檔案會統一輸出至 $RootPath\nupkgs\

.PARAMETER RootPath
    專案根目錄的絕對路徑

.PARAMETER PackTools
    包含要打包的專案名稱（資料夾名稱）陣列

.EXAMPLE
    $RootPath = Split-Path -Path $PSScriptRoot -Parent
    $PackTools = @(
        "FileTool",
        "JsonTool",
        "ProcessTool",
        "XMLTool",
        "ZipTool",
        "FormControlTool",
        "ApiTool",
        "SqlTool",
        "WPFTool"
    )

    PackageTools -RootPath $RootPath -PackTools $PackTools
#>
function PackTools{
    param (
        
        [Parameter(Mandatory = $true)] [string]$RootPath,
        [Parameter(Mandatory = $true)] [array]$PackTools
    )

    $OutputFolder = Join-Path $RootPath "nupkgs"

    foreach($Tool in $PackTools) {
        $ToolProjectPath = Join-Path $RootPath "Src"
        $ToolProjectPath = Join-Path $ToolProjectPath $Tool

        if(Test-Path $ToolProjectPath){
            Push-Location $ToolProjectPath

            dotnet clean -c Release

            dotnet build -c Release

            #Write-Host (Get-Location).Path
            dotnet pack -c Release -o $OutputFolder

            Pop-Location
        }
        else{
            Write-Host "Project path for $Tool does not exist: $ToolProjectPath"
        }
    }
}