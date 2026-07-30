param(
    [ValidateSet('Status', 'Stop')]
    [string]$Action = 'Status'
)

$ErrorActionPreference = 'Stop'
$solutionRoot = (Resolve-Path $PSScriptRoot).Path
$servicePorts = [ordered]@{
    ApiGateway = @(5028, 7023)
    ComicAPI   = @(5023, 7024)
    UserAPI    = @(5054, 7231)
    ChapterAPI = @(5115, 7114)
    BannerAPI  = @(5127, 7053)
    PaymentAPI = @(5128, 7071)
    SocialAPI  = @(5197, 7133)
    MissionAPI = @(5288, 7224)
    WalletAPI  = @(5091, 7066)
}

function Get-ProjectListeners {
    $rows = foreach ($service in $servicePorts.Keys) {
        foreach ($port in $servicePorts[$service]) {
            $connections = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue
            foreach ($connection in $connections) {
                $process = Get-CimInstance Win32_Process -Filter "ProcessId=$($connection.OwningProcess)" -ErrorAction SilentlyContinue
                [pscustomobject]@{
                    Service = $service
                    Port = $port
                    PID = $connection.OwningProcess
                    Process = $process.Name
                    IsThisSolution = [bool](
                        $process.ExecutablePath -like "$solutionRoot*" -or
                        $process.CommandLine -like "*$solutionRoot*"
                    )
                }
            }
        }
    }

    $rows | Sort-Object Port, PID -Unique
}

if ($Action -eq 'Status') {
    $listeners = @(Get-ProjectListeners)
    if ($listeners.Count -eq 0) {
        Write-Host 'Tat ca cong API dang trong.' -ForegroundColor Green
    } else {
        $listeners | Format-Table -AutoSize
    }
    exit 0
}

# Only stop executables/commands that belong to this solution. Processes from
# other applications are never terminated by this script.
$projectProcesses = @(
    Get-CimInstance Win32_Process | Where-Object {
        ($_.ExecutablePath -like "$solutionRoot*") -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -like "*$solutionRoot*")
    }
)

foreach ($process in ($projectProcesses | Sort-Object ProcessId -Descending)) {
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Milliseconds 500
$remaining = @(Get-ProjectListeners)
if ($remaining.Count -gt 0) {
    Write-Host 'Van con cong dang duoc su dung:' -ForegroundColor Yellow
    $remaining | Format-Table -AutoSize
    exit 1
}

Write-Host 'Da dung cac API cua solution; tat ca cong API da trong.' -ForegroundColor Green
