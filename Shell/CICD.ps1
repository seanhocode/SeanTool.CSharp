# CICD.ps1
# NugetPublish workflow 的邏輯集中處，每個 function 對應 yml 中的一個 step。
# 用法: pwsh -File Shell/CICD.ps1 -Command <Name> [-Param value ...]

param(
    [Parameter(Mandatory = $true)][string]$Command,
    [string]$Before,
    [string]$After,
    [string]$EnvFile,
    [string]$IncludeProjects,
    [string]$ChangedProjects,
    [string]$GitHubToken,
    [string]$NugetSource,
    [string]$OutputDir = "./nupkg"
)

$ErrorActionPreference = "Stop"

function Import-DotNetTool {
    <#
    .SYNOPSIS
        載入 SeanTool.Scripts 中的 DotNetTool 共用 function。

    .EXAMPLE
        Import-DotNetTool
    #>
    $dotNetToolPath = Join-Path $PSScriptRoot "SeanTool.Scripts\Shell\Windows\PowerShell\CSharp\DotNetTool.ps1"
    if (-not (Test-Path $dotNetToolPath)) {
        throw "DotNetTool.ps1 not found: $dotNetToolPath. Check out submodules before running PackProjects or PublishPackages."
    }

    return $dotNetToolPath
}

function Import-EnvFile {
    <#
    .SYNOPSIS
        將 workflow 環境設定檔匯入 GitHub Actions 環境。

    .PARAMETER EnvFile
        要匯入的環境設定檔路徑。

    .EXAMPLE
        Import-EnvFile -EnvFile ".github/workflows/public.env"
    #>
    param([Parameter(Mandatory = $true)][string]$EnvFile)

    Get-Content $EnvFile | Add-Content $env:GITHUB_ENV
}

function Get-ChangedFiles {
    <#
    .SYNOPSIS
        取得兩個 Git revision 之間變更的檔案。

    .PARAMETER Before
        變更前的 Git revision。

    .PARAMETER After
        變更後的 Git revision。

    .EXAMPLE
        Get-ChangedFiles -Before $env:BEFORE -After $env:AFTER
    #>
    param(
        [Parameter(Mandatory = $true)][string]$Before,
        [Parameter(Mandatory = $true)][string]$After
    )

    git diff --name-only $Before $After 2>$null | Set-Content diff.txt
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path diff.txt) -or (Get-Item diff.txt).Length -eq 0) {
        git diff --name-only HEAD~1 HEAD | Set-Content diff.txt
    }

    Write-Host "--- Changed files ---"
    Get-Content diff.txt | Write-Host
}

function Get-ChangedProjects {
    <#
    .SYNOPSIS
        根據變更檔案找出需要處理的工具專案。

    .PARAMETER IncludeProjects
        以分號分隔的專案名稱清單。

    .EXAMPLE
        Get-ChangedProjects -IncludeProjects "FileTool;JsonTool"
    #>
    param([Parameter(Mandatory = $true)][string]$IncludeProjects)

    $projects = $IncludeProjects -split ';'
    $diffContent = Get-Content diff.txt -Raw -ErrorAction SilentlyContinue
    $changed = @()

    foreach ($proj in $projects) {
        if ($diffContent -and $diffContent -match [regex]::Escape($proj)) {
            Write-Host "Detected project change: $proj"
            $changed += $proj
        }
    }

    $result = $changed -join ';'
    Add-Content $env:GITHUB_OUTPUT "changed_projects=$result"
    return $result
}

function Resolve-ProjectPaths {
    <#
    .SYNOPSIS
        將專案名稱解析為專案檔案完整路徑。

    .PARAMETER ChangedProjects
        以分號分隔的專案名稱清單。

    .EXAMPLE
        Resolve-ProjectPaths -ChangedProjects "FileTool;JsonTool"
    #>
    param([Parameter(Mandatory = $true)][string]$ChangedProjects)

    foreach ($proj in ($ChangedProjects -split ';' | Where-Object { $_ })) {
        Get-ChildItem -Recurse -Filter "$proj.csproj" |
            Select-Object -First 1 -ExpandProperty FullName
    }
}

function Invoke-PackProjects {
    <#
    .SYNOPSIS
        使用共用 DotNetTool function 清理、建置並封裝變更的專案。

    .PARAMETER ChangedProjects
        以分號分隔的專案名稱清單。

    .PARAMETER OutputDir
        NuGet 套件輸出資料夾。

    .EXAMPLE
        Invoke-PackProjects -ChangedProjects "FileTool;JsonTool" -OutputDir "./nupkg"
    #>
    param(
        [Parameter(Mandatory = $true)][string]$ChangedProjects,
        [Parameter(Mandatory = $true)][string]$OutputDir
    )

    . (Import-DotNetTool)

    Invoke-ProjectPack `
        -ProjectFullPaths @(Resolve-ProjectPaths -ChangedProjects $ChangedProjects) `
        -TargetFolder $OutputDir
}

function Publish-Packages {
    <#
    .SYNOPSIS
        使用共用 DotNetTool function 將 NuGet 套件發布至指定來源。

    .PARAMETER GitHubToken
        GitHub Packages 使用的 API token。

    .PARAMETER NugetSource
        NuGet 套件來源 URL。

    .PARAMETER OutputDir
        NuGet 套件所在資料夾。

    .EXAMPLE
        Publish-Packages -GitHubToken $env:GITHUB_TOKEN `
            -NugetSource $env:GITHUB_NUGET_SOURCE -OutputDir "./nupkg"
    #>
    param(
        [Parameter(Mandatory = $true)][string]$GitHubToken,
        [Parameter(Mandatory = $true)][string]$NugetSource,
        [Parameter(Mandatory = $true)][string]$OutputDir
    )

    . (Import-DotNetTool)

    Get-ChildItem -Path $OutputDir -Filter "*.nupkg" -ErrorAction SilentlyContinue |
        ForEach-Object {
            Publish-NuGetPackage `
                -PackagePath $_.FullName `
                -Source $NugetSource `
                -ApiKey $GitHubToken `
                -SkipDuplicate
        }
}

switch ($Command) {
    "ImportEnvFile"     { Import-EnvFile -EnvFile $EnvFile }
    "GetChangedFiles"   { Get-ChangedFiles -Before $Before -After $After }
    "GetChangedProjects" { Get-ChangedProjects -IncludeProjects $IncludeProjects }
    "PackProjects"      { Invoke-PackProjects -ChangedProjects $ChangedProjects -OutputDir $OutputDir }
    "PublishPackages"   {
        Publish-Packages `
            -GitHubToken $GitHubToken `
            -NugetSource $NugetSource `
            -OutputDir $OutputDir
    }
    default { throw "Unknown -Command '$Command'" }
}
