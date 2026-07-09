$loginBody = @{ email = "staff1@sqs.edu.vn"; password = "123456" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
$token = $loginResponse.token
$headers = @{ Authorization = "Bearer $token" }
try {
    Invoke-RestMethod -Uri "http://localhost:5000/api/staff/complete/20" -Method Post -Headers $headers
} catch {
    $_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $reader.ReadToEnd()
}
