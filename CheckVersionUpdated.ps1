# Checks if the assembly version of the program is the same as the latest release version on GitHub based on release name
# Can run this in release mode to ensure you didn't forget to update the assembly version
param (
    [Parameter(Mandatory=$true)]
    [string]$AssemblyInfoPath,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubRepo
)
$AssemblyInfoPath = $AssemblyInfoPath.Trim('"')
$GitHubRepo = $GitHubRepo.Trim('"')

# Function to extract version from AssemblyInfo.cs
function Get-AssemblyVersion {
    param ([string]$FilePath)
    $content = Get-Content $FilePath
    foreach ($line in $content) {
        # Skip lines that start with // (comments)
        if ($line -match '^\s*//') {
            continue
        }
        if ($line -match '(?<=AssemblyVersion\(")(?<version>[\d\.]+)') {
            return $matches['version']
        }
    }
    throw "Unable to find AssemblyVersion in $FilePath"
}

# Function to get latest GitHub release version
function Get-LatestGitHubVersion {
    param ([string]$Repo)
    $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
    $response = Invoke-RestMethod -Uri $apiUrl -Method Get
    return $response.name
}

# Function to show message box
function Show-MessageBox {
    param (
        [string]$Message,
        [string]$Title = "Version Check"
    )
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show($Message, $Title, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
}

# Function to compare versions (ignoring the last digit)
function Compare-Versions {
    param (
        [string]$Version1,
        [string]$Version2
    )
    $v1Parts = $Version1.Split('.')
    $v2Parts = $Version2.Split('.')
    
    # Ensure we have at least 3 parts for each version
    while ($v1Parts.Length -lt 3) { $v1Parts += '0' }
    while ($v2Parts.Length -lt 3) { $v2Parts += '0' }
    
    # Compare only the first three parts
    for ($i = 0; $i -lt 3; $i++) {
        if ([int]$v1Parts[$i] -ne [int]$v2Parts[$i]) {
            return $false
        }
    }
    return $true
}

try {
    $assemblyVersion = Get-AssemblyVersion -FilePath $AssemblyInfoPath
    $latestGitHubVersion = Get-LatestGitHubVersion -Repo $GitHubRepo
    Write-Host "Assembly Version: $assemblyVersion"
    Write-Host "Latest GitHub Release: $latestGitHubVersion"
    
    if (Compare-Versions -Version1 $assemblyVersion -Version2 $latestGitHubVersion) {
        $message = "Current version ($assemblyVersion) matches the latest GitHub release ($latestGitHubVersion), ignoring the revision number. You may need to increment the version."
        Write-Warning $message
        Show-MessageBox -Message $message
    } else {
        Write-Host "Version check passed. Assembly and GitHub versions are different." -ForegroundColor Green
    }
} catch {
    $errorMessage = "An error occurred: $_"
    Write-Error $errorMessage
    Show-MessageBox -Message $errorMessage -Title "Error"
    exit 1
}