
$endpoint = "https://$($Env:WEBSITE_HOSTNAME)/.certes/renew"
Write-Output "Start checking certificates..."
Write-Output "Using endpoint $($endpoint)"

wget $endpoint
