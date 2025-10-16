using Domain.Entities;
using Domain.Repositories;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using static Application.DTOs.ModelsDataPandape;

namespace Api.Services
{
    public class DownloadMultipleCV
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl = "https://ats.pandape.com"; 
        private readonly IUnitOfWork _unitOfWork;
        private readonly ResponseBDService _responseBDService;

        public DownloadMultipleCV(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, ResponseBDService responseBDService)
        {
            _httpClientFactory = httpClientFactory;
            _unitOfWork = unitOfWork;
            _responseBDService = responseBDService;
        }

        /// <summary>
        /// Procesa una lista de matches y extrae las URLs de CV de cada uno
        /// </summary>
        /// <param name="matches">Lista de matches con información básica</param>
        /// <param name="cookieHeader">String de cookies para el header Cookie</param>
        /// <returns>Lista de matches enriquecidos con URLs de CV</returns>
        public async Task<List<EnrichedMatchInfo>> ProcessMatchesAsync(List<MatchInfo> matches, string cookieHeader, int idVacancy)
        {
            var enrichedMatches = new List<EnrichedMatchInfo>();
            var client = CreateConfiguredHttpClient(cookieHeader);

            var semaphore = new SemaphoreSlim(10, 10);
            var tasks = matches.Select(async match =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ProcessSingleMatchAsync(client, match, idVacancy);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            enrichedMatches.AddRange(results.Where(r => r != null));

            // Guardar candidatos en la base de datos
            await SaveCandidatesAsync(enrichedMatches, idVacancy);

            return enrichedMatches;
        }

        /// <summary>
        /// Guarda una lista de candidatos a la tabla PandapeCandidates
        /// </summary>
        /// <param name="enrichedMatches"></param>
        /// <param name="idVacancy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SaveCandidatesAsync(List<EnrichedMatchInfo> enrichedMatches, int idVacancy)
        {
            if (!enrichedMatches.Any()) return;

            // Obtener la vacancy
            var vacancy = await _unitOfWork.VacancyRepo.GetT(
                predicate: v => v.ExternalId == idVacancy,
                includeProperties: "Stages"
            );

            if (vacancy == null)
            {
                throw new Exception($"Vacancy with IdVacancy {idVacancy} not found");
            }

            var candidatesToAdd = new List<PandapeCandidates>();

            foreach (var match in enrichedMatches)
            {
                // Buscar el stage correspondiente por IdVacancyStage (IdVacancyFolder del match)
                var stage = vacancy.Stages.FirstOrDefault(s => s.ExternalId == match.IdVacancyFolder);

                if (stage == null)
                {
                    // Si no existe el stage, podrías crearlo o usar un stage por defecto
                    // Por ahora, skip este candidato
                    Console.WriteLine($"Stage with IdVacancyStage {match.IdVacancyFolder} not found for candidate {match.Username}");
                    continue;
                }

                // Verificar si el candidato ya existe (por IdMatch para evitar duplicados)
                var existingCandidate = await _unitOfWork.CandidateRepo.GetT(
                    predicate: c => c.ExternalMatchId == match.IdMatch && c.StageId == stage.Id
                );

                if (existingCandidate != null)
                {
                    // Actualizar candidato existente si es necesario
                    existingCandidate.PhoneNumber = match.PhoneNumber ?? existingCandidate.PhoneNumber;
                    existingCandidate.Email = match.EmailUser ?? existingCandidate.Email;
                    existingCandidate.Description = match.DescriptionUser ?? existingCandidate.Description;
                    existingCandidate.ProfileImageUrl = ExtractImageGuid(match.userImageSrc);
                    existingCandidate.CvUrl = ExtractCvGuid(match.CvUrl);
                    continue;
                }

                // Crear nuevo candidato
                var newCandidate = CreateCandidateFromMatch(match, vacancy, stage);
                candidatesToAdd.Add(newCandidate);
            }

            // Agregar todos los candidatos nuevos
            if (candidatesToAdd.Any())
            {
                await _unitOfWork.CandidateRepo.AddRangeR(candidatesToAdd);
                await _unitOfWork.Save();
            }
        }

        /// <summary>
        /// Guarda la información de usuarios, vacantes y etapas
        /// </summary>
        /// <param name="enrichedMatches"></param>
        /// <param name="idVacancy"></param>
        /// <returns></returns>
        //private async Task SaveToAllTablesAsync(List<EnrichedMatchInfo> enrichedMatches, int idVacancy)
        //{
        //    if (!enrichedMatches.Any()) return;

        //    // 1. Obtener o crear la vacante
        //    var vacancy = await GetOrCreateVacancyAsync(idVacancy);

        //    // 2. Agrupar por etapa para procesar más eficientemente
        //    var matchesByStage = enrichedMatches.GroupBy(m => m.IdVacancyFolder);

        //    foreach (var stageGroup in matchesByStage)
        //    {
        //        var stageId = stageGroup.Key;
        //        var stageMatches = stageGroup.ToList();

        //        // 3. Obtener o crear la etapa una vez por grupo
        //        var stage = await GetOrCreateStageAsync(vacancy, stageId);

        //        // 4. Obtener candidatos existentes para esta etapa usando el método genérico
        //        var existingCandidates = await _unitOfWork.CandidateRepo.GetAll(
        //            c => c.IdVacancy == idVacancy &&
        //                 c.IdVacancyFolder == stageId &&
        //                 c.IsActive
        //        );

        //        var existingIdMatches = existingCandidates.Select(c => c.IdMatch).ToHashSet();

        //        // 5. Separar nuevos y existentes
        //        var newCandidates = new List<PandapeCandidates>();
        //        var candidatesToUpdate = new List<PandapeCandidates>();

        //        foreach (var enrichedMatch in stageMatches)
        //        {
        //            if (existingIdMatches.Contains(enrichedMatch.IdMatch))
        //            {
        //                var existing = existingCandidates.First(c => c.IdMatch == enrichedMatch.IdMatch);
        //                UpdateCandidateFromMatch(existing, enrichedMatch, stage);
        //                candidatesToUpdate.Add(existing);
        //            }
        //            else
        //            {
        //                var candidate = CreateCandidateFromMatch(enrichedMatch, vacancy, stage);
        //                newCandidates.Add(candidate);
        //            }
        //        }

        //        // 6. Guardar en lote usando los métodos genéricos
        //        if (newCandidates.Any())
        //        {
        //            await _unitOfWork.CandidateRepo.AddRangeR(newCandidates);
        //        }

        //        if (candidatesToUpdate.Any())
        //        {
        //            foreach (var candidate in candidatesToUpdate)
        //            {
        //                _unitOfWork.CandidateRepo.Update(candidate);
        //            }
        //        }
        //    }

        //    await _unitOfWork.Save();
        //}

        

        private PandapeCandidates CreateCandidateFromMatch(EnrichedMatchInfo enrichedMatch, Vacancies vacancy, Stages stage)
        {
            string guidCv = ExtractCvGuid(enrichedMatch.CvUrl);
            string guidImg = ExtractImageGuid(enrichedMatch.userImageSrc);

            return new PandapeCandidates
            {
                Id = Guid.NewGuid(),
                Username = enrichedMatch.Username ?? string.Empty,
                PhoneNumber = enrichedMatch.PhoneNumber ?? string.Empty,
                CvUrl = guidCv,
                ProfileImageUrl = guidImg,
                IsActive = true,

                // Foreign Key - Solo StageId
                StageId = stage.Id,

                // Campos de compatibilidad
                ExternalVacancyId = vacancy.ExternalId,
                ExternalStageId = enrichedMatch.IdVacancyFolder,
                ExternalMatchId = enrichedMatch.IdMatch,
                Description = enrichedMatch.DescriptionUser ?? string.Empty,
                Email = enrichedMatch.EmailUser ?? string.Empty
            };
        }

        //private string ExtractCvGuid(string? cvUrl)
        //{
        //    if (string.IsNullOrEmpty(cvUrl)) return string.Empty;

        //    // Extraer GUID del CV URL si es necesario
        //    // Ajusta según el formato de tu URL
        //    return cvUrl;
        //}

        //private string ExtractImageGuid(string? imageUrl)
        //{
        //    if (string.IsNullOrEmpty(imageUrl)) return string.Empty;

        //    // Extraer GUID de la imagen URL si es necesario
        //    return imageUrl;
        //}

        private void UpdateCandidateFromMatch(PandapeCandidates candidate, EnrichedMatchInfo enrichedMatch, Stages stage)
        {
            string guidCv = ExtractCvGuid(enrichedMatch.CvUrl);
            string guidImg = ExtractImageGuid(enrichedMatch.userImageSrc);

            candidate.Username = enrichedMatch.Username ?? candidate.Username;
            candidate.PhoneNumber = enrichedMatch.PhoneNumber ?? candidate.PhoneNumber;
            candidate.CvUrl = !string.IsNullOrEmpty(guidCv) ? guidCv : candidate.CvUrl;
            candidate.ProfileImageUrl = !string.IsNullOrEmpty(guidImg) ? guidImg : candidate.ProfileImageUrl;
            candidate.Description = enrichedMatch.DescriptionUser ?? candidate.Description;
            candidate.Email = enrichedMatch.EmailUser ?? candidate.Email;
            candidate.StageId = stage.Id;
            candidate.ExternalStageId = stage.ExternalId; // Actualizado de IdVacancyStage
            candidate.UpdatedAt = DateTime.UtcNow; // Agregar timestamp de actualización
        }

        /// <summary>
        /// Procesa un match individual para extraer la URL del CV
        /// </summary>
        private async Task<EnrichedMatchInfo?> ProcessSingleMatchAsync(HttpClient client, MatchInfo match, int idVacancy)
        {
            try
            {
                var detailCvUrl = $"{_baseUrl}/Company/Match/MenuDetail?id={match.IdDetail}&idMatch={match.IdMatch}&idVacancy={idVacancy}";

                var responseCv = await client.GetAsync(detailCvUrl);


                if (!responseCv.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error HTTP {responseCv.StatusCode} para match {match.IdMatch}");
                    return CreateEnrichedMatch(match, null, null, null, $"HTTP {responseCv.StatusCode}");
                }

                //var htmlContent = await response.Content.ReadAsStringAsync();
                var htmlContentCv = await responseCv.Content.ReadAsStringAsync();

                // Extraer la imagen del usuario del div con id "UserImg"
                var dtaHtml = ExtractCandidateDetailsFromHtml(htmlContentCv);
                string cvUrl = dtaHtml.cvUrl;
                string userNum = dtaHtml.phoneNumber;
                string userEmail = dtaHtml.EmailUser;


                return CreateEnrichedMatch(match, cvUrl, userNum, userEmail, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando match {match.IdMatch}: {ex.Message}");
                return CreateEnrichedMatch(match, null, null, null, ex.Message);
            }
        }


        /// <summary>
        /// Obtiene la lista html de los procesos que hay
        /// </summary>
        public async Task<(List<VacancyInfo> processList, int Subtotal)> GetVacanciesHtmlAsync(DetailRequest request)
        {
            try
            {
                var client = CreateConfiguredHttpClient(request.CookieString);
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
                    Console.WriteLine($"Error HTTP {response.StatusCode} al obtener vacantes");
                    return (null, 0);
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

                var totalVacanciesCount = _unitOfWork.VacancyRepo.GetAllCount();

                // Si está vacio, sacar toda la info de pandape
                if (totalVacanciesCount == 0)
                {
                    //int cantRecent = subtotal > 0 ? (subtotal - totalVacanciesCount) : 20;
                    Console.WriteLine(subtotal);
                    var data = await _responseBDService.UpdateDataVacancies(request, client, url, subtotal);
                    Console.WriteLine(data);
                    return (data, subtotal);
                }

                if (totalVacanciesCount > 0 && totalVacanciesCount == subtotal)
                {
                    List<VacancyInfo> totalVacancies = new List<VacancyInfo>();
                    int cantRecent = subtotal > 0 ? (subtotal - totalVacanciesCount) : 20;
                    //List<EnrichedMatchInfo> enrichedMatchInfos = new List<EnrichedMatchInfo>();
                    var vacanciesInfo = await _responseBDService.ExtractAllVacanciesInfoFromBD(request, client);

                    foreach (var item in vacanciesInfo)
                    {
                        //ProcessMatchesAsync(item.Candidates, request.CookieString, item.IdVacancy);
                        var semaphore = new SemaphoreSlim(10, 10);
                        var enrichedTasks = item.Candidates.Select(async match =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                return CreateEnrichedMatchBase(match, "", "", "", null);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }).ToList();

                        // Esperar a que todas las tareas se completen
                        var enrichedMatches = await Task.WhenAll(enrichedTasks);

                        await SaveCandidatesAsync(enrichedMatches.ToList(), item.IdVacancy);

                        //enrichedMatchInfos.AddRange(enrichedMatches);
                    }


                    PaginationDTO PaginationDTO = new PaginationDTO()
                    {
                        numberPage = request.PageNumber,
                        pageSize = request.PageSize,
                        category = null,
                        asigned = null,
                        textSearch = null
                    };
                    var paginatedResult = await _unitOfWork.VacancyRepo.GetAllPaginated(PaginationDTO, v => v.IsActive, includeProperties: "Stages");

                    foreach (var vacancy in paginatedResult.Data)
                    {
                        int cantCountCandidates = _unitOfWork.CandidateRepo.GetAllCount(c => c.ExternalVacancyId == vacancy.ExternalId && c.IsActive);

                        var vacancyInfo = new VacancyInfo
                        {
                            VacancyId = vacancy.ExternalId.ToString(),
                            NameProcess = vacancy.Name,
                            Location = vacancy.Location,
                            CreatedBy = vacancy.CreatedBy,
                            CounterNumVacancy = cantCountCandidates,
                            StatusProcess = "Publicado",
                            Urls = new List<VacancyUrlInfo>()
                        };
                        //// Agregar URLs de etapas
                        //foreach (var stage in vacancy.Stages)
                        //{
                        //    var urlInfo = new VacancyUrlInfo
                        //    {
                        //        Url = $"https://ats.pandape.com/Company/Vacancy/FolderMatches?idVacancy={vacancy.ExternalId}&idVacancyFolder={stage.ExternalId}",
                        //        Category = stage.Name,
                        //        Count = stage.CandidatesCount
                        //    };
                        //    vacancyInfo.Urls.Add(urlInfo);
                        //}
                        //totalVacancies.Add(vacancyInfo);
                    }


                    Console.WriteLine("saca info de base de datos");

                    return (totalVacancies, subtotal);
                }
                else
                {
                    //int cantRecent = subtotal > 0? ( subtotal - totalVacanciesCount ): 20;
                    int cantRecent = 20;
                    Console.WriteLine(cantRecent);
                    var data = await _responseBDService.UpdateDataVacancies(request, client, url, cantRecent);
                    Console.WriteLine(data);
                    return (data, subtotal);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo vacantes: {ex.Message}");
                return (null, 0);
            }
        }

        /// <summary>
        /// Extrae tanto la URL del CV como el número de teléfono desde el HTML
        /// </summary>
        private (string? cvUrl, string? phoneNumber, string? EmailUser) ExtractCandidateDetailsFromHtml(string htmlContent)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                string? cvUrl = null;
                string? phoneNumber = null;
                string? EmailUser = null;

                // Extraer URL del CV
                var attachCvDiv = doc.DocumentNode.SelectSingleNode("//div[@id='AttachCV']");
                if (attachCvDiv != null)
                {
                    var cvLink = attachCvDiv.SelectSingleNode(".//div[contains(@class, 'col-9')]//a[@href]");
                    if (cvLink != null)
                    {
                        var href = cvLink.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(href))
                        {
                            var decodedHref = HttpUtility.HtmlDecode(href);
                            cvUrl = decodedHref.StartsWith("/") ? $"{_baseUrl}{decodedHref}" : decodedHref;
                        }
                    }
                }

                // Extraer número de teléfono
                var whatsappLink = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'js_WhatsappLink')]");
                if (whatsappLink != null)
                {
                    // Intentar extraer del texto primero
                    phoneNumber = whatsappLink.InnerText?.Trim();

                    // Si no hay texto, extraer del href
                    if (string.IsNullOrEmpty(phoneNumber))
                    {
                        var href = whatsappLink.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(href))
                        {
                            var decodedHref = HttpUtility.HtmlDecode(href);

                            if (decodedHref.Contains("wa.me/"))
                            {
                                var number = decodedHref.Split(new[] { "wa.me/" }, StringSplitOptions.RemoveEmptyEntries);
                                if (number.Length > 1)
                                {
                                    phoneNumber = number[1].Split('?', '&')[0];
                                }
                            }
                            else
                            {
                                phoneNumber = decodedHref;
                            }
                        }
                    }
                }

                // Extraer correo de usuario
                var emailUserText = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'mailto:')]");
                if (emailUserText != null)
                {
                    // Intentar extraer del texto primero
                    EmailUser = emailUserText.InnerText?.Trim();

                    // Si no hay texto, extraer del href
                    if (string.IsNullOrEmpty(EmailUser))
                    {
                        var href = emailUserText.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(href))
                        {
                            var decodedHref = HttpUtility.HtmlDecode(href);

                            if (decodedHref.Contains("mailto:"))
                            {
                                var email = decodedHref.Split(new[] { "mailto:" }, StringSplitOptions.RemoveEmptyEntries);
                                if (email.Length > 1)
                                {
                                    EmailUser = email[1].Split(':')[0];
                                }
                            }
                            else
                            {
                                EmailUser = decodedHref;
                            }
                        }
                    }
                }

                return (cvUrl, phoneNumber, EmailUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extrayendo detalles del candidato: {ex.Message}");
                return (null, null, ex.Message);
            }
        }


        /// <summary>
        /// Lee el HTML completo y extrae la información de todas las vacantes
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public List<VacancyInfo> ExtractAllVacanciesInfoFromHtml(string htmlContent)
        {
            var vacanciesInfo = new List<VacancyInfo>();

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Buscar todos los divs cuyo id comienza con "rowVacancy_"
                var vacancyDivs = doc.DocumentNode.SelectNodes("//div[starts-with(@id, 'rowVacancy_')]");

                if (vacancyDivs == null || !vacancyDivs.Any())
                {
                    Console.WriteLine("No se encontraron divs con id que comience con 'rowVacancy_'");
                    return vacanciesInfo;
                }

                Console.WriteLine($"Se encontraron {vacancyDivs.Count} vacantes");

                // Procesar cada div individualmente
                foreach (var vacancyDiv in vacancyDivs)
                {
                    var vacancyInfo = ExtractVacancyInfoFromDiv(vacancyDiv);
                    if (vacancyInfo != null)
                    {
                        // Extraer el ID de la vacante del atributo id
                        var divId = vacancyDiv.GetAttributeValue("id", "");
                        vacancyInfo.VacancyId = divId.Replace("rowVacancy_", "");

                        vacanciesInfo.Add(vacancyInfo);
                    }
                }

                Console.WriteLine($"Se extrajo información de {vacanciesInfo.Count} vacantes exitosamente");
                return vacanciesInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extrayendo información de todas las vacantes: {ex.Message}");
                return vacanciesInfo;
            }
        }

        /// <summary>
        /// Obtiene los datos de cada elemento del div
        /// </summary>
        private VacancyInfo? ExtractVacancyInfoFromDiv(HtmlNode vacancyDiv)
        {
            try
            {
                var vacancyInfo = new VacancyInfo();
                vacancyInfo.Urls = new List<VacancyUrlInfo>();

                // Extraer el nombre del proceso
                var nameElement = vacancyDiv.SelectSingleNode(".//a[contains(@class, 'font-xl')]//span[contains(@class, 'text-capitalize-first')]");
                if (nameElement != null)
                {
                    vacancyInfo.NameProcess = HttpUtility.HtmlDecode(nameElement.InnerText?.Trim());
                }

                // Extraer la ubicación
                var locationElement = vacancyDiv.SelectSingleNode(".//div[contains(@class, 'vacancyLocation')]//div[contains(@class, 'col-auto')]");
                if (locationElement != null)
                {
                    // Obtener el texto, excluyendo el contenido del ícono
                    var locationText = locationElement.InnerText?.Trim();
                    // Alternativamente, si el ícono interfiere, buscar el texto después del ícono
                    //var iconElement = locationElement.SelectSingleNode(".//div[contains(@class, 'icon-container')]");
                    //if (iconElement != null && locationElement.LastChild != null)
                    //{
                    //    // Obtener el texto que viene después del contenedor del ícono
                    //    var textNode = iconElement.NextSibling;
                    //    while (textNode != null && string.IsNullOrWhiteSpace(textNode.InnerText))
                    //    {
                    //        textNode = textNode.NextSibling;
                    //    }
                    //    if (textNode != null)
                    //    {
                    //        vacancyInfo.Location = HttpUtility.HtmlDecode(textNode.InnerText?.Trim());
                    //    }
                    //}
                    //else
                    //{
                    //}
                        vacancyInfo.Location = HttpUtility.HtmlDecode(locationText);
                }

                // Extraer el nombre del creador
                var creatorSection = vacancyDiv.SelectSingleNode(".//strong[contains(text(), 'Creado por:')]");
                if (creatorSection != null && creatorSection.ParentNode != null)
                {
                    // Buscar el siguiente span que contiene el nombre
                    var nameSpan = creatorSection.ParentNode.SelectSingleNode(".//span[contains(@style, 'margin-right')]");
                    var dateSpan = creatorSection.ParentNode.SelectSingleNode(".//span[contains(@class, 'text-lowercase')]");
                    if (nameSpan != null)
                    {
                        var creatorName = nameSpan.InnerText?.Trim();
                        var dateCreate = dateSpan.InnerText?.Trim();
                        // Remover la coma final si existe
                        if (!string.IsNullOrEmpty(creatorName) && creatorName.EndsWith(","))
                        {
                            creatorName = creatorName.TrimEnd(',', '-');
                        }
                        vacancyInfo.CreatedBy = $"{HttpUtility.HtmlDecode(creatorName)} - {HttpUtility.HtmlDecode(dateCreate)}";
                    }
                }

                // Extraer el número de candidatos
                var counterElement = vacancyDiv.SelectSingleNode(".//div[contains(@class, 'counter-num')]");
                if (counterElement != null)
                {
                    var text = counterElement.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var cleanText = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "").Trim();
                        Match match = Regex.Match(cleanText, @"^\d+");
                        string numberText = match.Success ? match.Value : "0";

                        int numConut = int.Parse(numberText);
                        vacancyInfo.CounterNumVacancy = numConut;
                    }
                }

                // Extraer el estado del proceso
                var statusElement = vacancyDiv.SelectSingleNode(".//a[contains(@class, 'js_customDDbt') and contains(@class, 'c-green')]//span[contains(@class, 'js_btnText')]");
                if (statusElement != null)
                {
                    vacancyInfo.StatusProcess = statusElement.InnerText?.Trim();
                }

                // Extraer las URLs del total-detail-list
                var detailList = vacancyDiv.SelectSingleNode(".//div[contains(@class, 'total-detail-list')]");
                if (detailList != null)
                {
                    // Buscar todos los enlaces dentro del total-detail-list
                    var linkElements = detailList.SelectNodes(".//a[contains(@class, 'total-detail-item')]");

                    if (linkElements != null)
                    {
                        foreach (var linkElement in linkElements)
                        {
                            var urlInfo = new VacancyUrlInfo();

                            // Extraer la URL
                            urlInfo.Url = linkElement.GetAttributeValue("href", "");

                            // Extraer la categoría (texto del div con clase "counter-text")
                            var categoryElement = linkElement.SelectSingleNode(".//div[contains(@class, 'counter-text')]");
                            if (categoryElement != null)
                            {
                                urlInfo.Category = HttpUtility.HtmlDecode(categoryElement.InnerText?.Trim());
                            }

                            // Extraer el contador (texto del div con clase "counter-num")
                            var countElement = linkElement.SelectSingleNode(".//div[contains(@class, 'counter-num')]");
                            if (countElement != null)
                            {
                                var countText = countElement.InnerText?.Trim();
                                if (!string.IsNullOrEmpty(countText))
                                {
                                    // Limpiar y extraer el número principal
                                    var cleanCount = System.Text.RegularExpressions.Regex.Replace(countText, @"<[^>]+>", "").Trim();
                                    var numberMatch = System.Text.RegularExpressions.Regex.Match(cleanCount, @"\d+");
                                    urlInfo.Count = numberMatch.Success ? numberMatch.Value : cleanCount;
                                }
                            }

                            vacancyInfo.Urls.Add(urlInfo);
                        }
                    }
                }

                // Verificar que al menos un campo tenga datos
                if (string.IsNullOrEmpty(vacancyInfo.NameProcess) &&
                    string.IsNullOrEmpty(vacancyInfo.CounterNumVacancy.ToString()) &&
                    string.IsNullOrEmpty(vacancyInfo.StatusProcess) &&
                    string.IsNullOrEmpty(vacancyInfo.Location) &&
                    string.IsNullOrEmpty(vacancyInfo.CreatedBy) &&
                    (vacancyInfo.Urls == null || !vacancyInfo.Urls.Any()))
                {
                    Console.WriteLine($"No se pudieron extraer datos del div {vacancyDiv.GetAttributeValue("id", "unknown")}");
                    return null;
                }

                return vacancyInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extrayendo información de un div de vacante: {ex.Message}");
                return null;
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

        /// <summary>
        /// Crea un objeto EnrichedMatchInfo a partir de un MatchInfo
        /// </summary>
        public EnrichedMatchInfo CreateEnrichedMatch(MatchInfo match, string? cvUrl, string? userNum, string? userEmail, string? error)
        {
            var decodedUsername = HttpUtility.HtmlDecode(match.Username);
            var decodedDescUser = HttpUtility.HtmlDecode(match.DescriptionUser);
            return new EnrichedMatchInfo
            {
                IdMatch = match.IdMatch,
                Username = decodedUsername,
                //Href = match.Href,
                EmailUser = userEmail,
                IdDetail = match.IdDetail,
                PhoneNumber = userNum,
                userImageSrc = match.userImageSrc,
                IdVacancyFolder = match.IdVacancyFolder,
                DescriptionUser = decodedDescUser,
                CvUrl = cvUrl,
                Error = error
            };
        }


        /// <summary>
        /// Crea un objeto EnrichedMatchInfo con información base sin detalles adicionales
        /// </summary>
        private EnrichedMatchInfo CreateEnrichedMatchBase(MatchInfo match, string? cvUrl, string? userNum, string? userEmail, string? error)
        {
            var decodedUsername = HttpUtility.HtmlDecode(match.Username);
            var decodedDescUser = HttpUtility.HtmlDecode(match.DescriptionUser);
            return new EnrichedMatchInfo
            {
                IdMatch = match.IdMatch,
                Username = decodedUsername,
                //Href = match.Href,
                EmailUser = userEmail,
                IdDetail = match.IdDetail,
                PhoneNumber = userNum,
                userImageSrc = match.userImageSrc,
                IdVacancyFolder = match.IdVacancyFolder,
                DescriptionUser = decodedDescUser,
                CvUrl = cvUrl,
                Error = error
            };
        }

        // Métodos auxiliares
        private string ExtractCvGuid(string? cvUrl)
        {
            if (string.IsNullOrEmpty(cvUrl)) return "";
            return cvUrl.Split('?').Length > 1 ? cvUrl.Split('?')[1] : "";
        }

        private string ExtractImageGuid(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";
            return imageUrl.Split('/').Last().Split('_').First();
        }
    }
}
