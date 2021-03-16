param (
    [Parameter()][string]$Configuration = "Release",
    [Parameter()][string]$RuntimeIdentifier = "win7-x64",
    [Parameter()][string]$CertificatePath = "Signing.pfx"
)

$cert = Get-PfxCertificate $CertificatePath
dotnet publish -c $Configuration -r $RuntimeIdentifier
Get-ChildItem -Recurse -File "bin\$Configuration\net5.0\$RuntimeIdentifier\publish\*" -Include ('*.dll', '*.exe', '*.ps*') | ForEach-Object {
    if ((get-authenticodesignature $_).status -ne "valid") {
        Set-AuthenticodeSignature $_ $cert 
    } 
}

# SIG # Begin signature block
# MIIGjAYJKoZIhvcNAQcCoIIGfTCCBnkCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUvJst7qFE6/WI7wFzRw4QtV00
# zuWgggQlMIIEITCCAgmgAwIBAgIQeCEN9VB1iKpAMNgkEDD/djANBgkqhkiG9w0B
# AQUFADAcMRowGAYDVQQDDBFXYW4gS2VyciBDby4gTHRkLjAeFw0yMTAzMTYxNDQ1
# NThaFw0yNTAxMDEwMDAwMDBaMBQxEjAQBgNVBAMMCVBvd2VyQ29yZDCCASIwDQYJ
# KoZIhvcNAQEBBQADggEPADCCAQoCggEBAM/x3tPDbKg6itBm5qZHgbouHsW23NeY
# 2riz+1SZtD8RJXiPi4nIhfSG5FqVhgVG8iF2sQOiVMu/L0FFVRxZcvyDwc+NJywI
# ClH88LMGJDRB9V5DQ/Rlsr9QzXWQU8wXjVBcEtohO6ZDnInGb53WjLOCYoeZvl9B
# onCrTBiLdts2Wn+i2C9cU/zU+4uGGH7SMaaby6ktUKcO+c/PDfZmT/cc5OwXLOyw
# sLDxJN5S0oSpPV3IXUMFrLpp0oJpY0WLabJwx9JnN2knqNZVRcKrHm7URAO4QSYo
# felNO6Hh1UPwXsAz/GA7y+BX+WIHw0KgZph4yQmDfA6VE2LP795Tz2kCAwEAAaNn
# MGUwDgYDVR0PAQH/BAQDAgeAMBMGA1UdJQQMMAoGCCsGAQUFBwMDMB8GA1UdIwQY
# MBaAFJlj37DyEnjsODd1M5OPl+Nj+2WbMB0GA1UdDgQWBBQ82Rzl8Dw+VJVU4E8N
# lLaPyucFGTANBgkqhkiG9w0BAQUFAAOCAgEACTBfn+Gj4OfjTMf8xebDe+TtB1q1
# 605iHz/9GNJA/IEhSFgHU+Z/ihh3+GHtb/Q9v6/qSmKeLLHVawJHRR7JdvydmYxg
# J0rKM9OK84FOH+B6zx0A8X15TjhDOC6OUIuAd7WP4dzBCEwbJoiGk6kKzr4K4KBd
# vGbPoWmX6yLirJgvy+yAtKdx3CU86lm/LYuESpLxFOgQkExEWJeo9nJZMd47HZAS
# dHVMGG5D7V0XDtHjSLowC9GmSa2G40zM68aFp7yOplQlnvAEl9T8gmjVCHvibHex
# NgNb/V8YwZcUJPg7zCf1GrJljUNiz6DmsqqXmqNMLomVQOF2aJHGQey45dZ/YSF4
# E4lZR261NGcpQA4Oe6NHR7AK2HDUcWkjOnU2i3R2QG3Tq1ETLtxTuk64a1dxENhV
# vT8ihKRQWUYOSwcCva6Me+Uy2M5J7PTQbuq8YulKjBQ3Q+gsmfNtp7LAFFQ7jXtg
# nAZh5Vs7drv3umZha0qkC1bFJhuTI3Ymt/i3C0uHwrnS4KhGxNxrX9jZ3OvY3BTu
# bfVbFnf93YpUDrs9WdTqWEZfOomzYUcYc2cEvWvT2lcN2R+YoinO2BDSJXZvizIo
# EFy08vNErFIC6k+muOJBrnQ5/NXulSXY+GQUlGq2znboM/EOfvuDd5BIHqGhjYxD
# jx8bK4UnZkSEhokxggHRMIIBzQIBATAwMBwxGjAYBgNVBAMMEVdhbiBLZXJyIENv
# LiBMdGQuAhB4IQ31UHWIqkAw2CQQMP92MAkGBSsOAwIaBQCgeDAYBgorBgEEAYI3
# AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3AgEEMBwGCisG
# AQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMCMGCSqGSIb3DQEJBDEWBBTvxuIfxkth
# OzqyJHkGDh1QY0SUjzANBgkqhkiG9w0BAQEFAASCAQAt3nzOnh8T3kq2yY+2P7uN
# EWZT9E4qth19uiVf2P+kxHVMx0gw2brypP6eHHVTeQZr1UWodgaclchCDjaIGN2B
# 4iA4qlZlrjsm5UCfr0Ddi2PDEjhFlbP8ZDBDVVvXNBklzD5HR/fLcJvPUGFGAbV0
# KJnCsuUMMR8rzHTOgQURGp1WsA0SX3s3XKe32Cu8gfsJtdbvKJsLEkDJCumcpegb
# DYn+Ap/R7/m2srSpPiSci160smTF+Ml8alhLtbR/FZEt67vxXxdhZLzGQVnVqdmS
# kM1PZz+MSMYzbqCjH5YEwpVFO/DHbpqxmUl9vwRBL6xdDdr/+x4NhE7aNbZ72ps2
# SIG # End signature block
