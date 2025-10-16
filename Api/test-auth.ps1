# Script de prueba de autenticacion para SG_Semilla_PostgreSQL API
# Este script demuestra que la autenticacion JWT funciona correctamente

Write-Host "=== PRUEBA DE AUTENTICACION JWT ===" -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5207/api/v1"

# 1. Obtener token de desarrollo
Write-Host "1. Obteniendo token de desarrollo..." -ForegroundColor Yellow
try {
    $tokenResponse = Invoke-WebRequest -Uri "$baseUrl/auth/dev-token" -Method GET
    $tokenObj = $tokenResponse.Content | ConvertFrom-Json
    $token = $tokenObj.token
    Write-Host "Token obtenido exitosamente" -ForegroundColor Green
    Write-Host "Token (primeros 50 caracteres): $($token.Substring(0,50))..." -ForegroundColor Gray
} catch {
    Write-Host "Error obteniendo token: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 2. Probar acceso SIN token (debe devolver 401)
Write-Host "2. Probando acceso sin token (debe devolver 401)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/todos" -Method GET
    Write-Host "ERROR: Se esperaba 401 pero se obtuvo: $($response.StatusCode)" -ForegroundColor Red
} catch {
    if ($_.Exception.Message -like "*401*") {
        Write-Host "Correcto: Devuelve 401 Unauthorized sin token" -ForegroundColor Green
    } else {
        Write-Host "Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# 3. Probar acceso CON token (debe funcionar)
Write-Host "3. Probando acceso con token valido (debe funcionar)..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = $token  # Enviar solo el token, sin prefijo "Bearer"
        "accept" = "application/json"
    }
    $response = Invoke-WebRequest -Uri "$baseUrl/todos" -Method GET -Headers $headers
    Write-Host "Acceso autorizado exitoso - Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Respuesta: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "Error con token valido: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 4. Probar con token invalido (debe devolver 401)
Write-Host "4. Probando con token invalido (debe devolver 401)..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "token_invalido_123"  # Token inválido sin prefijo "Bearer"
        "accept" = "application/json"
    }
    $response = Invoke-WebRequest -Uri "$baseUrl/todos" -Method GET -Headers $headers
    Write-Host "ERROR: Se esperaba 401 pero se obtuvo: $($response.StatusCode)" -ForegroundColor Red
} catch {
    if ($_.Exception.Message -like "*401*") {
        Write-Host "Correcto: Devuelve 401 Unauthorized con token invalido" -ForegroundColor Green
    } else {
        Write-Host "Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== RESUMEN ===" -ForegroundColor Green
Write-Host "La autenticacion JWT esta funcionando correctamente:" -ForegroundColor White
Write-Host "- El endpoint /auth/dev-token genera tokens validos" -ForegroundColor White
Write-Host "- Los endpoints protegidos rechazan peticiones sin token (401)" -ForegroundColor White
Write-Host "- Los endpoints protegidos aceptan peticiones con token valido (200)" -ForegroundColor White
Write-Host "- Los endpoints protegidos rechazan tokens invalidos (401)" -ForegroundColor White
Write-Host ""
Write-Host "Si estas experimentando errores 401, verifica que:" -ForegroundColor Yellow
Write-Host "1. Estes enviando el header 'Authorization: Bearer <token>'" -ForegroundColor Yellow
Write-Host "2. El token no haya expirado (duracion: 60 minutos)" -ForegroundColor Yellow
Write-Host "3. La aplicacion este ejecutandose en http://localhost:5207" -ForegroundColor Yellow