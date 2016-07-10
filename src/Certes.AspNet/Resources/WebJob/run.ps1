
$endpoint = "https://$($Env:WEBSITE_HOSTNAME)/.certies/renew"
Write-Output "Start checking certificates..."
Write-Output "Using endpoint $($endpoint)"

wget $endpoint
