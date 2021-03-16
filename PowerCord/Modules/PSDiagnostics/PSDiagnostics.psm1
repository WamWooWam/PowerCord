# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
  PowerShell Diagnostics Module
  This module contains a set of wrapper scripts that
  enable a user to use ETW tracing in Windows
  PowerShell.
 #>

$script:Logman="$env:windir\system32\logman.exe"
$script:wsmanlogfile = "$env:windir\system32\wsmtraces.log"
$script:wsmprovfile = "$env:windir\system32\wsmtraceproviders.txt"
$script:wsmsession = "wsmlog"
$script:pssession = "PSTrace"
$script:psprovidername="Microsoft-Windows-PowerShell"
$script:wsmprovidername = "Microsoft-Windows-WinRM"
$script:oplog = "/Operational"
$script:analyticlog="/Analytic"
$script:debuglog="/Debug"
$script:wevtutil="$env:windir\system32\wevtutil.exe"
$script:slparam = "sl"
$script:glparam = "gl"

function Start-Trace
{
    Param(
    [Parameter(Mandatory=$true,
               Position=0)]
    [string]
    $SessionName,
    [Parameter(Position=1)]
    [ValidateNotNullOrEmpty()]
    [string]
    $OutputFilePath,
    [Parameter(Position=2)]
    [ValidateNotNullOrEmpty()]
    [string]
    $ProviderFilePath,
    [Parameter()]
    [Switch]
    $ETS,
    [Parameter()]
    [ValidateSet("bin", "bincirc", "csv", "tsv", "sql")]
    $Format,
    [Parameter()]
    [int]
    $MinBuffers=0,
    [Parameter()]
    [int]
    $MaxBuffers=256,
    [Parameter()]
    [int]
    $BufferSizeInKB = 0,
    [Parameter()]
    [int]
    $MaxLogFileSizeInMB=0
    )

    Process
    {
        $executestring = " start $SessionName"

        if ($ETS)
        {
            $executestring += " -ets"
        }

        if ($null -ne $OutputFilePath)
        {
            $executestring += " -o ""$OutputFilePath"""
        }

        if ($null -ne $ProviderFilePath)
        {
            $executestring += " -pf ""$ProviderFilePath"""
        }

        if ($null -ne $Format)
        {
            $executestring += " -f $Format"
        }

        if ($MinBuffers -ne 0 -or $MaxBuffers -ne 256)
        {
            $executestring += " -nb $MinBuffers $MaxBuffers"
        }

        if ($BufferSizeInKB -ne 0)
        {
            $executestring += " -bs $BufferSizeInKB"
        }

        if ($MaxLogFileSizeInMB -ne 0)
        {
            $executestring += " -max $MaxLogFileSizeInMB"
        }

        & $script:Logman $executestring.Split(" ")
    }
}

function Stop-Trace
{
    param(
    [Parameter(Mandatory=$true,
               Position=0)]
    $SessionName,
    [Parameter()]
    [switch]
    $ETS
    )

    Process
    {
        if ($ETS)
        {
            & $script:Logman update $SessionName -ets
            & $script:Logman stop $SessionName -ets
        }
        else
        {
            & $script:Logman update $SessionName
            & $script:Logman stop $SessionName
        }
    }
}

function Enable-WSManTrace
{

    # winrm
    "{04c6e16d-b99f-4a3a-9b3e-b8325bbc781e} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii

    # winrsmgr
    "{c0a36be8-a515-4cfa-b2b6-2676366efff7} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    # WinrsExe
    "{f1cab2c0-8beb-4fa2-90e1-8f17e0acdd5d} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    # WinrsCmd
    "{03992646-3dfe-4477-80e3-85936ace7abb} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    # IPMIPrv
    "{651d672b-e11f-41b7-add3-c2f6a4023672} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    #IpmiDrv
    "{D5C6A3E9-FA9C-434e-9653-165B4FC869E4} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    # WSManProvHost
    "{6e1b64d7-d3be-4651-90fb-3583af89d7f1} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    # Event Forwarding
    "{6FCDF39A-EF67-483D-A661-76D715C6B008} 0xffffffff 0xff" | Out-File $script:wsmprovfile -Encoding ascii -Append

    Start-Trace -SessionName $script:wsmsession -ETS -OutputFilePath $script:wsmanlogfile -Format bincirc -MinBuffers 16 -MaxBuffers 256 -BufferSizeInKB 64 -MaxLogFileSizeInMB 256 -ProviderFilePath $script:wsmprovfile
}

