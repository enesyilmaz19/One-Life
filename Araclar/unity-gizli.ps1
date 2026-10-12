# Unity'yi GİZLİ pencerede çalıştırır — Enes'in odağını çalmasın (2026-10-12: "sürekli alttab atıyorsun").
# Kullanım: powershell -NoProfile -ExecutionPolicy Bypass -File unity.ps1 -Method <executeMethod> -Log <log> [-Otopilot]
param([string]$Method, [string]$Log, [switch]$Otopilot)
$U = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$P = 'C:\Users\Enesin pc\UnityProjects\Tek Can'
if ($Otopilot) {
    # Otopilot kendi çıkar: -quit yok; Play modu için -nographics yok.
    $a = "-batchmode -projectPath `"$P`" -executeMethod $Method -logFile `"$Log`""
} else {
    $a = "-batchmode -quit -nographics -projectPath `"$P`" -executeMethod $Method -logFile `"$Log`""
}
$p = Start-Process -FilePath $U -ArgumentList $a -WindowStyle Hidden -Wait -PassThru
exit $p.ExitCode
