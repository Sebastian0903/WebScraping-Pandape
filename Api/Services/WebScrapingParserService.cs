using Domain.Repositories;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;
using static Application.DTOs.ModelsDataPandape;

namespace Api.Services
{
    public class WebScrapingParserService
    {

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
        /// Obtiene el nombre de las personas con su imagen y su identificador
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public List<MatchInfo> ExtractMatchesWithSpecificSelectors(string htmlContent, DetailRequest request)
        {
            var matches = new List<MatchInfo>();

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Selector más específico - ajusta según la estructura real de tu HTML
                var linkNodes = doc.DocumentNode
                    .SelectNodes("//a[contains(@href, 'idMatch=') and @data-username]");

                if (linkNodes != null)
                {
                    foreach (var linkNode in linkNodes)
                    {
                        var linkNodeImg = linkNode.SelectSingleNode(".//img[@class='img-avatar']");

                        var href = linkNode.GetAttributeValue("href", "");
                        var username = linkNode.GetAttributeValue("data-username", "");
                        var idVacancyFolderStrg = linkNode.GetAttributeValue("data-idvacancyfolder", "");
                        int idVacancyFolder = int.Parse(idVacancyFolderStrg);
                        var idMatch = linkNode.GetAttributeValue("data-idmatch", "");
                        var idDataUser = linkNode.GetAttributeValue("data-idcandidateresume", "");
                        int numIdDataUser = int.Parse(idDataUser);
                        string src = linkNodeImg?.GetAttributeValue("src", "");

                        var professionElement = linkNode.SelectSingleNode(".//div[contains(@class, 'mt-10') and contains(@class, 'c-drk')]//span");
                        var profession = professionElement?.InnerText?.Trim() ?? "";

                        // NUEVO: Extraer la fecha/hora
                        var dateTimeElement = linkNode.SelectSingleNode(".//div[contains(@class, 'flex-grow-1') and contains(@class, 'lh-120')]");
                        var dateTimeText = dateTimeElement?.InnerText?.Trim() ?? "";

                        // Alternativa: buscar el div que contiene el texto de fecha específico
                        if (string.IsNullOrEmpty(dateTimeText))
                        {
                            // Buscar en todos los divs con clase flex-grow-1 lh-120 dentro del contenedor de fecha
                            var dateContainer = linkNode.SelectSingleNode(".//div[contains(@class, 'd-flex') and contains(@class, 'gap-5') and contains(@class, 'c-drk')]//div[contains(@class, 'flex-grow-1') and contains(@class, 'lh-120')]");
                            dateTimeText = dateContainer?.InnerText?.Trim() ?? "";
                        }

                        if (!string.IsNullOrEmpty(idMatch))
                        {
                            matches.Add(new MatchInfo
                            {
                                IdMatch = idMatch,
                                IdDetail = numIdDataUser,
                                userImageSrc = src,
                                Username = username,
                                IdVacancyFolder = idVacancyFolder,
                                DescriptionUser = profession
                            });
                        }
                    }
                }

                // Si no encuentra con data-username, buscar sin ese atributo
                if (!matches.Any())
                {
                    var allLinkNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'idMatch=')]");

                    if (allLinkNodes != null)
                    {
                        foreach (var linkNode in allLinkNodes)
                        {
                            var href = linkNode.GetAttributeValue("href", "");
                            var idMatch = linkNode.GetAttributeValue("data-idmatch", "");
                            var idDataUser = linkNode.GetAttributeValue("data-idcandidateresume", "");

                            // Extraer fecha/hora también en el segundo intento
                            var dateTimeElement = linkNode.SelectSingleNode(".//div[contains(@class, 'flex-grow-1') and contains(@class, 'lh-120')]");
                            var dateTimeText = dateTimeElement?.InnerText?.Trim() ?? "";

                            if (!string.IsNullOrEmpty(idMatch))
                            {
                                matches.Add(new MatchInfo
                                {
                                    IdMatch = idMatch,
                                    IdDetail = int.Parse(idDataUser),
                                    Username = "", // Se puede llenar después si es necesario
                                    //DateTimeText = dateTimeText // NUEVA PROPIEDAD
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error con HTML Agility Pack (selectores específicos): {ex.Message}");
            }

            return matches;
        }
    }
}
