using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Web;
using static Application.DTOs.ModelsDataPandape;

namespace Api.Services
{
    public class PDFDownloadZipService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PDFDownloadZipService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Descarga todos los PDFs de la lista y los empaqueta en un ZIP
        /// </summary>
        /// <returns>Resultado con el archivo ZIP y estadísticas</returns>
        public async Task<ZipDownloadResult> DownloadPDFsAsZipAsync(BodyCvCookie bodyCvCookie)
        {
            var result = new ZipDownloadResult();

            try
            {
                string tempTextPath = "usuarios_sin_cv.txt";
                using var memoryStream = new MemoryStream();

                var client = CreateConfiguredHttpClient(bodyCvCookie.CookieString);
                var semaphore = new SemaphoreSlim(10, 10); // Limitar descargas concurrentes

                // Filtrar solo matches que tienen CV URL
                var matchesWithCv = bodyCvCookie.MatchesUser.Where(m => !string.IsNullOrEmpty(m.CvUrl)).ToList();
                var whitoutCv = bodyCvCookie.MatchesUser.Where(m => string.IsNullOrEmpty(m.CvUrl)).ToList();

                if (!matchesWithCv.Any())
                {
                    return new ZipDownloadResult
                    {
                        Success = false,
                        Error = "No hay CVs para descargar"
                    };
                }
                // Crear contenido del archivo de texto
                var emptyContentList = whitoutCv
                    .Select(x => $"{HttpUtility.HtmlDecode(x.Username)} {x.IdMatch}")
                    .OrderBy(item => item) // Orden alfabético simple
                    .ToList();
                string textContent = string.Join(Environment.NewLine, emptyContentList);

                // Usar using para asegurar que el ZIP se cierre correctamente
                using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Agregar archivo de texto al ZIP
                    if (whitoutCv.Any())
                    {
                        var textEntry = zip.CreateEntry("usuarios_sin_cv.txt");
                        using (var writer = new StreamWriter(textEntry.Open()))
                        {
                            await writer.WriteAsync(textContent);
                        }
                    }


                    var downloadTasks = matchesWithCv.Select(async match =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            return await DownloadAndAddToZip(client, match, zip);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    var downloadResults = await Task.WhenAll(downloadTasks);

                    result.SuccessfulDownloads = downloadResults.Count(r => r);
                    result.FailedDownloads = downloadResults.Count(r => !r);
                } // El ZIP se cierra aquí automáticamente

                // Ahora obtener los bytes después de cerrar el ZIP
                result.Success = true;
                result.ZipFileBytes = memoryStream.ToArray();
                result.ZipFileName = $"CVs_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                result.TotalAttempted = matchesWithCv.Count;

                Console.WriteLine($"Descarga completada: {result.SuccessfulDownloads}/{result.TotalAttempted} exitosas");
                Console.WriteLine($"Tamaño del ZIP: {result.ZipFileBytes.Length} bytes");

                // Eliminar archivo temporal si existe (ya no es necesario guardarlo en disco)
                if (File.Exists(tempTextPath))
                {
                    File.Delete(tempTextPath);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando ZIP: {ex.Message}");
                return new ZipDownloadResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Descarga un PDF individual y lo agrega al ZIP
        /// </summary>
        private async Task<bool> DownloadAndAddToZip(
        HttpClient client,
        EnrichedMatchInfo match,
        ZipArchive zip)
        {
            try
            {

                var response = await client.GetAsync(match.CvUrl);
                string extFile = GetFileExtensionFromUrl(match.CvUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error HTTP {response.StatusCode} descargando CV para {match.Username}");
                    return false;
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                if (fileBytes.Length == 0)
                {
                    Console.WriteLine($"Archivo vacío para {match.Username}");
                    return false;
                }

                // Convertir Word a PDF si es necesario
                if (extFile == ".docx" || extFile == ".doc")
                {
                    Console.WriteLine($"Convirtiendo archivo Word a PDF para {match.Username}");
                    try
                    {
                        fileBytes = ConvertWordToPdf(fileBytes);
                        extFile = ".pdf"; // Cambiar extensión a PDF
                    }
                    catch (Exception conversionEx)
                    {
                        Console.WriteLine($"Error en conversión para {match.Username}: {conversionEx.Message}");
                        // Continuar con el archivo original si la conversión falla
                    }
                }

                // Crear nombre de archivo seguro
                var safeFileName = CreateSafeFileName(match.Username, match.IdMatch, extFile);

                // Agregar al ZIP de forma thread-safe
                lock (zip)
                {
                    var entry = zip.CreateEntry(safeFileName);
                    using var entryStream = entry.Open();
                    entryStream.Write(fileBytes, 0, fileBytes.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error descargando CV para {match.Username}: {ex.Message}");
                return false;
            }
        }

        public string GetFileExtensionFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);

                // Primero intentar con el parámetro 'extension'
                var extension = queryParams["extension"];
                if (!string.IsNullOrEmpty(extension))
                {
                    return extension.StartsWith(".") ? extension : $".{extension}";
                }

                // Luego intentar con el parámetro 'filename'
                var filename = queryParams["filename"];
                if (!string.IsNullOrEmpty(filename))
                {
                    return Path.GetExtension(filename);
                }

                return ".bin"; // extensión por defecto
            }
            catch
            {
                return ".bin";
            }
        }

        private byte[] ConvertWordToPdf(byte[] wordBytes)
        {
            if (wordBytes == null || wordBytes.Length == 0)
                throw new ArgumentException("Invalid word document bytes");

            var tempDir = "/tmp";
            var sessionId = Guid.NewGuid().ToString("N")[..8];
            var inputFile = Path.Combine(tempDir, $"word_{sessionId}.docx");
            var outputDir = Path.Combine(tempDir, $"pdf_{sessionId}");

            try
            {
                // Preparar directorio
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
                Directory.CreateDirectory(outputDir);

                // Escribir documento Word
                File.WriteAllBytes(inputFile, wordBytes);

                // Intentar conversión con diferentes estrategias
                byte[] result = null;
                Exception lastException = null;

                // Estrategia 1: Conversión estándar con Java
                try
                {
                    result = TryConvertWithStrategy(inputFile, outputDir, "standard");
                    if (result != null) return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"Standard conversion failed: {ex.Message}");
                }

                // Estrategia 2: Sin Java (para documentos simples)
                try
                {
                    result = TryConvertWithStrategy(inputFile, outputDir, "no-java");
                    if (result != null) return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"No-java conversion failed: {ex.Message}");
                }

                // Estrategia 3: Modo seguro (última opción)
                try
                {
                    result = TryConvertWithStrategy(inputFile, outputDir, "safe-mode");
                    if (result != null) return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"Safe-mode conversion failed: {ex.Message}");
                }

                throw new Exception($"All conversion strategies failed. Last error: {lastException?.Message}", lastException);
            }
            finally
            {
                CleanupFiles(inputFile, outputDir);
            }
        }

