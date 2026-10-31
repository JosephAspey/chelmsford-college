# Wipes the prototype's database and uploaded media, so the next run seeds a fresh CMS and fresh college
# tables. Stop the AppHost first. Only touches this project's container and volume.
$ErrorActionPreference = 'Stop'
$volume = 'chelmsford-prototype-sql'

Get-Process Prototype.Web, Prototype.AppHost -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$containers = docker ps -a --format '{{.Names}}' | Where-Object {
    (docker inspect $_ --format '{{range .Mounts}}{{.Name}} {{end}}') -match $volume
}
foreach ($c in $containers) { docker rm -f $c | Out-Null; "Removed container $c" }

if (docker volume ls --format '{{.Name}}' | Where-Object { $_ -eq $volume }) {
    docker volume rm $volume | Out-Null
    "Removed volume $volume"
}

$media = Join-Path $PSScriptRoot '..\src\Prototype.Web\wwwroot\media'
if (Test-Path $media) { Remove-Item -Recurse -Force $media; 'Removed uploaded media' }
'Done. Start the AppHost to seed a fresh copy.'
