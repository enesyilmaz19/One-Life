# Unity'yi GİZLİ pencerede çalıştırır — Enes'in odağını çalmasın (2026-10-12: "sürekli alttab atıyorsun").
# Kullanım: powershell -NoProfile -ExecutionPolicy Bypass -File unity-gizli.ps1 -Method <executeMethod> -Log <log> [-Otopilot]
param([string]$Method, [string]$Log, [switch]$Otopilot)
$U = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$P = 'C:\Users\Enesin pc\UnityProjects\Tek Can'
if ($Otopilot) {
    # Otopilot kendi çıkar: -quit yok; Play modu için -nographics yok.
    $a = "-batchmode -projectPath `"$P`" -executeMethod $Method -logFile `"$Log`""
} else {
    $a = "-batchmode -quit -nographics -projectPath `"$P`" -executeMethod $Method -logFile `"$Log`""
}
# ⚠️ `Start-Process -Wait` YOK: o, Unity'nin arkada açık bıraktığı alt süreçleri (dotnet derleme
# sunucusu) de bekliyor — Unity kapandığı halde betik takılı kalıyordu (2026-10-13).
$p = Start-Process -FilePath $U -ArgumentList $a -WindowStyle Hidden -PassThru
$p.WaitForExit()
exit $p.ExitCode