        private byte[] TryConvertWithStrategy(string inputFile, string outputDir, string strategy)
        {
            using var process = new Process();

            // Configurar comando según estrategia
            string arguments;
            switch (strategy)
            {
                case "standard":
                    arguments = $"--headless --invisible --nodefault --nolockcheck --nologo --norestore " +
                               $"--convert-to pdf:writer_pdf_Export " +
                               $"--outdir \"{outputDir}\" \"{inputFile}\"";
                    break;

                case "no-java":
                    arguments = $"--headless --invisible --nodefault --nolockcheck --nologo --norestore " +
                               $"--nojava " +
                               $"--convert-to pdf:writer_pdf_Export " +
                               $"--outdir \"{outputDir}\" \"{inputFile}\"";
                    break;

                case "safe-mode":
                    arguments = $"--headless --invisible --safe-mode " +
                               $"--convert-to pdf " +
                               $"--outdir \"{outputDir}\" \"{inputFile}\"";
                    break;

                default:
                    throw new ArgumentException($"Unknown strategy: {strategy}");
            }

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/libreoffice",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = "/tmp"
            };

            // Configurar variables de entorno
            process.StartInfo.Environment.Clear();
            process.StartInfo.Environment["HOME"] = "/tmp";
            process.StartInfo.Environment["TMPDIR"] = "/tmp";
            process.StartInfo.Environment["USER"] = "root";

            // Variables específicas para Java (solo para estrategia standard)
            if (strategy == "standard")
            {
                process.StartInfo.Environment["JAVA_HOME"] = "/usr/lib/jvm/default-java";
                process.StartInfo.Environment["JRE_HOME"] = "/usr/lib/jvm/default-java";
            }

            Console.WriteLine($"Trying {strategy} strategy: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            // Ejecutar conversión
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            var finished = process.WaitForExit(120000); // 2 minutos timeout

            Console.WriteLine($"Strategy {strategy} - Finished: {finished}, ExitCode: {process.ExitCode}");
            Console.WriteLine($"Output: {output}");
            Console.WriteLine($"Error: {error}");

            if (!finished)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException($"Strategy {strategy} timed out");
            }

