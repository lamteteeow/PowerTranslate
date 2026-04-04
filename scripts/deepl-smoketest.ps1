param(
    [string]$ApiKeyFile = ".\\.local\\deepl-api-key.txt",
    [string]$Text = "Hello from PowerTranslate",
    [string]$TargetLang = "DE"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ApiKeyFile)) {
    Write-Error "API key file not found: $ApiKeyFile"
}

$apiKey = (Get-Content $ApiKeyFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($apiKey) -or $apiKey -eq "PASTE_YOUR_DEEPL_API_KEY_HERE") {
    Write-Error "Set your DeepL API key in $ApiKeyFile first."
}

$endpoint = if ($apiKey -match ':fx') {
    "https://api-free.deepl.com/v2/translate"
} else {
    "https://api.deepl.com/v2/translate"
}

$body = @{
    text = $Text
    target_lang = $TargetLang.ToUpperInvariant()
}

$headers = @{
    Authorization = "DeepL-Auth-Key $apiKey"
}

$response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body -ContentType "application/x-www-form-urlencoded"
if (-not $response.translations -or $response.translations.Count -eq 0) {
    Write-Error "DeepL returned no translations."
}

$translated = $response.translations[0].text
Write-Output "OK"
Write-Output "Target: $($TargetLang.ToUpperInvariant())"
Write-Output "Input : $Text"
Write-Output "Output: $translated"