function Disable-WSManTrace
{
    Stop-Trace $script:wsmsession -ETS
}

function Enable-PSWSManCombinedTrace
{
    param (
        [switch] $DoNotOverwriteExistingTrace
    )

    $provfile = [io.path]::GetTempFilename()

    $traceFileName = [string][Guid]::NewGuid()
    if ($DoNotOverwriteExistingTrace) {
        $fileName = [string][guid]::newguid()
        $logfile = $PSHOME + "\\Traces\\PSTrace_$fileName.etl"
    } else {
        $logfile = $PSHOME + "\\Traces\\PSTrace.etl"
    }

    "Microsoft-Windows-PowerShell 0 5" | Out-File $provfile -Encoding ascii
    "Microsoft-Windows-WinRM 0 5" | Out-File $provfile -Encoding ascii -Append

    if (!(Test-Path $PSHOME\Traces))
    {
        New-Item -ItemType Directory -Force $PSHOME\Traces | Out-Null
    }

    if (Test-Path $logfile)
    {
        Remove-Item -Force $logfile | Out-Null
    }

    Start-Trace -SessionName $script:pssession -OutputFilePath $logfile -ProviderFilePath $provfile -ETS

    Remove-Item $provfile -Force -ea 0
}

function Disable-PSWSManCombinedTrace
{
    Stop-Trace -SessionName $script:pssession -ETS
}

function Set-LogProperties
{
    param(
        [Parameter(Mandatory=$true, Position=0, ValueFromPipeline=$true)]
        [Microsoft.PowerShell.Diagnostics.LogDetails]
        $LogDetails,
        [switch] $Force
     )

    Process
    {
        if ($LogDetails.AutoBackup -and !$LogDetails.Retention)
        {
            throw (New-Object System.InvalidOperationException)
        }

        $enabled = $LogDetails.Enabled.ToString()
        $retention = $LogDetails.Retention.ToString()
        $autobackup = $LogDetails.AutoBackup.ToString()
        $maxLogSize = $LogDetails.MaxLogSize.ToString()
        $osVersion = [Version] (Get-CimInstance Win32_OperatingSystem).Version

        if (($LogDetails.Type -eq "Analytic") -or ($LogDetails.Type -eq "Debug"))
        {
            if ($LogDetails.Enabled)
            {
                if($osVersion -lt 6.3.7600)
                {
                    & $script:wevtutil $script:slparam $LogDetails.Name -e:$Enabled
                }
                else
                {
                    & $script:wevtutil /q:$Force $script:slparam $LogDetails.Name -e:$Enabled
                }
            }
            else
            {
                if($osVersion -lt 6.3.7600)
                {
                    & $script:wevtutil $script:slparam $LogDetails.Name -e:$Enabled -rt:$Retention -ms:$MaxLogSize
                }
                else
                {
                    & $script:wevtutil /q:$Force $script:slparam $LogDetails.Name -e:$Enabled -rt:$Retention -ms:$MaxLogSize
                }
            }
        }
        else
        {
            if($osVersion -lt 6.3.7600)
            {
                & $script:wevtutil $script:slparam $LogDetails.Name -e:$Enabled -rt:$Retention -ab:$AutoBackup -ms:$MaxLogSize
            }
            else
            {
                & $script:wevtutil /q:$Force $script:slparam $LogDetails.Name -e:$Enabled -rt:$Retention -ab:$AutoBackup -ms:$MaxLogSize
            }
        }
    }
}

function ConvertTo-Bool([string]$value)
{
    if ($value -ieq "true")
    {
        return $true
    }
    else
    {
        return $false
    }
}

function Get-LogProperties
{
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, Position=0)] $Name
    )

    Process
    {
        $details = & $script:wevtutil $script:glparam $Name
        $indexes = @(1,2,8,9,10)
        $value = @()
        foreach($index in $indexes)
        {
            $value += @(($details[$index].SubString($details[$index].IndexOf(":")+1)).Trim())
        }

        $enabled = ConvertTo-Bool $value[0]
        $retention = ConvertTo-Bool $value[2]
        $autobackup = ConvertTo-Bool $value[3]

        New-Object Microsoft.PowerShell.Diagnostics.LogDetails $Name, $enabled, $value[1], $retention, $autobackup, $value[4]
    }
}