            // Verificar si hay warnings pero el proceso terminó exitosamente
            if (process.ExitCode != 0)
            {
                // Si es solo warning de Java pero hay PDF, continuar
                if (error.Contains("Warning: failed to launch javaldx") && process.ExitCode == 1)
                {
                    var pdfFilesIf = Directory.GetFiles(outputDir, "*.pdf");
                    if (pdfFilesIf.Length > 0)
                    {
                        Console.WriteLine("Java warning detected but PDF was created, proceeding...");
                    }
                    else
                    {
                        throw new Exception($"Strategy {strategy} failed with Java warning and no PDF created");
                    }
                }
                else
                {
                    throw new Exception($"Strategy {strategy} failed with exit code {process.ExitCode}: {error}");
                }
            }

            // Buscar archivo PDF generado
            var pdfFiles = Directory.GetFiles(outputDir, "*.pdf");
            if (pdfFiles.Length == 0)
            {
                throw new Exception($"Strategy {strategy} - No PDF file was generated");
            }

            var pdfBytes = File.ReadAllBytes(pdfFiles[0]);
            if (pdfBytes.Length == 0)
            {
                throw new Exception($"Strategy {strategy} - Generated PDF file is empty");
            }

            Console.WriteLine($"Strategy {strategy} successful: {pdfBytes.Length} bytes");
            return pdfBytes;
        }

        private static void CleanupFiles(string inputFile, string outputDir)
        {
            try
            {
                if (File.Exists(inputFile))
                {
                    File.Delete(inputFile);
                }
            }
            catch { }

            try
            {
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
            }
            catch { }
        }

        /// <summary>
        /// Crea un nombre de archivo seguro basado en el nombre de usuario
        /// </summary>
        private string CreateSafeFileName(string username, string idMatch, string extension)
        {
            try
            {
                // Decodificar entidades HTML en el username
                var decodedUsername = HttpUtility.HtmlDecode(username);

                // Si el username está vacío, usar un nombre genérico
                if (string.IsNullOrWhiteSpace(decodedUsername))
                {
                    decodedUsername = "Usuario_Sin_Nombre";
                }

                // Limpiar caracteres inválidos para nombres de archivo
                var invalidChars = Path.GetInvalidFileNameChars();
                var cleanName = new StringBuilder();

                foreach (char c in decodedUsername)
                {
                    if (invalidChars.Contains(c))
                    {
                        cleanName.Append('_');
                    }
                    else if (char.IsControl(c))
                    {
                        cleanName.Append('_');
                    }
                    else
                    {
                        cleanName.Append(c);
                    }
                }

                var safeName = cleanName.ToString().Trim();

                // Remover múltiples underscores consecutivos
                while (safeName.Contains("__"))
                {
                    safeName = safeName.Replace("__", "_");
                }

                // Limitar longitud y asegurar que no esté vacío
                if (string.IsNullOrWhiteSpace(safeName))
                {
                    safeName = "Usuario_Sin_Nombre";
                }

                safeName = safeName.Length > 50 ? safeName.Substring(0, 50) : safeName;
                safeName = safeName.TrimEnd('_');

                // Asegurar extensión .pdf
                //return $"{safeName}_{idMatch}.pdf";
                return $"{safeName}_{idMatch}{extension}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando nombre de archivo para {username}: {ex.Message}");
                return $"CV_{idMatch}.pdf";
            }
        }

        /// <summary>
        /// Crea un HttpClient configurado con cookies y headers
        /// </summary>
        private HttpClient CreateConfiguredHttpClient(string cookieHeader)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar timeout más largo para descargas
            client.Timeout = TimeSpan.FromMinutes(5);

            // Configurar cookies
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            }

            // Configurar headers
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9");

            return client;
        }
    }

    /// <summary>
    /// Resultado de la operación de descarga y creación de ZIP
    /// </summary>
    public class ZipDownloadResult
    {
        public bool Success { get; set; }
        public byte[]? ZipFileBytes { get; set; }
        public string? ZipFileName { get; set; }
        public int TotalAttempted { get; set; }
        public int SuccessfulDownloads { get; set; }
        public int FailedDownloads { get; set; }
        public string? Error { get; set; }

        public double SuccessRate => TotalAttempted > 0 ? (double)SuccessfulDownloads / TotalAttempted * 100 : 0;
    }
}
