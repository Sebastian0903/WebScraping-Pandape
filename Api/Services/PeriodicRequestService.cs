using Api.Controllers;
using Azure.Core;
using Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static Application.DTOs.ModelsDataPandape;

namespace Api.Services
{
    public class PeriodicRequestService : BackgroundService
    {
        private readonly ILogger<PeriodicRequestService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public PeriodicRequestService(
            ILogger<PeriodicRequestService> logger,
            IHttpClientFactory httpClientFactory,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de consulta periódica iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RealizarConsulta(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el loop principal del servicio");
                }

                // Esperar 1 minuto antes de la próxima ejecución
                await Task.Delay(TimeSpan.FromMinutes(100), stoppingToken);
            }

            _logger.LogInformation("Servicio de consulta periódica detenido");
        }

        private async Task RealizarConsulta(CancellationToken cancellationToken)
        {
            // Crear un scope principal para la consulta
            using var scope = _scopeFactory.CreateScope();

            try
            {
                _logger.LogInformation("Ejecutando consulta a las {Time}", DateTime.UtcNow);

                // Resolver TODOS los servicios scoped dentro del scope principal
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var playwrightService = scope.ServiceProvider.GetRequiredService<IPlaywrightService>();
                var responseBDService = scope.ServiceProvider.GetRequiredService<ResponseBDService>();

                LoginRequest request = new LoginRequest()
                {
                    Username = "usuario@gmail.com",
                    Password = "contraseña"
                };

                // Usar el unitOfWork resuelto dentro del scope
                var vacanciesInfoCount = unitOfWork.CandidateRepo.GetAllCount();
                string urlMatches = "https://ats.pandape.com/Company/Match/ListMatches";

                var authResult = await playwrightService.AuthenticateAsync(request);
                if (!authResult.Success)
                {
                    _logger.LogError("No se pudo iniciar sesión");
                    return;
                }

                var cookieString = string.Join("; ", authResult.Cookies.Select(c => $"{Uri.EscapeDataString(c.Name)}={Uri.EscapeDataString(c.Value)}"));

                // Configurar HttpClient con cookies
                var client = CreateConfiguredHttpClient(cookieString);
                string url = "https://ats.pandape.com/Company/Vacancy/ListVacancies";
                int intitalPageSize = 1;

                // Crear form-data
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Pagination[PageNumber]", intitalPageSize.ToString()),
                    new KeyValuePair<string, string>("Pagination[PageSize]", intitalPageSize.ToString()),
                    new KeyValuePair<string, string>("Order", "1"),
                    new KeyValuePair<string, string>("IdsFilter[]", "2")
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error HTTP {response.StatusCode} al obtener vacantes en segundo plano");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Parsear el JSON para extraer la propiedad "view" y "subtotal"
                var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                int subtotal = 0;

                // Extraer subtotal
                if (jsonObject.TryGetProperty("subtotal", out JsonElement subtotalElement) && subtotalElement.ValueKind == JsonValueKind.Number)
                {
                    subtotal = subtotalElement.GetInt32();
                }

                int cantRecent = vacanciesInfoCount < subtotal ? (subtotal - vacanciesInfoCount) : 5;

                DetailRequest requestVacancy = new DetailRequest()
                {
                    PageNumber = 1,
                    PageSize = cantRecent,
                    CookieString = cookieString
                };

                // Usar el responseBDService resuelto dentro del scope
                var data = await responseBDService.UpdateDataVacancies(requestVacancy, client, url, cantRecent);

                var dataCandidates = await responseBDService.ExtractAllVacanciesInfoFromBD(requestVacancy, client);

                if (dataCandidates != null && dataCandidates.Count > 0)
                {
                    Console.WriteLine("si funciona todo");
                    var semaphore = new SemaphoreSlim(10, 10);

                    var enrichedTasks = new List<Task>();

                    foreach (var item in dataCandidates)
                    {
                        var task = Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                // Crear un nuevo scope para cada operación de base de datos
                                using (var itemScope = _scopeFactory.CreateScope())
                                {
                                    var scopedServices = itemScope.ServiceProvider;
                                    var downloadService = scopedServices.GetRequiredService<DownloadMultipleCV>();

                                    List<EnrichedMatchInfo> enrichedMatchInfos = ConvertEnrichedMatches(item.Candidates, "", "", "", "");
                                    await downloadService.SaveCandidatesAsync(enrichedMatchInfos.ToList(), item.IdVacancy);
                                }
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, cancellationToken);

                        enrichedTasks.Add(task);
                    }

                    // Esperar a que todas las tareas se completen
                    await Task.WhenAll(enrichedTasks);
                    Console.WriteLine("pausa");
                }
                else
                {
                    _logger.LogInformation("No hay nuevos candidatos para procesar.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la consulta periódica");
            }
        }

        /// <summary>
        /// Crea un HttpClient configurado con el header de cookies
        /// </summary>
        private HttpClient CreateConfiguredHttpClient(string cookieHeader)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar cookie como header directo
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            }

            // Configurar headers estándar
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9");

            return client;
        }

        public List<EnrichedMatchInfo> ConvertEnrichedMatches(List<MatchInfo> matches, string? cvUrl, string? userNum, string? userEmail, string? error)
        {
            List<EnrichedMatchInfo> enrichedMatches = new List<EnrichedMatchInfo>();

            foreach (var match in matches)
            {
                var decodedUsername = HttpUtility.HtmlDecode(match.Username);
                var decodedDescUser = HttpUtility.HtmlDecode(match.DescriptionUser);
                enrichedMatches.Add(new EnrichedMatchInfo
                {
                    IdMatch = match.IdMatch,
                    Username = decodedUsername,
                    EmailUser = userEmail,
                    IdDetail = match.IdDetail,
                    PhoneNumber = userNum,
                    userImageSrc = match.userImageSrc,
                    IdVacancyFolder = match.IdVacancyFolder,
                    DescriptionUser = decodedDescUser,
                    CvUrl = cvUrl,
                    Error = error
                });
            }
            return enrichedMatches;
        }
    }
}