function Enable-PSTrace
{
    param(
        [switch] $Force,
		[switch] $AnalyticOnly
     )

    $Properties = Get-LogProperties ($script:psprovidername + $script:analyticlog)

	if (!$Properties.Enabled) {
		$Properties.Enabled = $true
		if ($Force) {
			Set-LogProperties $Properties -Force
		} else {
			Set-LogProperties $Properties
		}
	}

	if (!$AnalyticOnly) {
		$Properties = Get-LogProperties ($script:psprovidername + $script:debuglog)
		if (!$Properties.Enabled) {
			$Properties.Enabled = $true
			if ($Force) {
				Set-LogProperties $Properties -Force
			} else {
				Set-LogProperties $Properties
			}
		}
	}
}

function Disable-PSTrace
{
    param(
		[switch] $AnalyticOnly
     )
    $Properties = Get-LogProperties ($script:psprovidername + $script:analyticlog)
	if ($Properties.Enabled) {
		$Properties.Enabled = $false
		Set-LogProperties $Properties
	}

	if (!$AnalyticOnly) {
		$Properties = Get-LogProperties ($script:psprovidername + $script:debuglog)
		if ($Properties.Enabled) {
			$Properties.Enabled = $false
			Set-LogProperties $Properties
		}
	}
}
Add-Type @"
using System;

namespace Microsoft.PowerShell.Diagnostics
{
    public class LogDetails
    {
        public string Name
        {
            get
            {
                return name;
            }
        }
        private string name;

        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }
        private bool enabled;

        public string Type
        {
            get
            {
                return type;
            }
        }
        private string type;

        public bool Retention
        {
            get
            {
                return retention;
            }
            set
            {
                retention = value;
            }
        }
        private bool retention;

        public bool AutoBackup
        {
            get
            {
                return autoBackup;
            }
            set
            {
                autoBackup = value;
            }
        }
        private bool autoBackup;

        public int MaxLogSize
        {
            get
            {
                return maxLogSize;
            }
            set
            {
                maxLogSize = value;
            }
        }
        private int maxLogSize;

        public LogDetails(string name, bool enabled, string type, bool retention, bool autoBackup, int maxLogSize)
        {
            this.name = name;
            this.enabled = enabled;
            this.type = type;
            this.retention = retention;
            this.autoBackup = autoBackup;
            this.maxLogSize = maxLogSize;
        }
    }
}
"@

if (Get-Command logman.exe -Type Application -ErrorAction SilentlyContinue)
{
    Export-ModuleMember Disable-PSTrace, Disable-PSWSManCombinedTrace, Disable-WSManTrace, Enable-PSTrace, Enable-PSWSManCombinedTrace, Enable-WSManTrace, Get-LogProperties, Set-LogProperties, Start-Trace, Stop-Trace
}
else
{
    # Currently we only support these cmdlets as logman.exe is not available on systems like Nano and IoT
    Export-ModuleMember Disable-PSTrace, Enable-PSTrace, Get-LogProperties, Set-LogProperties
}

