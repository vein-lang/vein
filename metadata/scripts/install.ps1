param (
    [switch]$bypassRoot
)

$arch = if ([System.Environment]::Is64BitOperatingSystem) { "win-x64" } else { "win-arm64" }
if ($bypassRoot)
{
    if (([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        Write-Host "You cannot install vein-sdk from under the administrator." -ForegroundColor Red
        return
    } 
}

$env:RUNE_NOVID = "1";
$env:VEINC_NOVID = "1";

$apiUrl = "https://releases.vein-lang.org/api/get-release"

$outputDir = "$HOME\.vein"

function DownloadFile {
    param (
        [string]$url,
        [string]$outputFile
    )

    $fileStream = $null
    $stream = $null

    try {
        $request = [System.Net.HttpWebRequest]::Create($url)
        $request.Method = "GET"
        $response = $request.GetResponse()
        $totalBytes = $response.ContentLength
        $stream = $response.GetResponseStream()
        $fileStream = [System.IO.File]::Create($outputFile)
        $buffer = New-Object byte[] 8192
        $totalReadBytes = 0
        while ($true) {
            $readBytes = $stream.Read($buffer, 0, $buffer.Length)
            if ($readBytes -le 0) { break }
            $fileStream.Write($buffer, 0, $readBytes)
            $totalReadBytes += $readBytes
            $percentComplete = [math]::Round(($totalReadBytes / $totalBytes) * 100, 2)
            Write-Progress -Activity "Downloading $url" -Status "$percentComplete% Complete" -PercentComplete $percentComplete
        }
    } finally {
        if ($fileStream) {
            $fileStream.Close()
        }
        if ($stream) {
            $stream.Close()
        }
        Write-Progress -Activity "Downloading $url" -Status "Download Complete" -Completed
    }
}


try {
    $releaseInfo = Invoke-RestMethod -Uri $apiUrl
    $asset = $releaseInfo.assets | Where-Object { $_.name -eq "rune.$arch.zip" }
    $downloadUrl = $asset.browser_download_url
    $tagVersion = $releaseInfo.tag_name.Replace("v", "")
    Write-Host $tagVersion
    New-Item -ItemType Directory -Force -Path $outputDir > $null
    $zipFile = "$outputDir\rune.$arch.zip"
    DownloadFile $downloadUrl $zipFile
    Expand-Archive -Path $zipFile -DestinationPath $outputDir -Force > $null
    Remove-Item -Force $zipFile > $null
    $currentPath = [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User)
    if (-not $currentPath.Contains("$outputDir\bin")) {
        $env:Path += ";$outputDir\bin"
        [Environment]::SetEnvironmentVariable("Path", $env:Path, [EnvironmentVariableTarget]::User)
    }

    Invoke-Expression "$outputDir\rune.exe telemetry"

    if ($LASTEXITCODE -ne 0) {
        return;
    }

    $installWorkloads = Read-Host "Do you want to install vein.runtime and vein.compiler workloads? (y/n)"
    if ($installWorkloads -eq 'y') {
        Invoke-Expression "$outputDir\rune.exe workload install vein.runtime@$tagVersion"
        if ($LASTEXITCODE -ne 0) {
            return;
        }
        Invoke-Expression "$outputDir\rune.exe workload install vein.compiler@$tagVersion"
        if ($LASTEXITCODE -ne 0) {
            return;
        }
        Write-Output "Workloads installed."
    } else {
        Write-Output "Workloads installation skipped."
    }
    Write-Output "Rune Installed, restart your teminal for use"
}
catch {
    Write-Error "Failed to install Vein Rune package: $_"
}
# SIG # Begin signature block
# MIIoZQYJKoZIhvcNAQcCoIIoVjCCKFICAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCB2T8/Pmnot9IFJ
# SZLlgqZIyY2vx5u5M1vp9YDa0ine0KCCDdgwgga/MIIEp6ADAgECAhEAgU5CF6Ep
# f+1azNQX+JGtdTANBgkqhkiG9w0BAQsFADBTMQswCQYDVQQGEwJCRTEZMBcGA1UE
# ChMQR2xvYmFsU2lnbiBudi1zYTEpMCcGA1UEAxMgR2xvYmFsU2lnbiBDb2RlIFNp
# Z25pbmcgUm9vdCBSNDUwHhcNMjQwNjE5MDMyNTExWhcNMzgwNzI4MDAwMDAwWjBZ
# MQswCQYDVQQGEwJCRTEZMBcGA1UEChMQR2xvYmFsU2lnbiBudi1zYTEvMC0GA1UE
# AxMmR2xvYmFsU2lnbiBHQ0MgUjQ1IENvZGVTaWduaW5nIENBIDIwMjAwggIiMA0G
# CSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDWQk3540/GI/RsHYGmMPdIPc/Q5Y3l
# ICKWB0Q1XQbPDx1wYOYmVPpTI2ACqF8CAveOyW49qXgFvY71TxkkmXzPERabH3tr
# 0qN7aGV3q9ixLD/TcgYyXFusUGcsJU1WBjb8wWJMfX2GFpWaXVS6UNCwf6JEGenW
# bmw+E8KfEdRfNFtRaDFjCvhb0N66WV8xr4loOEA+COhTZ05jtiGO792NhUFVnhy8
# N9yVoMRxpx8bpUluCiBZfomjWBWXACVp397CalBlTlP7a6GfGB6KDl9UXr3gW8/y
# DATS3gihECb3svN6LsKOlsE/zqXa9FkojDdloTGWC46kdncVSYRmgiXnQwp3UrGZ
# UUL/obLdnNLcGNnBhqlAHUGXYoa8qP+ix2MXBv1mejaUASCJeB+Q9HupUk5qT1QG
# KoCvnsdQQvplCuMB9LFurA6o44EZqDjIngMohqR0p0eVfnJaKnsVahzEaeawvkAZ
# mcvSfVVOIpwQ4KFbw7MueovE3vFLH4woeTBFf2wTtj0s/y1KiirsKA8tytScmIpK
# bVo2LC/fusviQUoIdxiIrTVhlBLzpHLr7jaep1EnkTz3ohrM/Ifll+FRh2npIsyD
# wLcPRWwH4UNP1IxKzs9jsbWkEHr5DQwosGs0/iFoJ2/s+PomhFt1Qs2JJnlZnWur
# Y3FikCUNCCDx/wIDAQABo4IBhjCCAYIwDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQM
# MAoGCCsGAQUFBwMDMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYDVR0OBBYEFNqzjcAk
# kKNrd9MMoFndIWdkdgt4MB8GA1UdIwQYMBaAFB8Av0aACvx4ObeltEPZVlC7zpY7
# MIGTBggrBgEFBQcBAQSBhjCBgzA5BggrBgEFBQcwAYYtaHR0cDovL29jc3AuZ2xv
# YmFsc2lnbi5jb20vY29kZXNpZ25pbmdyb290cjQ1MEYGCCsGAQUFBzAChjpodHRw
# Oi8vc2VjdXJlLmdsb2JhbHNpZ24uY29tL2NhY2VydC9jb2Rlc2lnbmluZ3Jvb3Ry
# NDUuY3J0MEEGA1UdHwQ6MDgwNqA0oDKGMGh0dHA6Ly9jcmwuZ2xvYmFsc2lnbi5j
# b20vY29kZXNpZ25pbmdyb290cjQ1LmNybDAuBgNVHSAEJzAlMAgGBmeBDAEEATAL
# BgkrBgEEAaAyATIwDAYKKwYBBAGgMgoEAjANBgkqhkiG9w0BAQsFAAOCAgEAMhDk
# vBelgxBAndOp/SfPRXKpxR9LM1lvLDIxeXGE1jZn1at0/NTyBjputdbL8UKDlr19
# 3pUsGu1q40EcpsiJMcJZbIm8KiMDWVBHSf1vUw4qKMxIVO/zIxhbkjZOvKNj1MP7
# AA+A0SDCyuWWuvCaW6qkJXoZ2/rbe1NP+baj2WPVdV8BpSjbthgpFGV5nNu064iY
# FFNQYDEMZrNR427JKSZk8BTRc3jEhI0+FKWSWat5QUbqNM+BdkY6kXgZc77+BvXX
# wYQ5oHBMCjUAXtgqMCQfMne24Xzfs0ZB4fptjePjC58vQNmlOg1kyb6M0RrJZSA6
# 4gD6TnohN0FwmZ1QH5l7dZB0c01FpU5Yf912apBYiWaTZKP+VPdNquvlIO5114iy
# HQw8vKGSoFbkR/xnD+p4Kd+Po8fZ4zF4pwsplGscJ10hJ4fio+/IQJAuXBcoJdMB
# RBergNp8lKhbI/wgnpuRoZD/sw3lckQsRxXz1JFyJvnyBeMBZ/dptd4Ftv4okIx/
# oSk7tyzaZCJplsT001cNKoXGu2horIvxUktkbqq4t+xNFBz6qBQ4zuwl6+Ri3TX5
# uHsHXRtDZwIIaz2/JSODgZZzB+7+WFo8N9qg21/SnDpGkpzEJhwJMNol5A4dkHPU
# HodOaYSBkc1lfuc1+oOAatM0HUaneAimeDIlZnowggcRMIIE+aADAgECAgwjTXrV
# AkZg+Wr4NqkwDQYJKoZIhvcNAQELBQAwWTELMAkGA1UEBhMCQkUxGTAXBgNVBAoT
# EEdsb2JhbFNpZ24gbnYtc2ExLzAtBgNVBAMTJkdsb2JhbFNpZ24gR0NDIFI0NSBD
# b2RlU2lnbmluZyBDQSAyMDIwMB4XDTI0MTEwODEzMzA0MVoXDTI1MTEwOTEzMzA0
# MVowfzELMAkGA1UEBhMCUlUxGTAXBgNVBAgTEExlbmluZ3JhZCBPYmxhc3QxDzAN
# BgNVBAcTBk11cmlubzEhMB8GA1UEChMYSVAgVmVzcCBZdWtpIEFsZWtzZWV2aWNo
# MSEwHwYDVQQDExhJUCBWZXNwIFl1a2kgQWxla3NlZXZpY2gwggIiMA0GCSqGSIb3
# DQEBAQUAA4ICDwAwggIKAoICAQDIR56qBFueiBmSKEqKAfHlZZ2ZgrPfaBZBLvhb
# 7ewSxV+47urEyb1qJCRkcQRuPUXNeQr4WYyrBlIgvrRIBlWpDvqNof9j4TJzrCSl
# 8f+aetteezMu9c91Pzoj2oly3af4oJu8UIZEHcNn5OLFkCjkp3ThCHZz7baFqcin
# s1TV+34yIhnIX7OGJS7u2ZhIZdJjX1MmdWXiJrXNvQYC7p2wsBsNj+7/IA3e/fZK
# xBJsi113FJy3m98blM5n1mIgdI0qa/BfUwZER0zW+ZNAm7WZzCJCYFMAsVPbfa6y
# F4T3JIPsu3tp5RU1pWD5iIucK0aryVAhs1J1E1wmdLvWC9x4yOzz4HgZvFFecu2K
# GyFml8JtZ3e0upeUSPNLPzp1cBsjZ5Y3v81BjJ/1DwEFrssrBNNUcKMe/avc25UI
# jj605MBzrJ0Z6C5F6dX9QocdzBJpHOChHT+r+aSRzMpgCWYt/mj8qcS2mXOAVCto
# kC0G3LWcUdgJLMZj/ADWiaz6iyjOM3G2atUWRHjJeqEh2raehe49AsInp751Lmvh
# 7qR/3pXFlYs1XLXOwmRHnQiq3wtEo66Hes8WmxbQNQtRc6lhGD0+79HXIYW9xO9x
# PEQJMkyuN2c/MnAZ3z6v3D/TDvRIgimc3rFOedUHGjF+T7tXI6XGaz//wN6Ff4o/
# hyxahwIDAQABo4IBsTCCAa0wDgYDVR0PAQH/BAQDAgeAMIGbBggrBgEFBQcBAQSB
# jjCBizBKBggrBgEFBQcwAoY+aHR0cDovL3NlY3VyZS5nbG9iYWxzaWduLmNvbS9j
# YWNlcnQvZ3NnY2NyNDVjb2Rlc2lnbmNhMjAyMC5jcnQwPQYIKwYBBQUHMAGGMWh0
# dHA6Ly9vY3NwLmdsb2JhbHNpZ24uY29tL2dzZ2NjcjQ1Y29kZXNpZ25jYTIwMjAw
# VgYDVR0gBE8wTTBBBgkrBgEEAaAyATIwNDAyBggrBgEFBQcCARYmaHR0cHM6Ly93
# d3cuZ2xvYmFsc2lnbi5jb20vcmVwb3NpdG9yeS8wCAYGZ4EMAQQBMAkGA1UdEwQC
# MAAwRQYDVR0fBD4wPDA6oDigNoY0aHR0cDovL2NybC5nbG9iYWxzaWduLmNvbS9n
# c2djY3I0NWNvZGVzaWduY2EyMDIwLmNybDATBgNVHSUEDDAKBggrBgEFBQcDAzAf
# BgNVHSMEGDAWgBTas43AJJCja3fTDKBZ3SFnZHYLeDAdBgNVHQ4EFgQUTsSa2Rk3
# fGrkOKccCMKTmjd62jcwDQYJKoZIhvcNAQELBQADggIBAJrOw3DRBDE6s71Rsamy
# nV9KqOAKpK8oAWBLQ+LMJf1QtiPmB3s+bsX1InjL9Qwd105RDhyz1YO5EaavFlWL
# 1YkBZ5wyglSuhgCMmkXdET8xdGeFcGXLZpRdzVdjiKuRXHvEZgS7mXTpquJtVH5a
# sFwawQzUxnWQu66srLAodce3k+bgbKlyKbIx4fuviBXaji9fxHQ4ZaD2ZaV1wmqb
# R2yipFdjHDa5EkxfUlO2JwBKO/cjb75FvFaBf/KiTJKzj3vowBTMqUr18YlLeWow
# CFInPp2mzLnrRT9zhNPkQWxP8bKQAkMeMZhrFGaJM589rCOqbht0CcfcYuaFpuCQ
# TSAUHAS1pkXLDzhXBkPsAGKTW+N+k8KyOcdneFRUpubBDkN8IrqxnVK4Klzgc+gZ
# 3nUNZkX6lJhfu3DPrlMnHvYXc+Da8jf1Wa3K69paMo9ObWSsEWqm03Id5i3wm4Dc
# Oc/pNHOLFKFdE8Dkd0X1PKgDR2DEorR0P8nr1EMso3qNCiJwusE1gwJvGqHdnaz6
# YeL5MyPCky2ebjaj8bs4bKEoSXobdVPMQA0+8Ejg3ldY5zT6m3LvpBvZhcd7Yshj
# RhjdfazlnvX+5WctS4xLuiWaUsYW+tBSZb1eNL9fPYB7GIgTkVnfbWSveEuG+9lp
# pSrOOkBp5b27odZgj1d6Uf7vMYIZ4zCCGd8CAQEwaTBZMQswCQYDVQQGEwJCRTEZ
# MBcGA1UEChMQR2xvYmFsU2lnbiBudi1zYTEvMC0GA1UEAxMmR2xvYmFsU2lnbiBH
# Q0MgUjQ1IENvZGVTaWduaW5nIENBIDIwMjACDCNNetUCRmD5avg2qTANBglghkgB
# ZQMEAgEFAKB8MBAGCisGAQQBgjcCAQwxAjAAMBkGCSqGSIb3DQEJAzEMBgorBgEE
# AYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3DQEJ
# BDEiBCBXfQ90z5D18XVyM+Q8AY2KIxRj3d2UndaJpste0jCABTANBgkqhkiG9w0B
# AQEFAASCAgCNOKm7Unukb+NGI0cN1EjBSriKXJQNH43hoQEILMBW2S6UmPUS/Q1O
# WLsGF5ocfWDFmEhqcVy/3545YuPh3Rv99DI+0R3iHl/hcjCG/AbljIe93x2lMvaS
# HyYzF8+QmEUuIFH/wIb1xOo4+nav0F7GcrS13BE4M4dJrXRpGrGmEhniztBk5Y5E
# oBKkA9EG/8zeVdSd0Y1IY5ZrJ61ODjfhrDf2BnhcnZ9ngkWN83J50rB3R0grlLWU
# 5BKBKXwFGL9+o8xmn5N8ae61SEwAXfHgt6uRvqi7JrzZuNfcnIvXGHrEUYgxZiGT
# 3bXqzO84I9E6dheZpsYoED51CUZoOl58OtsYvj8IdNEoAb1+aGVx8Ie49L4gJcXn
# JLIwiUKUjceFY9gqU2lT7xN6kN2RkPOiQ2ZHrFstEICp29N95mLtYR7eAwSah/VV
# Wx9Qk9wPZ7h/46+Y7bRw171coywFuNH7zx+0sQ/EAfn7vxHVglz3o5MSoSe+cZPZ
# FNoJjq14+sm/E0NuzlHXTl3cKMrDjIdXXK511rmvvlX2SDYV87aNm3aDGsGcctj6
# 5PxrpZu7ypqp/zclYbcKjr1akG+bKWmzKv7P1rdt+AtAiURPGA1IzJz6y2bXGvnW
# WVxYCUthZKKjbbBV5O1wlWWpT6yKuv2y7sAOCftXgskpTYOjLk2AyKGCFs0wghbJ
# BgorBgEEAYI3AwMBMYIWuTCCFrUGCSqGSIb3DQEHAqCCFqYwghaiAgEDMQ0wCwYJ
# YIZIAWUDBAIBMIHoBgsqhkiG9w0BCRABBKCB2ASB1TCB0gIBAQYLKwYBBAGgMgID
# AQIwMTANBglghkgBZQMEAgEFAAQgyErK7soUXPJq9qQC/NisEvAeHKzZTB5gJdsD
# bHvCkPsCFDgblmVJnygFEMoyEwI2614b0fkTGA8yMDI1MDExNDA2MzcxNFowAwIB
# AaBhpF8wXTELMAkGA1UEBhMCQkUxGTAXBgNVBAoMEEdsb2JhbFNpZ24gbnYtc2Ex
# MzAxBgNVBAMMKkdsb2JhbHNpZ24gVFNBIGZvciBDb2RlU2lnbjEgLSBSNiAtIDIw
# MjMxMaCCElQwggZsMIIEVKADAgECAhABm+reyE1rj/dsOp8uASQWMA0GCSqGSIb3
# DQEBCwUAMFsxCzAJBgNVBAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNh
# MTEwLwYDVQQDEyhHbG9iYWxTaWduIFRpbWVzdGFtcGluZyBDQSAtIFNIQTM4NCAt
# IEc0MB4XDTIzMTEwNzE3MTM0MFoXDTM0MTIwOTE3MTM0MFowXTELMAkGA1UEBhMC
# QkUxGTAXBgNVBAoMEEdsb2JhbFNpZ24gbnYtc2ExMzAxBgNVBAMMKkdsb2JhbHNp
# Z24gVFNBIGZvciBDb2RlU2lnbjEgLSBSNiAtIDIwMjMxMTCCAaIwDQYJKoZIhvcN
# AQEBBQADggGPADCCAYoCggGBAOqEN1BoPJWFtUhUcZfzhHLJnYDTCNxZu7/LTZBp
# R4nlLNjxqGp+YDdJc5u4mLMU4O+Mk3AgtfUr12YFdT96hFCpUg/g1udv1Bw1LuAv
# KSSjjnclJ+C4831kdQyaQuXGneLYh3OL76CNl34WoMSyRs9gxs8PgVCA3U/p5Eai
# NKc+GMdrtLb7vtqpVn5/nF02PWM0IUvI0qMTGj4vUWh1+X/8cIQRZTMSs0ZlKISg
# M8CSne24H4lj0B57LFuwBPS9cmPOsDEhAQJqcrIiLO/rKjsQ1fGa9CaiLPxTAQR5
# I2lR012+c4TLm4OIbSDSIM6Bq2oiS3mQQuaCQq8D69TQ2oN6wy1I8c1FkbcRQd0X
# 70D8EqKywFmqVJdObcN63YaG1Ds3RzjoAzwxv0wze0Ps8ND/ZaafmD3SxrpZImwQ
# WBHFBMzoopiwHTPQ85Ud+O1xtAtB1WR5orxgLsN6yd5wNxIWPgKPXTgRsASJZ4ul
# LSDbuNb1nPUPvIi/JyzD+SCiwQIDAQABo4IBqDCCAaQwDgYDVR0PAQH/BAQDAgeA
# MBYGA1UdJQEB/wQMMAoGCCsGAQUFBwMIMB0GA1UdDgQWBBT5Tqu+uPhb/8LHA/RB
# 7pz41nR9PzBWBgNVHSAETzBNMAgGBmeBDAEEAjBBBgkrBgEEAaAyAR4wNDAyBggr
# BgEFBQcCARYmaHR0cHM6Ly93d3cuZ2xvYmFsc2lnbi5jb20vcmVwb3NpdG9yeS8w
# DAYDVR0TAQH/BAIwADCBkAYIKwYBBQUHAQEEgYMwgYAwOQYIKwYBBQUHMAGGLWh0
# dHA6Ly9vY3NwLmdsb2JhbHNpZ24uY29tL2NhL2dzdHNhY2FzaGEzODRnNDBDBggr
# BgEFBQcwAoY3aHR0cDovL3NlY3VyZS5nbG9iYWxzaWduLmNvbS9jYWNlcnQvZ3N0
# c2FjYXNoYTM4NGc0LmNydDAfBgNVHSMEGDAWgBTqFsZp5+PLV0U5M6TwQL7Qw71l
# ljBBBgNVHR8EOjA4MDagNKAyhjBodHRwOi8vY3JsLmdsb2JhbHNpZ24uY29tL2Nh
# L2dzdHNhY2FzaGEzODRnNC5jcmwwDQYJKoZIhvcNAQELBQADggIBAJX0Z8+TmkOS
# gxd21iBVvIn/5F+y5RUat5cRQC4AQb7FPySgG0cHMwRMtLRi/8bu0wzCNKCUXDeY
# 60T4X/gnCgK+HtEkHSPLLxyrJ3qzqcUvDOTlkPAVJB6jFRn474PoT7toniNvfT0N
# cXBhMnxGbvKP0ZzoQ036g+H/xOA+/t5X3wZr82oGgWirDHwq949C/8BzadscpxZP
# JhlYc+2UXuQaohCCBzI7yp6/3Tl11LyLVD9+UJU0n5I5JFMYg1DUWy9mtHv+Wynr
# HsUF/aM9+6Gw8yt5D7FLrMOj2aPcLJwrI5b2eiq7rcVXtoS2Y7NgmBHsxtZmbyKD
# HIpYA/SP7JxO0N/uzmEh07WVVEk7IVE9oSOFksJb8nqUhJgKjyRWIooE+rSaiUg1
# +G/rgYYRU8CTezq01DTMYtY1YY6mUPuIdB7XMTUhHhG/q6NkU45U4nNmpPtmY+E3
# ycRr+yszixHDdJCBg8hPhsrdSpfbfpBQJaFh7IabNlIHyz5iVewzpuW4GvrdJC4M
# +TKJMWo1lf720f8Xiq4jCSshrmLu9+4357DJsxXtdpq3/ef+4WjeRMEKdOGVyFf7
# FOseWt+WdcVlGff01Y0hr2O26/TiF0aft9cHbmqdK/7p0nFO0r5PYtNJ1mBfQON2
# mSBE2Epcs10a2eKqv01ZABeeYGc6RxKgMIIGWTCCBEGgAwIBAgINAewckkDe/S5A
# XXxHdDANBgkqhkiG9w0BAQwFADBMMSAwHgYDVQQLExdHbG9iYWxTaWduIFJvb3Qg
# Q0EgLSBSNjETMBEGA1UEChMKR2xvYmFsU2lnbjETMBEGA1UEAxMKR2xvYmFsU2ln
# bjAeFw0xODA2MjAwMDAwMDBaFw0zNDEyMTAwMDAwMDBaMFsxCzAJBgNVBAYTAkJF
# MRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMTEwLwYDVQQDEyhHbG9iYWxTaWdu
# IFRpbWVzdGFtcGluZyBDQSAtIFNIQTM4NCAtIEc0MIICIjANBgkqhkiG9w0BAQEF
# AAOCAg8AMIICCgKCAgEA8ALiMCP64BvhmnSzr3WDX6lHUsdhOmN8OSN5bXT8MeR0
# EhmW+s4nYluuB4on7lejxDXtszTHrMMM64BmbdEoSsEsu7lw8nKujPeZWl12rr9E
# qHxBJI6PusVP/zZBq6ct/XhOQ4j+kxkX2e4xz7yKO25qxIjw7pf23PMYoEuZHA6H
# pybhiMmg5ZninvScTD9dW+y279Jlz0ULVD2xVFMHi5luuFSZiqgxkjvyen38Dljf
# gWrhsGweZYIq1CHHlP5CljvxC7F/f0aYDoc9emXr0VapLr37WD21hfpTmU1bdO1y
# S6INgjcZDNCr6lrB7w/Vmbk/9E818ZwP0zcTUtklNO2W7/hn6gi+j0l6/5Cx1Pcp
# Fdf5DV3Wh0MedMRwKLSAe70qm7uE4Q6sbw25tfZtVv6KHQk+JA5nJsf8sg2glLCy
# lMx75mf+pliy1NhBEsFV/W6RxbuxTAhLntRCBm8bGNU26mSuzv31BebiZtAOBSGs
# sREGIxnk+wU0ROoIrp1JZxGLguWtWoanZv0zAwHemSX5cW7pnF0CTGA8zwKPAf1y
# 7pLxpxLeQhJN7Kkm5XcCrA5XDAnRYZ4miPzIsk3bZPBFn7rBP1Sj2HYClWxqjcoi
# XPYMBOMp+kuwHNM3dITZHWarNHOPHn18XpbWPRmwl+qMUJFtr1eGfhA3HWsaFN8C
# AwEAAaOCASkwggElMA4GA1UdDwEB/wQEAwIBhjASBgNVHRMBAf8ECDAGAQH/AgEA
# MB0GA1UdDgQWBBTqFsZp5+PLV0U5M6TwQL7Qw71lljAfBgNVHSMEGDAWgBSubAWj
# kxPioufi1xzWx/B/yGdToDA+BggrBgEFBQcBAQQyMDAwLgYIKwYBBQUHMAGGImh0
# dHA6Ly9vY3NwMi5nbG9iYWxzaWduLmNvbS9yb290cjYwNgYDVR0fBC8wLTAroCmg
# J4YlaHR0cDovL2NybC5nbG9iYWxzaWduLmNvbS9yb290LXI2LmNybDBHBgNVHSAE
# QDA+MDwGBFUdIAAwNDAyBggrBgEFBQcCARYmaHR0cHM6Ly93d3cuZ2xvYmFsc2ln
# bi5jb20vcmVwb3NpdG9yeS8wDQYJKoZIhvcNAQEMBQADggIBAH/iiNlXZytCX4Gn
# CQu6xLsoGFbWTL/bGwdwxvsLCa0AOmAzHznGFmsZQEklCB7km/fWpA2PHpbyhqIX
# 3kG/T+G8q83uwCOMxoX+SxUk+RhE7B/CpKzQss/swlZlHb1/9t6CyLefYdO1RkiY
# lwJnehaVSttixtCzAsw0SEVV3ezpSp9eFO1yEHF2cNIPlvPqN1eUkRiv3I2ZOBlY
# wqmhfqJuFSbqtPl/KufnSGRpL9KaoXL29yRLdFp9coY1swJXH4uc/LusTN763lNM
# g/0SsbZJVU91naxvSsguarnKiMMSME6yCHOfXqHWmc7pfUuWLMwWaxjN5Fk3hgks
# 4kXWss1ugnWl2o0et1sviC49ffHykTAFnM57fKDFrK9RBvARxx0wxVFWYOh8lT0i
# 49UKJFMnl4D6SIknLHniPOWbHuOqhIKJPsBK9SH+YhDtHTD89szqSCd8i3VCf2vL
# 86VrlR8EWDQKie2CUOTRe6jJ5r5IqitV2Y23JSAOG1Gg1GOqg+pscmFKyfpDxMZX
# xZ22PLCLsLkcMe+97xTYFEBsIB3CLegLxo1tjLZx7VIh/j72n585Gq6s0i96ILH0
# rKod4i0UnfqWah3GPMrz2Ry/U02kR1l8lcRDQfkl4iwQfoH5DZSnffK1CfXYYHJA
# UJUg1ENEvvqglecgWbZ4xqRqqiKbMIIFgzCCA2ugAwIBAgIORea7A4Mzw4VlSOb/
# RVEwDQYJKoZIhvcNAQEMBQAwTDEgMB4GA1UECxMXR2xvYmFsU2lnbiBSb290IENB
# IC0gUjYxEzARBgNVBAoTCkdsb2JhbFNpZ24xEzARBgNVBAMTCkdsb2JhbFNpZ24w
# HhcNMTQxMjEwMDAwMDAwWhcNMzQxMjEwMDAwMDAwWjBMMSAwHgYDVQQLExdHbG9i
# YWxTaWduIFJvb3QgQ0EgLSBSNjETMBEGA1UEChMKR2xvYmFsU2lnbjETMBEGA1UE
# AxMKR2xvYmFsU2lnbjCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAJUH
# 6HPKZvnsFMp7PPcNCPG0RQssgrRIxutbPK6DuEGSMxSkb3/pKszGsIhrxbaJ0cay
# /xTOURQh7ErdG1rG1ofuTToVBu1kZguSgMpE3nOUTvOniX9PeGMIyBJQbUJmL025
# eShNUhqKGoC3GYEOfsSKvGRMIRxDaNc9PIrFsmbVkJq3MQbFvuJtMgamHvm566qj
# uL++gmNQ0PAYid/kD3n16qIfKtJwLnvnvJO7bVPiSHyMEAc4/2ayd2F+4OqMPKq0
# pPbzlUoSB239jLKJz9CgYXfIWHSw1CM69106yqLbnQneXUQtkPGBzVeS+n68UARj
# NN9rkxi+azayOeSsJDa38O+2HBNXk7besvjihbdzorg1qkXy4J02oW9UivFyVm4u
# iMVRQkQVlO6jxTiWm05OWgtH8wY2SXcwvHE35absIQh1/OZhFj931dmRl4QKbNQC
# TXTAFO39OfuD8l4UoQSwC+n+7o/hbguyCLNhZglqsQY6ZZZZwPA1/cnaKI0aEYdw
# gQqomnUdnjqGBQCe24DWJfncBZ4nWUx2OVvq+aWh2IMP0f/fMBH5hc8zSPXKbWQU
# LHpYT9NLCEnFlWQaYw55PfWzjMpYrZxCRXluDocZXFSxZba/jJvcE+kNb7gu3Gdu
# yYsRtYQUigAZcIN5kZeR1BonvzceMgfYFGM8KEyvAgMBAAGjYzBhMA4GA1UdDwEB
# /wQEAwIBBjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBSubAWjkxPioufi1xzW
# x/B/yGdToDAfBgNVHSMEGDAWgBSubAWjkxPioufi1xzWx/B/yGdToDANBgkqhkiG
# 9w0BAQwFAAOCAgEAgyXt6NH9lVLNnsAEoJFp5lzQhN7craJP6Ed41mWYqVuoPId8
# AorRbrcWc+ZfwFSY1XS+wc3iEZGtIxg93eFyRJa0lV7Ae46ZeBZDE1ZXs6KzO7V3
# 3EByrKPrmzU+sQghoefEQzd5Mr6155wsTLxDKZmOMNOsIeDjHfrYBzN2VAAiKrlN
# IC5waNrlU/yDXNOd8v9EDERm8tLjvUYAGm0CuiVdjaExUd1URhxN25mW7xocBFym
# Fe944Hn+Xds+qkxV/ZoVqW/hpvvfcDDpw+5CRu3CkwWJ+n1jez/QcYF8AOiYrg54
# NMMl+68KnyBr3TsTjxKM4kEaSHpzoHdpx7Zcf4LIHv5YGygrqGytXm3ABdJ7t+uA
# /iU3/gKbaKxCXcPu9czc8FB10jZpnOZ7BN9uBmm23goJSFmH63sUYHpkqmlD75HH
# TOwY3WzvUy2MmeFe8nI+z1TIvWfspA9MRf/TuTAjB0yPEL+GltmZWrSZVxykzLsV
# iVO6LAUP5MSeGbEYNNVMnbrt9x+vJJUEeKgDu+6B5dpffItKoZB0JaezPkvILFa9
# x8jvOOJckvB595yEunQtYQEgfn7R8k8HWV+LLUNS60YMlOH1Zkd5d9VUWx+tJDfL
# RVpOoERIyNiwmcUVhAn21klJwGW45hpxbqCo8YLoRT5s1gLXCmeDBVrJpBAxggNJ
# MIIDRQIBATBvMFsxCzAJBgNVBAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52
# LXNhMTEwLwYDVQQDEyhHbG9iYWxTaWduIFRpbWVzdGFtcGluZyBDQSAtIFNIQTM4
# NCAtIEc0AhABm+reyE1rj/dsOp8uASQWMAsGCWCGSAFlAwQCAaCCAS0wGgYJKoZI
# hvcNAQkDMQ0GCyqGSIb3DQEJEAEEMCsGCSqGSIb3DQEJNDEeMBwwCwYJYIZIAWUD
# BAIBoQ0GCSqGSIb3DQEBCwUAMC8GCSqGSIb3DQEJBDEiBCDOo+CFqNZ9aZzAo5qr
# 23i+EEtWy37JlXlngJgisesfFDCBsAYLKoZIhvcNAQkQAi8xgaAwgZ0wgZowgZcE
# IDqIepUbXrkqXuFPbLt2gjelRdAQW/BFEb3iX4KpFtHoMHMwX6RdMFsxCzAJBgNV
# BAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMTEwLwYDVQQDEyhHbG9i
# YWxTaWduIFRpbWVzdGFtcGluZyBDQSAtIFNIQTM4NCAtIEc0AhABm+reyE1rj/ds
# Op8uASQWMA0GCSqGSIb3DQEBCwUABIIBgJ3LktJyJo6cfWgcHdowmqjDzcQjdqAO
# DWVhtdIOoYhYZ4Xqy5Tx9InOFotg40rTlqxxPr3ftyccJ9tENPbiEnqGnGIzX5OS
# 9Zjy+TX5kTHSg9Eh9UCqxCZnQiax109t08rD/BeMFNd4Gdr9lqp3gAWwoMFQiGDW
# YEbOwIA9O2PBsDEumbxJMRnm8rCtWeR0MQG+saw/ZBFspHD7o/sqyaRLOFjT/Fap
# Af3tdU+HbQXQrobdnuwMGZnlNSNerrdK1AJs0RL2EluddthWZKo3Ynotqj446Em5
# LPInGiq+6LzLGuo6Vhshc4eYlU+ZUy3hCCPnHF3834vsJegM4HTNqK24WkdI4VwF
# MRrRfkH5A+/w7Yzr5DCo7Ns2XBPoIsmNg/p7AzD9YZ0qbLBIYFjSt16wmAhhKm4x
# hhT/7UxA/RObRbHb4/wB5EvKyjJYGlh+clcDiY8aVQ+nVAAYvBmrhLPtby0/Xmt6
# 8IzMrHvzNPZomVzHbFyuUM8u9qQ4F9gepA==
# SIG # End signature block
