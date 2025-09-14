param(
  [ValidateSet('up','down','rebuild','logs','ps','migrate','seed','reset')]
  [string]$cmd = 'up'
)

function Compose([string]$a, [string]$b = $null, [string]$c = $null) {
  if ($null -ne $c) { & docker compose $a $b $c; return }
  if ($null -ne $b) { & docker compose $a $b;    return }
  & docker compose $a
}

switch ($cmd) {
  'up'       { Compose 'up' '-d' '--build' }
  'down'     { Compose 'down' '-v' }
  'rebuild'  { Compose 'build' '--no-cache'; Compose 'up' '-d' }
  'logs'     { Compose 'logs' '-f' }
  'ps'       { Compose 'ps' }
  'migrate'  { dotnet ef database update -p backend/AegisGuard.Infrastructure -s backend/AegisGuard.Api }
  'seed'     { Write-Host "Seeding passiert beim API-Start automatisch, wenn die DB leer ist." }
  'reset'    {
               Compose 'down' '-v'
               Write-Host "Stacks & Volumes entfernt. Starte neu mit: .\dev.ps1 up"
             }
}