# SIG # Begin signature block
# MIIjnwYJKoZIhvcNAQcCoIIjkDCCI4wCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCBzuFp8c8UN6ilP
# /hSmUPDtAUbo8uWTs6wUbaR/ZpM1B6CCDYEwggX/MIID56ADAgECAhMzAAAB32vw
# LpKnSrTQAAAAAAHfMA0GCSqGSIb3DQEBCwUAMH4xCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01pY3Jvc29mdCBDb2RlIFNpZ25p
# bmcgUENBIDIwMTEwHhcNMjAxMjE1MjEzMTQ1WhcNMjExMjAyMjEzMTQ1WjB0MQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMR4wHAYDVQQDExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
# AQC2uxlZEACjqfHkuFyoCwfL25ofI9DZWKt4wEj3JBQ48GPt1UsDv834CcoUUPMn
# s/6CtPoaQ4Thy/kbOOg/zJAnrJeiMQqRe2Lsdb/NSI2gXXX9lad1/yPUDOXo4GNw
# PjXq1JZi+HZV91bUr6ZjzePj1g+bepsqd/HC1XScj0fT3aAxLRykJSzExEBmU9eS
# yuOwUuq+CriudQtWGMdJU650v/KmzfM46Y6lo/MCnnpvz3zEL7PMdUdwqj/nYhGG
# 3UVILxX7tAdMbz7LN+6WOIpT1A41rwaoOVnv+8Ua94HwhjZmu1S73yeV7RZZNxoh
# EegJi9YYssXa7UZUUkCCA+KnAgMBAAGjggF+MIIBejAfBgNVHSUEGDAWBgorBgEE
# AYI3TAgBBggrBgEFBQcDAzAdBgNVHQ4EFgQUOPbML8IdkNGtCfMmVPtvI6VZ8+Mw
# UAYDVR0RBEkwR6RFMEMxKTAnBgNVBAsTIE1pY3Jvc29mdCBPcGVyYXRpb25zIFB1
# ZXJ0byBSaWNvMRYwFAYDVQQFEw0yMzAwMTIrNDYzMDA5MB8GA1UdIwQYMBaAFEhu
# ZOVQBdOCqhc3NyK1bajKdQKVMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly93d3cu
# bWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY0NvZFNpZ1BDQTIwMTFfMjAxMS0w
# Ny0wOC5jcmwwYQYIKwYBBQUHAQEEVTBTMFEGCCsGAQUFBzAChkVodHRwOi8vd3d3
# Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRzL01pY0NvZFNpZ1BDQTIwMTFfMjAx
# MS0wNy0wOC5jcnQwDAYDVR0TAQH/BAIwADANBgkqhkiG9w0BAQsFAAOCAgEAnnqH
# tDyYUFaVAkvAK0eqq6nhoL95SZQu3RnpZ7tdQ89QR3++7A+4hrr7V4xxmkB5BObS
# 0YK+MALE02atjwWgPdpYQ68WdLGroJZHkbZdgERG+7tETFl3aKF4KpoSaGOskZXp
# TPnCaMo2PXoAMVMGpsQEQswimZq3IQ3nRQfBlJ0PoMMcN/+Pks8ZTL1BoPYsJpok
# t6cql59q6CypZYIwgyJ892HpttybHKg1ZtQLUlSXccRMlugPgEcNZJagPEgPYni4
# b11snjRAgf0dyQ0zI9aLXqTxWUU5pCIFiPT0b2wsxzRqCtyGqpkGM8P9GazO8eao
# mVItCYBcJSByBx/pS0cSYwBBHAZxJODUqxSXoSGDvmTfqUJXntnWkL4okok1FiCD
# Z4jpyXOQunb6egIXvkgQ7jb2uO26Ow0m8RwleDvhOMrnHsupiOPbozKroSa6paFt
# VSh89abUSooR8QdZciemmoFhcWkEwFg4spzvYNP4nIs193261WyTaRMZoceGun7G
# CT2Rl653uUj+F+g94c63AhzSq4khdL4HlFIP2ePv29smfUnHtGq6yYFDLnT0q/Y+
# Di3jwloF8EWkkHRtSuXlFUbTmwr/lDDgbpZiKhLS7CBTDj32I0L5i532+uHczw82
# oZDmYmYmIUSMbZOgS65h797rj5JJ6OkeEUJoAVwwggd6MIIFYqADAgECAgphDpDS
# AAAAAAADMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJVUzETMBEGA1UECBMK
# V2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0
# IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9vdCBDZXJ0aWZpY2F0
# ZSBBdXRob3JpdHkgMjAxMTAeFw0xMTA3MDgyMDU5MDlaFw0yNjA3MDgyMTA5MDla
# MH4xCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMT
# H01pY3Jvc29mdCBDb2RlIFNpZ25pbmcgUENBIDIwMTEwggIiMA0GCSqGSIb3DQEB
# AQUAA4ICDwAwggIKAoICAQCr8PpyEBwurdhuqoIQTTS68rZYIZ9CGypr6VpQqrgG
# OBoESbp/wwwe3TdrxhLYC/A4wpkGsMg51QEUMULTiQ15ZId+lGAkbK+eSZzpaF7S
# 35tTsgosw6/ZqSuuegmv15ZZymAaBelmdugyUiYSL+erCFDPs0S3XdjELgN1q2jz
# y23zOlyhFvRGuuA4ZKxuZDV4pqBjDy3TQJP4494HDdVceaVJKecNvqATd76UPe/7
# 4ytaEB9NViiienLgEjq3SV7Y7e1DkYPZe7J7hhvZPrGMXeiJT4Qa8qEvWeSQOy2u
# M1jFtz7+MtOzAz2xsq+SOH7SnYAs9U5WkSE1JcM5bmR/U7qcD60ZI4TL9LoDho33
# X/DQUr+MlIe8wCF0JV8YKLbMJyg4JZg5SjbPfLGSrhwjp6lm7GEfauEoSZ1fiOIl
# XdMhSz5SxLVXPyQD8NF6Wy/VI+NwXQ9RRnez+ADhvKwCgl/bwBWzvRvUVUvnOaEP
# 6SNJvBi4RHxF5MHDcnrgcuck379GmcXvwhxX24ON7E1JMKerjt/sW5+v/N2wZuLB
# l4F77dbtS+dJKacTKKanfWeA5opieF+yL4TXV5xcv3coKPHtbcMojyyPQDdPweGF
# RInECUzF1KVDL3SV9274eCBYLBNdYJWaPk8zhNqwiBfenk70lrC8RqBsmNLg1oiM
# CwIDAQABo4IB7TCCAekwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFEhuZOVQ
# BdOCqhc3NyK1bajKdQKVMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1Ud
# DwQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFHItOgIxkEO5FAVO
# 4eqnxzHRI4k0MFoGA1UdHwRTMFEwT6BNoEuGSWh0dHA6Ly9jcmwubWljcm9zb2Z0
# LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcmwwXgYIKwYBBQUHAQEEUjBQME4GCCsGAQUFBzAChkJodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcnQwgZ8GA1UdIASBlzCBlDCBkQYJKwYBBAGCNy4DMIGDMD8GCCsGAQUFBwIB
# FjNodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2RvY3MvcHJpbWFyeWNw
# cy5odG0wQAYIKwYBBQUHAgIwNB4yIB0ATABlAGcAYQBsAF8AcABvAGwAaQBjAHkA
# XwBzAHQAYQB0AGUAbQBlAG4AdAAuIB0wDQYJKoZIhvcNAQELBQADggIBAGfyhqWY
# 4FR5Gi7T2HRnIpsLlhHhY5KZQpZ90nkMkMFlXy4sPvjDctFtg/6+P+gKyju/R6mj
# 82nbY78iNaWXXWWEkH2LRlBV2AySfNIaSxzzPEKLUtCw/WvjPgcuKZvmPRul1LUd
# d5Q54ulkyUQ9eHoj8xN9ppB0g430yyYCRirCihC7pKkFDJvtaPpoLpWgKj8qa1hJ
# Yx8JaW5amJbkg/TAj/NGK978O9C9Ne9uJa7lryft0N3zDq+ZKJeYTQ49C/IIidYf
# wzIY4vDFLc5bnrRJOQrGCsLGra7lstnbFYhRRVg4MnEnGn+x9Cf43iw6IGmYslmJ
# aG5vp7d0w0AFBqYBKig+gj8TTWYLwLNN9eGPfxxvFX1Fp3blQCplo8NdUmKGwx1j
# NpeG39rz+PIWoZon4c2ll9DuXWNB41sHnIc+BncG0QaxdR8UvmFhtfDcxhsEvt9B
# xw4o7t5lL+yX9qFcltgA1qFGvVnzl6UJS0gQmYAf0AApxbGbpT9Fdx41xtKiop96
# eiL6SJUfq/tHI4D1nvi/a7dLl+LrdXga7Oo3mXkYS//WsyNodeav+vyL6wuA6mk7
# r/ww7QRMjt/fdW1jkT3RnVZOT7+AVyKheBEyIXrvQQqxP/uozKRdwaGIm1dxVk5I
# RcBCyZt2WwqASGv9eZ/BvW1taslScxMNelDNMYIVdDCCFXACAQEwgZUwfjELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEoMCYGA1UEAxMfTWljcm9z
# b2Z0IENvZGUgU2lnbmluZyBQQ0EgMjAxMQITMwAAAd9r8C6Sp0q00AAAAAAB3zAN
# BglghkgBZQMEAgEFAKCBrjAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgor
# BgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQgSB3jjVY/
# WMZ8WHFGDewy2JQLkkMsgxe7OQ9hGxl1POAwQgYKKwYBBAGCNwIBDDE0MDKgFIAS
# AE0AaQBjAHIAbwBzAG8AZgB0oRqAGGh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbTAN
# BgkqhkiG9w0BAQEFAASCAQCWkuu4JwVVNM7OgS/oMMxdXcLFx0QQxmyjvdyVWbJT
# P6a9tO56gH0zgVedmqx+8ym+ox/waXw46UdDsQiwf3CtASxNis9quTkZnOCZWQIL
# 8OfQLdtCIQzhYy04mFouu5MfbD/3RIZVZ4zm4IJDDhxxEFbNearaoaHhD1MEg9Yd
# UviXseFVXA297Lc2MwUMNRyqkJS9TEahekiEM969NC/jGr+3pNHEJWg/R4P/Q1Qp
# nvvXo1WzX8G3YS5FeIHt0jKi1mPWRDAmFNXHl3LmdwJDXAFsezdpBfXZZIt1wGsq
# 9lJl7ETDylUAOmtgGOhPR0BNQpAcHeXL65vxyxJZ9Kg0oYIS/jCCEvoGCisGAQQB
# gjcDAwExghLqMIIS5gYJKoZIhvcNAQcCoIIS1zCCEtMCAQMxDzANBglghkgBZQME
# AgEFADCCAVkGCyqGSIb3DQEJEAEEoIIBSASCAUQwggFAAgEBBgorBgEEAYRZCgMB
# MDEwDQYJYIZIAWUDBAIBBQAEIEDJpkzas5H9Nfnb9biOeOoxAGK+g9ZKwU9uiKTf
# +hEQAgZgPOAKujcYEzIwMjEwMzEwMDExOTA5LjU1OFowBIACAfSggdikgdUwgdIx
# CzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRt
# b25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xLTArBgNVBAsTJE1p
# Y3Jvc29mdCBJcmVsYW5kIE9wZXJhdGlvbnMgTGltaXRlZDEmMCQGA1UECxMdVGhh
# bGVzIFRTUyBFU046RTA0MS00QkVFLUZBN0UxJTAjBgNVBAMTHE1pY3Jvc29mdCBU
# aW1lLVN0YW1wIFNlcnZpY2Wggg5NMIIE+TCCA+GgAwIBAgITMwAAATdBj0PnWltv
# pwAAAAABNzANBgkqhkiG9w0BAQsFADB8MQswCQYDVQQGEwJVUzETMBEGA1UECBMK
# V2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0
# IENvcnBvcmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0Eg
# MjAxMDAeFw0yMDEwMTUxNzI4MTRaFw0yMjAxMTIxNzI4MTRaMIHSMQswCQYDVQQG
# EwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwG
# A1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMS0wKwYDVQQLEyRNaWNyb3NvZnQg
# SXJlbGFuZCBPcGVyYXRpb25zIExpbWl0ZWQxJjAkBgNVBAsTHVRoYWxlcyBUU1Mg
# RVNOOkUwNDEtNEJFRS1GQTdFMSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFt
# cCBTZXJ2aWNlMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxBHuadEl
# m3G5tikhTzjSDB0+9sXmUhUyDVRj0Y4vz9rZ9sykNobL5/6At5zOkeB2bl9IXvVd
# yS/ZJNZT373knzrQ347z30Mmw7++VU/CE+4x4w9kb5bqQHfSzbJQt6KmWsuMmJLz
# g4R5MeJs5MY5YdPLxoMoDRcTi//KoMFR0KzS1/324D2/4KkHD1Xt+s0xY0DICUOK
# 1RbmJCKEgBP1/GDZjuZQBS9Di89yTnvLJV+Lr1QtriH4EqmRoAdmV3zJ0GJsr5vh
# GPmKfOPCRSk7Q8igX7goFnCLzpYcfHGCqoR/mw95gfQpwymVwxZB0PkGMrQw+LKV
# Pa/FHP4C4KO+QQIDAQABo4IBGzCCARcwHQYDVR0OBBYEFA1gsHMM+udgY7rEne66
# OyzxlE9lMB8GA1UdIwQYMBaAFNVjOlyKMZDzQ3t8RhvFM2hahW1VMFYGA1UdHwRP
# ME0wS6BJoEeGRWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1
# Y3RzL01pY1RpbVN0YVBDQV8yMDEwLTA3LTAxLmNybDBaBggrBgEFBQcBAQROMEww
# SgYIKwYBBQUHMAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMv
# TWljVGltU3RhUENBXzIwMTAtMDctMDEuY3J0MAwGA1UdEwEB/wQCMAAwEwYDVR0l
# BAwwCgYIKwYBBQUHAwgwDQYJKoZIhvcNAQELBQADggEBAJ32U9d90RVuAUb9NsnX
# BG1K42qjhU+jHvwBdbipIcX4Wg7dH5ZduQZj3gWgKADZ5z+TehX7GnBbi265VI7x
# DRsFe2CjkTm4JIoisdKwYBDruS+YRRBG4B1ERuWi54XGwx+lSA+iQNrIi6Jm0CL/
# MfQLvwsqPJSGP69OEHCyaExos486+X3JTuGV11CBl/BO7r8UHbx/rE6fZrlZZYab
# IF6aeahvTL14LvZLV/bMzYSODsbjHHsTm9QaGm1ijhagCdbkAqr8+7HAgYEar8XP
# lzxUhVI4ShVB5ZGd9gZ2yBkwxdA0oFc745TdOPrbP79vd0ePqgvJDH5tkOhTRNI5
# 5XQwggZxMIIEWaADAgECAgphCYEqAAAAAAACMA0GCSqGSIb3DQEBCwUAMIGIMQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNy
# b3NvZnQgUm9vdCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAxMDAeFw0xMDA3MDEy
# MTM2NTVaFw0yNTA3MDEyMTQ2NTVaMHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpX
# YXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQg
# Q29ycG9yYXRpb24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAy
# MDEwMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqR0NvHcRijog7PwT
# l/X6f2mUa3RUENWlCgCChfvtfGhLLF/Fw+Vhwna3PmYrW/AVUycEMR9BGxqVHc4J
# E458YTBZsTBED/FgiIRUQwzXTbg4CLNC3ZOs1nMwVyaCo0UN0Or1R4HNvyRgMlhg
# RvJYR4YyhB50YWeRX4FUsc+TTJLBxKZd0WETbijGGvmGgLvfYfxGwScdJGcSchoh
# iq9LZIlQYrFd/XcfPfBXday9ikJNQFHRD5wGPmd/9WbAA5ZEfu/QS/1u5ZrKsajy
# eioKMfDaTgaRtogINeh4HLDpmc085y9Euqf03GS9pAHBIAmTeM38vMDJRF1eFpwB
# BU8iTQIDAQABo4IB5jCCAeIwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFNVj
# OlyKMZDzQ3t8RhvFM2hahW1VMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsG
# A1UdDwQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFNX2VsuP6KJc
# YmjRPZSQW9fOmhjEMFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6Ly9jcmwubWljcm9z
# b2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dF8yMDEwLTA2LTIz
# LmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0dHA6Ly93d3cubWlj
# cm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljUm9vQ2VyQXV0XzIwMTAtMDYtMjMuY3J0
# MIGgBgNVHSABAf8EgZUwgZIwgY8GCSsGAQQBgjcuAzCBgTA9BggrBgEFBQcCARYx
# aHR0cDovL3d3dy5taWNyb3NvZnQuY29tL1BLSS9kb2NzL0NQUy9kZWZhdWx0Lmh0
# bTBABggrBgEFBQcCAjA0HjIgHQBMAGUAZwBhAGwAXwBQAG8AbABpAGMAeQBfAFMA
# dABhAHQAZQBtAGUAbgB0AC4gHTANBgkqhkiG9w0BAQsFAAOCAgEAB+aIUQ3ixuCY
# P4FxAz2do6Ehb7Prpsz1Mb7PBeKp/vpXbRkws8LFZslq3/Xn8Hi9x6ieJeP5vO1r
# VFcIK1GCRBL7uVOMzPRgEop2zEBAQZvcXBf/XPleFzWYJFZLdO9CEMivv3/Gf/I3
# fVo/HPKZeUqRUgCvOA8X9S95gWXZqbVr5MfO9sp6AG9LMEQkIjzP7QOllo9ZKby2
# /QThcJ8ySif9Va8v/rbljjO7Yl+a21dA6fHOmWaQjP9qYn/dxUoLkSbiOewZSnFj
# nXshbcOco6I8+n99lmqQeKZt0uGc+R38ONiU9MalCpaGpL2eGq4EQoO4tYCbIjgg
# tSXlZOz39L9+Y1klD3ouOVd2onGqBooPiRa6YacRy5rYDkeagMXQzafQ732D8OE7
# cQnfXXSYIghh2rBQHm+98eEA3+cxB6STOvdlR3jo+KhIq/fecn5ha293qYHLpwms
# ObvsxsvYgrRyzR30uIUBHoD7G4kqVDmyW9rIDVWZeodzOwjmmC3qjeAzLhIp9cAv
# VCch98isTtoouLGp25ayp0Kiyc8ZQU3ghvkqmqMRZjDTu3QyS99je/WZii8bxyGv
# WbWu3EQ8l1Bx16HSxVXjad5XwdHeMMD9zOZN+w2/XU/pnR4ZOC+8z1gFLu8NoFA1
# 2u8JJxzVs341Hgi62jbb01+P3nSISRKhggLXMIICQAIBATCCAQChgdikgdUwgdIx
# CzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRt
# b25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xLTArBgNVBAsTJE1p
# Y3Jvc29mdCBJcmVsYW5kIE9wZXJhdGlvbnMgTGltaXRlZDEmMCQGA1UECxMdVGhh
# bGVzIFRTUyBFU046RTA0MS00QkVFLUZBN0UxJTAjBgNVBAMTHE1pY3Jvc29mdCBU
# aW1lLVN0YW1wIFNlcnZpY2WiIwoBATAHBgUrDgMCGgMVAOq7qDk4iVz8ITuZbUFr
# AG7ecxqcoIGDMIGApH4wfDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0
# b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3Jh
# dGlvbjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBIDIwMTAwDQYJ
# KoZIhvcNAQEFBQACBQDj8pLWMCIYDzIwMjEwMzEwMDgzNTM0WhgPMjAyMTAzMTEw
# ODM1MzRaMHcwPQYKKwYBBAGEWQoEATEvMC0wCgIFAOPyktYCAQAwCgIBAAICG1gC
# Af8wBwIBAAICEXIwCgIFAOPz5FYCAQAwNgYKKwYBBAGEWQoEAjEoMCYwDAYKKwYB
# BAGEWQoDAqAKMAgCAQACAwehIKEKMAgCAQACAwGGoDANBgkqhkiG9w0BAQUFAAOB
# gQAEm+4DlSjlm+kjVP/XWSP1LLC5N6X06oX7IKQb2lcTL3q5s1ynb7tB/NfXw706
# FVRph6EaD0qjarySbmh09p5ppiTU+mgVW/3/Hp3mBR1zO2hyp4DKQRvcQf8YXC7U
# ww8Nxsm6i5/qmPyT5t99cAW9Kb8buKSY/gbaUc26bwAOOTGCAw0wggMJAgEBMIGT
# MHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJjAkBgNVBAMT
# HU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwAhMzAAABN0GPQ+daW2+nAAAA
# AAE3MA0GCWCGSAFlAwQCAQUAoIIBSjAaBgkqhkiG9w0BCQMxDQYLKoZIhvcNAQkQ
# AQQwLwYJKoZIhvcNAQkEMSIEIGVoV/BvdHODfqAcTTnA/vWsfjYyINbQB2GxK7WZ
# WbUvMIH6BgsqhkiG9w0BCRACLzGB6jCB5zCB5DCBvQQgHVl+r8CeBJ0iyX/aGZD2
# YbQ7gk+U7N7BQiTDKAYSHBAwgZgwgYCkfjB8MQswCQYDVQQGEwJVUzETMBEGA1UE
# CBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9z
# b2Z0IENvcnBvcmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGltZS1TdGFtcCBQ
# Q0EgMjAxMAITMwAAATdBj0PnWltvpwAAAAABNzAiBCD2iEPL/BIac/kP8dmCd2vf
# 3BesgfWvHGDySCXAotVyszANBgkqhkiG9w0BAQsFAASCAQC7AtoEn4IIFJIe811U
# m/Tj7qIG5WVkeiEY0tpXfB6KQtTitT8ij0q9O93bSQNxItgMUBkvTm6p5fiqkF2R
# naigBL7CJMwNhl2p659+UqgSRhHEMte/sjuYDiqf/7uddVv1hrrFrnzTz3VtQ34z
# d4QNIlysBW/b2OJgHf3+rRcoZwn/E85d4O1pbj9rgGZsuMmE4uBXNZ6PWKvQr6w5
# FdRcYLltwSGY5Q4BZbiNIirhMGCAuaRhr6eGTVK/E/SHazPS/0KWlCSFo9qrBAHS
# V1M2cEA2AgqYXzrp8VKk+za4Yvrn7OMNJf5ZE5vrTdj093kjHPlatwwkOJAFiJcG
# ecaG
# SIG # End signature block
