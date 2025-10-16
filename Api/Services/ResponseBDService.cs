using Api.Controllers;
using Azure;
using Azure.Core;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Playwright;
using System;
using System.Text.Json;
using static Application.DTOs.ModelsDataPandape;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Api.Services
{
    public class ResponseBDService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl = "https://ats.pandape.com";
        private readonly IUnitOfWork _unitOfWork;
        private readonly WebScrapingParserService _webScrapingParser;
        private readonly IPandapeApiService _pandapeApiService;

        public ResponseBDService(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, WebScrapingParserService webScrapingParser, IPandapeApiService pandapeApiService)
        {
            _httpClientFactory = httpClientFactory;
            _unitOfWork = unitOfWork;
            _webScrapingParser = webScrapingParser;
            _pandapeApiService = pandapeApiService;
        }


        public async Task<List<CandidatesGroupsDto>> ExtractAllVacanciesInfoFromBD(DetailRequest request, HttpClient client)
        {
            try
            {
                List<CandidatesGroupsDto> candidatesList = new List<CandidatesGroupsDto>();

                PaginationDTO pagination = new PaginationDTO
                {
                    numberPage = request.PageNumber,
                    pageSize = request.PageSize,
                };

                var listVacancies = await _unitOfWork.VacancyRepo.GetAllPaginated(pagination);

                if (listVacancies == null || !listVacancies.Data.Any())
                {
                    return new List<CandidatesGroupsDto>();
                }

                string url = "https://ats.pandape.com/Company/Match/ListMatches";

                foreach (var item in listVacancies.Data)
                {
                    int totalCandidatesPandape = 0;

                    // Primero: Consultar el total de candidatos
                    var formData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("PageNumber", "1"),
                        new KeyValuePair<string, string>("PageSize", "1"),
                        new KeyValuePair<string, string>("Order", "1"),
                        new KeyValuePair<string, string>("IdVacancy", item.ExternalId.ToString()),
                        new KeyValuePair<string, string>("IdVacancyFolder", "0"),
                    };

                    var content = new FormUrlEncodedContent(formData);
                    var response = await client.PostAsync(url, content);
                    var candidatesInVacancies = _unitOfWork.CandidateRepo.GetAllCount(c => c.ExternalVacancyId == item.ExternalId);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error HTTP {response.StatusCode} al obtener vacante {item.ExternalId}");
                        continue; // Continuar con la siguiente vacante
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                    if (jsonObject.TryGetProperty("total", out JsonElement subtotalElement) && subtotalElement.ValueKind == JsonValueKind.Number)
                    {
                        totalCandidatesPandape = subtotalElement.GetInt32();
                    }

                    Console.WriteLine($"Vacante {item.ExternalId}: {candidatesInVacancies} en BD vs {totalCandidatesPandape} en Pandape");

                    // Inactivar candidatos en BD que ya no estén en pandape
                    if (candidatesInVacancies > totalCandidatesPandape)
                    {
                        // aqui logica para buscar e inactivar los candidatos de la base de datos si no están en pandape
                    }

                        // Si no hay registros o hay menos de los que existen en Pandape
                    if (candidatesInVacancies == 0 || candidatesInVacancies < totalCandidatesPandape)
                    {
                        int cantCandidates = totalCandidatesPandape - candidatesInVacancies;
                        Console.WriteLine($"Necesitamos consultar {cantCandidates} candidatos para vacante {item.ExternalId}");

                        // Calcular número de páginas necesarias (lotes de 100)
                        int pageSize = 100;
                        int totalPages = (int)Math.Ceiling((double)totalCandidatesPandape / pageSize);

                        if (cantCandidates < 100)
                        {
                            pageSize = cantCandidates;
                            totalPages = 1;
                        }

                        List<MatchInfo> allMatchesForVacancy = new List<MatchInfo>();

                        // Consultar por lotes de 100
                        for (int currentPage = 1; currentPage <= totalPages; currentPage++)
                        {
                            try
                            {
                                Console.WriteLine($"Consultando página {currentPage} de {totalPages} para vacante {item.ExternalId}");

                                var formDataBatch = new List<KeyValuePair<string, string>>
                                {
                                    new KeyValuePair<string, string>("PageNumber", currentPage.ToString()),
                                    new KeyValuePair<string, string>("PageSize", pageSize.ToString()), // ajusdtar parta que solo traiga items necesarios
                                    new KeyValuePair<string, string>("Order", "1"),
                                    new KeyValuePair<string, string>("IdVacancy", item.ExternalId.ToString()),
                                    new KeyValuePair<string, string>("IdVacancyFolder", "0"),
                                };

                                var contentBatch = new FormUrlEncodedContent(formDataBatch);
                                var responseBatch = await client.PostAsync(url, contentBatch);

                                if (!responseBatch.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"Error en página {currentPage} para vacante {item.ExternalId}: {responseBatch.StatusCode}");
                                    continue; // Continuar con la siguiente página
                                }

                                var responseContent = await responseBatch.Content.ReadAsStringAsync();

                                try
                                {
                                    var matchesResponse = JsonSerializer.Deserialize<MatchesResponse>(responseContent, new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });

                                    var dataList = _webScrapingParser.ExtractMatchesWithSpecificSelectors(matchesResponse.ViewList, request);
                                    allMatchesForVacancy.AddRange(dataList);

                                    Console.WriteLine($"Página {currentPage} procesada: {dataList.Count} candidatos"); //bien

                                    // Pequeña pausa entre páginas para no saturar
                                    if (currentPage < totalPages)
                                    {
                                        await Task.Delay(500);
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    Console.WriteLine($"Error deserializando página {currentPage} para vacante {item.ExternalId}: {jsonEx.Message}");
                                }
                            }
                            catch (Exception pageEx)
                            {
                                Console.WriteLine($"Error en página {currentPage} para vacante {item.ExternalId}: {pageEx.Message}");
                                // Continuar con la siguiente página
                            }
                        }

                        // Agregar todos los matches de esta vacante a la lista final
                        candidatesList.Add(new CandidatesGroupsDto
                        {
                            IdVacancy = item.ExternalId,
                            Candidates = allMatchesForVacancy
                        });

                        Console.WriteLine($"Vacante {item.ExternalId} completada: {allMatchesForVacancy.Count} candidatos obtenidos");
                    }
                    else
                    {
                        // Ya tenemos todos los candidatos en BD
                        IEnumerable<PandapeCandidates> matchesCandidates = await _unitOfWork.CandidateRepo.GetAll(c => c.ExternalVacancyId == item.ExternalId);

                        // Convertir PandapeCandidates a MatchInfo si es necesario, o usar lista vacía
                        candidatesList.Add(new CandidatesGroupsDto
                        {
                            IdVacancy = item.ExternalId,
                            Candidates = new List<MatchInfo>() // O convertir matchesCandidates a List<MatchInfo>
                        });

                        Console.WriteLine($"Vacante {item.ExternalId} ya tiene todos los candidatos en BD");
                    }
                }

                Console.WriteLine($"Proceso completado. {candidatesList.Count} vacantes procesadas");
                return candidatesList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR CRÍTICO en ExtractAllVacanciesInfoFromBD: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new List<CandidatesGroupsDto>();
            }
        }

        // Método auxiliar para procesar candidatos por lotes
        private async Task<int> ProcessAndSaveCandidatesByBatches(Stages stage, int batchSize)
        {
            int totalCandidatesProcessed = 0;
            int currentBatch = 1;
            bool hasMoreCandidates = true;

            try
            {
                Console.WriteLine($"Iniciando procesamiento por lotes para etapa: {stage.Name} (Lote: {batchSize})");

                while (hasMoreCandidates)
                {
                    try
                    {
                        Console.WriteLine($"Procesando lote {currentBatch} para etapa {stage.Name}...");

                        // Aquí llamas a tu método que obtiene los candidatos desde la API/HTML
                        // Debes implementar este método según tu fuente de datos
                        var candidatesBatch = await GetCandidatesFromSource(stage, currentBatch, batchSize);

                        if (candidatesBatch == null || !candidatesBatch.Any())
                        {
                            hasMoreCandidates = false;
                            Console.WriteLine($"No hay más candidatos para etapa {stage.Name}");
                            break;
                        }

                        // Guardar el lote actual
                        await _unitOfWork.CandidateRepo.AddRangeR(candidatesBatch);
                        await _unitOfWork.Save();

                        totalCandidatesProcessed += candidatesBatch.Count;
                        Console.WriteLine($"Lote {currentBatch} guardado: {candidatesBatch.Count} candidatos");

                        // Verificar si debemos continuar (si obtuvimos menos del batchSize, es el último lote)
                        if (candidatesBatch.Count < batchSize)
                        {
                            hasMoreCandidates = false;
                        }

                        currentBatch++;

                        // Pequeña pausa entre lotes para no saturar el sistema
                        if (hasMoreCandidates)
                        {
                            await Task.Delay(1000); // 1 segundo entre lotes
                        }
                    }
                    catch (Exception batchEx)
                    {
                        Console.WriteLine($"Error en lote {currentBatch} para etapa {stage.Name}: {batchEx.Message}");
                        // Continuar con el siguiente lote
                        currentBatch++;

                        // Si hay muchos errores consecutivos, salir
                        if (currentBatch > 10) // Límite de seguridad
                        {
                            Console.WriteLine($"Demasiados errores en etapa {stage.Name}, abortando...");
                            break;
                        }
                    }
                }

                Console.WriteLine($"Etapa {stage.Name}: {totalCandidatesProcessed} candidatos procesados en {currentBatch - 1} lotes");
                return totalCandidatesProcessed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general procesando candidatos para etapa {stage.Name}: {ex.Message}");
                return totalCandidatesProcessed; // Retornar lo que se haya procesado
            }
        }


        public async Task<List<VacancyInfo>> UpdateDataVacancies(DetailRequest request, HttpClient client, string url, int cantRecent)
        {
            try
            {
                List<VacancyInfo> vacancyList = new List<VacancyInfo>();
                int totalRegistrosGuardados = 0;

                // Configurar paginación
                const int MAX_PAGE_SIZE = 100;
                //int pageSize = Math.Min(cantRecent, MAX_PAGE_SIZE);

                int[] allowedSizes = { 10, 15, 20, 25, 50, 75, 100 };
                //int baseNumber = 78; // tu número base

                int pageSize = allowedSizes
                    .Where(size => size >= cantRecent)
                    .OrderBy(size => size)
                    .FirstOrDefault();

                Console.WriteLine(pageSize);

                int totalPages = (int)Math.Ceiling((double)cantRecent / pageSize);

                Console.WriteLine($"Procesando {cantRecent} registros en {totalPages} páginas");

                for (int currentPage = 1; currentPage <= totalPages; currentPage++)
                {
                    var formData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Pagination[PageNumber]", currentPage.ToString()),
                        new KeyValuePair<string, string>("Pagination[PageSize]", pageSize.ToString()),
                        new KeyValuePair<string, string>("Order", "1")
                    };

                    var contentParams = new FormUrlEncodedContent(formData);
                    var response = await client.PostAsync(url, contentParams);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error HTTP {response.StatusCode} en página {currentPage}");
                        break; // Si falla una página, salir del loop
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                    if (jsonObject.TryGetProperty("view", out JsonElement viewElement) &&
                        viewElement.ValueKind == JsonValueKind.String)
                    {
                        var respHtml = _webScrapingParser.ExtractAllVacanciesInfoFromHtml(viewElement.GetString());

                        if (respHtml != null && respHtml.Any())
                        {
                            //foreach (var item in respHtml)
                            //{
                            //    var candidatesBD = _unitOfWork.CandidateRepo.GetAllCount(c => c.ExternalVacancyId.ToString() == item.VacancyId);
                            //    if (item.CounterNumVacancy > candidatesBD)
                            //    {
                                    
                            //    }
                            //}
                            var vacancy = await GetOrCreateVacancyAsync(respHtml);
                            await _unitOfWork.Save();
                            totalRegistrosGuardados += respHtml.Count;
                        }
                    }

                    // Pausa entre páginas para no saturar
                    if (currentPage < totalPages)
                    {
                        await Task.Delay(300);
                    }
                }
                Console.WriteLine(totalRegistrosGuardados);
                return vacancyList;
            }
            catch (Exception ex)
            {
                return new List<VacancyInfo>();
            }
        }


        // Método que necesitas implementar según tu fuente de datos
        private async Task<List<PandapeCandidates>> GetCandidatesFromSource(Stages stage, int batchNumber, int batchSize)
        {
            var candidatesToAdd = new List<PandapeCandidates>();
            try
            {
                // TODO: Implementar la lógica para obtener candidatos desde tu fuente
                // Esto puede ser desde una API, HTML scraping, etc.

                // Ejemplo de implementación:
                /*
                var request = new DetailRequest 
                {
                    // Configurar parámetros para paginación
                    PageNumber = batchNumber,
                    PageSize = batchSize,
                    // otros parámetros necesarios...
                };

                var candidatesData = await _someService.GetCandidatesAsync(request, stage.ExternalId);

                // Convertir a entidades Candidates y retornar
                return candidatesData.Select(c => new Candidates
                {
                    Id = Guid.NewGuid(),
                    StageId = stage.Id,
                    ExternalId = c.Id,
                    Name = c.Name,
                    // ... otras propiedades
                }).ToList();
                */

                // Por ahora retornar lista vacía como placeholder
                Console.WriteLine($"Obtener candidatos - Etapa: {stage.Name}, Lote: {batchNumber}, Tamaño: {batchSize}");
                return new List<PandapeCandidates>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo candidatos para etapa {stage.Name}, lote {batchNumber}: {ex.Message}");
                return new List<PandapeCandidates>();
            }
        }



        /// <summary>
        /// llena datos de la tabla Vacantes
        /// </summary>
        /// <param name="externalVacancyId"></param>
        /// <returns></returns>
        public async Task<List<Vacancies>> GetOrCreateVacancyAsync(List<VacancyInfo> vacancyInfos)
        {
            List<Vacancies> vacancieList = new List<Vacancies>();
            foreach (var vacInfo in vacancyInfos)
            {
                int externalVacancyId = int.Parse(vacInfo.VacancyId);
                //var vacancy = await GetOrCreateVacancyAsync(externalVacancyId);

                // Buscar si ya existe la vacante usando el método genérico
                var existingVacancy = await _unitOfWork.VacancyRepo.GetT(v => v.ExternalId == externalVacancyId && v.IsActive);

                if (existingVacancy == null)
                {
                    // Crear nueva vacante
                    var vacancy = new Vacancies
                    {
                        Id = Guid.NewGuid(),
                        Name = vacInfo.NameProcess,
                        ExternalId = externalVacancyId,
                        IsActive = true,
                        Location = vacInfo.Location,
                        CreatedBy = vacInfo.CreatedBy
                    };
                    vacancieList.Add(vacancy);
                    await _unitOfWork.VacancyRepo.Add(vacancy);
                    await _unitOfWork.Save();
                    var stages = await GetOrCreateStageAsync(vacInfo, vacancy.Id);
                }
            }
            Console.WriteLine(vacancieList);
            //await _unitOfWork.VacancyRepo.AddRangeR(vacancieList);
            return vacancieList;
        }


        /// <summary>
        /// llena datos de la tabla Etapas
        /// </summary>
        /// <param name="vacancy"></param>
        /// <param name="externalStageId"></param>
        /// <returns></returns>
        private async Task<List<Stages>> GetOrCreateStageAsync(VacancyInfo vacancy, Guid vacGuid)
        {
            List<Stages> stageList = new List<Stages>();
            foreach (var stageInfo in vacancy.Urls)
            {
                string textIdStage = stageInfo.Url.Split('=')[1];
                int externalStageId = int.Parse(textIdStage);

                var existingStage = await _unitOfWork.StageRepo.GetT(s => s.VacancyId == vacGuid && s.ExternalId == externalStageId);

                if (existingStage == null)
                {
                    // Crear nueva etapa
                    var stage = new Stages
                    {
                        Id = Guid.NewGuid(),
                        Name = stageInfo.Category,
                        ExternalId = int.Parse(textIdStage),
                        VacancyId = vacGuid,
                        //VacancyExternalId = int.Parse(vacancy.VacancyId),
                        //CountCandidates = int.Parse(stageInfo.Count)
                    };
                    stageList.Add(stage);
                    await _unitOfWork.StageRepo.Add(stage);
                }
            }

            // Buscar si ya existe la etapa usando el método genérico
            await _unitOfWork.StageRepo.AddRangeR(stageList);
            return stageList;
        }

       
    }
}
