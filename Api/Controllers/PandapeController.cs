using Api.Services;
using Domain.Entities;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using Polly;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using System.Xml;
using static Application.DTOs.ModelsDataPandape;
namespace Api.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class PandapeController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPlaywrightService _playwrightService;
        private readonly IPandapeApiService _pandapeApiService;
        private readonly DownloadMultipleCV _downloadMultipleCVService;
        private readonly PDFDownloadZipService _pdfDownloadService;

        public PandapeController(
            IHttpClientFactory httpClientFactory,
            IPlaywrightService playwrightService,
            IPandapeApiService pandapeApiService,
            DownloadMultipleCV downloadMultipleCVService,
            PDFDownloadZipService pdfDownloadService)
        {
            _httpClientFactory = httpClientFactory;
            _playwrightService = playwrightService;
            _pandapeApiService = pandapeApiService;
            _downloadMultipleCVService = downloadMultipleCVService;
            _pdfDownloadService = pdfDownloadService;
        }

        /// <summary>
        /// Genera la cadena de texto cookie para poder consultar la información (cookieString)
        /// </summary>
        [HttpPost("loginUserPandape")]
        public async Task<IActionResult> LoginUserPandape([FromBody] LoginRequest request)
        {
            try
            {
                // Validación de entrada
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Paso 1: Autenticación y obtención de cookies
                var authResult = await _playwrightService.AuthenticateAsync(request);
                if (!authResult.Success)
                {
                    return BadRequest(authResult);
                }

                var cookieString = string.Join("; ", authResult.Cookies.Select(c => $"{Uri.EscapeDataString(c.Name)}={Uri.EscapeDataString(c.Value)}"));

                return Ok(cookieString);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name
                });
            }
        }


        /// <summary>
        /// Obtiene la lista de personas postuladas a la vacante junto a la url de su CV
        /// </summary>
        /// <remarks>
        /// Los parámetros son :
        /// 
        /// "pageNumber": El número de página para la paginación,
        /// 
        /// "pageSize": La cantidad de usuarios que debe traer,
        /// 
        /// "idVacancy": el identificador de la vacante que está en la url de Pandapé,
        /// 
        /// "idVacancyFolder": el identificador de la vacante que está en la url de Pandapé que está después de "idvacancyfolder=",
        /// 
        /// "cookieString": "La cadena de texto generada en el servicio /loginUserPandape"
        /// 
        /// 
        /// </remarks>
        [HttpPost("getListPersons")]
        public async Task<IActionResult> GetListPerson([FromBody] DetailFolderRequest request)
        {
            try
            {
                List <MatchInfo> matchesList = new List<MatchInfo>();
                MatchesApiResult dtaExt = new MatchesApiResult();
                foreach (var folderId in request.IdVacancyFolder)
                {
                    DetailRequest detailRequestUni = new DetailRequest
                    {
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize,
                        IdVacancy = request.IdVacancy,
                        IdVacancyFolder = folderId,
                        CookieString = request.CookieString
                    };

                    var matchesResult = await _pandapeApiService.GetMatchesAsync(detailRequestUni);

                    if (matchesResult.Success)
                    {
                        matchesList.AddRange(matchesResult.Matches);
                        dtaExt = matchesResult;
                    }
                    else
                    {
                        return StatusCode(500, new
                        {
                            Success = false,
                            Error = matchesResult.Error
                        });
                    }
                }

                if (dtaExt.Success)
                {
                    var enrichedMatches = await _downloadMultipleCVService.ProcessMatchesAsync(matchesList, request.CookieString, request.IdVacancy);
                    return Ok(enrichedMatches);
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Error = dtaExt.Error
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name
                });
            }
        }


        /// <summary>
        /// Obtiene la lista de personas postuladas a la vacante junto a la url de su CV
        /// </summary>
        /// <remarks>
        /// Los parámetros son :
        /// 
        /// "cookieString": "La cadena de texto generada en el servicio /loginUserPandape"
        /// 
        /// 
        /// </remarks>
        [HttpPost("getListProcess")]
        public async Task<IActionResult> GetListProcess([FromBody] DetailRequest request)
        {
            try
            {
                
                //var enrichedMatches = await _downloadMultipleCVService.GetVacanciesHtmlAsync(request);
                var (processList, subtotal) = await _downloadMultipleCVService.GetVacanciesHtmlAsync(request);

                if (processList == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Error = "No se pudieron obtener las vacantes."
                    });
                }

                //List<MatchInfo> matchesList = new List<MatchInfo>();
                //MatchesApiResult dtaExt = new MatchesApiResult();

                // recorre cada vacante
                //foreach (var vacancy in processList)
                //{
                //    int numVacancy = int.Parse(vacancy.VacancyId);

                //    // recorre cada etapa de la vacante
                //    foreach (var item in vacancy.Urls)
                //    {
                //        string idVacancyFolderStr = item.Url.Split("=").Last();
                //        int idVacancyFolder = int.Parse(idVacancyFolderStr);
                //        int counterNumVacancy = int.Parse(item.Count);
                //        int idVacancy = int.Parse(vacancy.VacancyId);

                //        DetailRequest detailRequestUni = new DetailRequest
                //        {
                //            PageNumber = 1,
                //            PageSize = counterNumVacancy,
                //            IdVacancy = idVacancy,
                //            IdVacancyFolder = idVacancyFolder,
                //            CookieString = request.CookieString
                //        };

                //        var matchesResult = await _pandapeApiService.GetMatchesAsync(detailRequestUni);

                //        if (matchesResult.Success)
                //        {
                //            matchesList.AddRange(matchesResult.Matches);
                //            dtaExt = matchesResult;
                //        }
                //        else
                //        {
                //            return StatusCode(500, new
                //            {
                //                Success = false,
                //                Error = matchesResult.Error
                //            });
                //        }
                //    }

                //    if (dtaExt.Success)
                //    {
                //        var enrichedMatches = await _downloadMultipleCVService.ProcessMatchesAsync(matchesList, request.CookieString, numVacancy);
                //        //return Ok(enrichedMatches);
                //    }
                //    else
                //    {
                //        return StatusCode(500, new
                //        {
                //            Success = false,
                //            Error = dtaExt.Error
                //        });
                //    }
                //};


                //return Ok(processList);
                return Ok(new
                {
                    data = processList,
                    subTotal = subtotal
                });
               
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name
                });
            }
        }


        /// <summary>
        /// Genera un zip con las CV de la lista
        /// </summary>
        /// <remarks>
        /// Los parámetros son :
        /// 
        /// "matchesUser": Es la lista que retorna el servicio "/getListPersons",
        /// 
        /// "cookieString": "La cadena de texto generada en el servicio /loginUserPandape"
        /// 
        /// </remarks>
        [HttpPost("DownloadCvPerson")]
        public async Task<IActionResult> GetZipPersonCv([FromBody] BodyCvCookie request)
        {
            var zipResult = await _pdfDownloadService.DownloadPDFsAsZipAsync(request);

            if (!zipResult.Success)
            {
                return BadRequest(new { Error = zipResult.Error });
            }

            // Retornar el ZIP como archivo
            return File(
                zipResult.ZipFileBytes,
                "application/zip",
                zipResult.ZipFileName);
        }
    }

    // Servicio para operaciones con Playwright
    public interface IPlaywrightService
    {
        Task<AuthenticationResult> AuthenticateAsync(LoginRequest request);
    }

    public class PlaywrightService : IPlaywrightService
    {
        private static readonly string[] UsernameSelectors = { "#Username", "input[name='Username']", "input[type='email']" };
        private static readonly string[] PasswordSelectors = { "#Password", "input[name='Password']", "input[type='password']" };
        private static readonly string[] LoginButtonSelectors = { "#btLogin", "button[type='submit']", "input[type='submit']" };

        public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request)
        {
            IBrowser browser = null;
            IPage page = null;

            try
            {
                using var playwright = await Playwright.CreateAsync();
                browser = await playwright.Chromium.LaunchAsync(CreateBrowserOptions());

                var context = await browser.NewContextAsync(CreateContextOptions());
                page = await context.NewPageAsync();

                // Navegar a login
                await page.GotoAsync("https://login.pandape.com/Account/Login", new PageGotoOptions
                {
                    Timeout = 60000,
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                await Task.Delay(2000);

                // Buscar y llenar formulario
                var formElements = await FindFormElementsAsync(page);
                if (!formElements.IsValid)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Elementos de login no encontrados",
                        Debug = formElements.Debug
                    };
                }

                await FillLoginFormAsync(formElements, request);

                // Hacer login
                var navigationTask = page.WaitForNavigationAsync(new PageWaitForNavigationOptions
                {
                    Timeout = 30000,
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                await formElements.LoginButton.ClickAsync();

                try
                {
                    await navigationTask;
                }
                catch (TimeoutException)
                {
                    // Continuar verificando el estado actual
                }

                // Verificar login exitoso
                if (IsLoginFailed(page.Url))
                {
                    var errorText = await ExtractErrorMessageAsync(page);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Login fallido",
                        ErrorMessage = errorText
                    };
                }

                // Extraer cookies relevantes
                var cookies = await ExtractRelevantCookiesAsync(context);

                return new AuthenticationResult
                {
                    Success = true,
                    Cookies = cookies
                };
            }
            finally
            {
                await CleanupAsync(page, browser);
            }
        }


        // Métodos auxiliares privados
        private static BrowserTypeLaunchOptions CreateBrowserOptions()
        {
            return new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--disable-web-security"
                }
            };
        }

        private static BrowserNewContextOptions CreateContextOptions()
        {
            return new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "es-ES,es;q=0.9,en;q=0.8",
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"
                }
            };
        }

        private async Task<FormElements> FindFormElementsAsync(IPage page)
        {
            var usernameField = await FindElementBySelectorsAsync(page, UsernameSelectors);
            var passwordField = await FindElementBySelectorsAsync(page, PasswordSelectors);
            var loginButton = await FindElementBySelectorsAsync(page, LoginButtonSelectors);

            var isValid = usernameField != null && passwordField != null && loginButton != null;

            return new FormElements
            {
                UsernameField = usernameField,
                PasswordField = passwordField,
                LoginButton = loginButton,
                IsValid = isValid,
                Debug = isValid ? null : await CreateDebugInfoAsync(page)
            };
        }

        private async Task<IElementHandle?> FindElementBySelectorsAsync(IPage page, string[] selectors)
        {
            foreach (var selector in selectors)
            {
                try
                {
                    await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 3000 });
                    var element = await page.QuerySelectorAsync(selector);
                    if (element != null) return element;
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        private async Task FillLoginFormAsync(FormElements elements, LoginRequest request)
        {
            await elements.UsernameField.FillAsync(request.Username);
            await Task.Delay(500);
            await elements.PasswordField.FillAsync(request.Password);
            await Task.Delay(500);
        }

        private static bool IsLoginFailed(string currentUrl)
        {
            return currentUrl.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
                   currentUrl.Contains("Error", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> ExtractErrorMessageAsync(IPage page)
        {
            var errorSelectors = new[] { ".error", ".validation-summary-errors", ".field-validation-error", ".alert-danger" };

            foreach (var selector in errorSelectors)
            {
                try
                {
                    var errorElement = await page.QuerySelectorAsync(selector);
                    if (errorElement != null)
                    {
                        var text = await errorElement.TextContentAsync();
                        if (!string.IsNullOrEmpty(text))
                            return text.Trim();
                    }
                }
                catch { continue; }
            }

            return "Error de login desconocido";
        }

        private async Task<List<CookieInfo>> ExtractRelevantCookiesAsync(IBrowserContext context)
        {
            var cookies = await context.CookiesAsync();
            var relevantCookieNames = new HashSet<string>
            {
                "ATSCultureCookie", "ats-webui", ".AspNetCore.Antiforgery.MzFEACH9dlA",
                "allowcookies", "AWSALB", "AWSALBCORS"
            };

            return cookies
                .Where(c => relevantCookieNames.Contains(c.Name))
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .Select(c => new CookieInfo
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    HttpOnly = c.HttpOnly,
                    Secure = c.Secure
                })
                .ToList();
        }

        private async Task<object> CreateDebugInfoAsync(IPage page)
        {
            var allInputs = await page.QuerySelectorAllAsync("input");
            var allButtons = await page.QuerySelectorAllAsync("button");

            return new
            {
                PageTitle = await page.TitleAsync(),
                Url = page.Url,
                InputsFound = allInputs.Count,
                ButtonsFound = allButtons.Count
            };
        }

        private static async Task CleanupAsync(IPage? page, IBrowser? browser)
        {
            try
            {
                if (page != null) await page.CloseAsync();
                if (browser != null) await browser.CloseAsync();
            }
            catch
            {
                // Ignorar errores de cleanup
            }
        }
    }

    // Servicio para operaciones con API directa
    public interface IPandapeApiService
    {
        Task<MatchesApiResult> GetMatchesAsync(DetailRequest request);
    }

    public class PandapeApiService : IPandapeApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PandapeApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private string ExtractIdMatchFromHref(string href)
        {
            try
            {
                // Si la URL es relativa, agregarle un dominio temporal para parsearla
                var uriString = href.StartsWith("/") ? $"https://example.com{href}" : href;

                if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                {
                    // Parsear los query parameters
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    return query["idMatch"];
                }

                // Si no se puede parsear como URI, buscar manualmente
                var idMatchIndex = href.IndexOf("idMatch=", StringComparison.OrdinalIgnoreCase);
                if (idMatchIndex >= 0)
                {
                    var startIndex = idMatchIndex + "idMatch=".Length;
                    var endIndex = href.IndexOf('&', startIndex);
                    if (endIndex == -1)
                        endIndex = href.Length;

                    return href.Substring(startIndex, endIndex - startIndex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extrayendo idMatch de '{href}': {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Obtiene el nombre de las personas con su imagen y su identificador
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        private List<MatchInfo> ExtractMatchesWithSpecificSelectors(string htmlContent, DetailRequest request)
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
                        var idMatch = ExtractIdMatchFromHref(href);
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
                            var idMatch = ExtractIdMatchFromHref(href);
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

        public async Task<MatchesApiResult> GetMatchesAsync(DetailRequest request)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();

                // Configurar headers y cookies
                ConfigureHttpClient(client, request.CookieString);

                // Crear form-data content
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("PageNumber", (request.PageNumber).ToString()),
                    new("PageSize", (request.PageSize).ToString()),
                    new("IdVacancy", (request.IdVacancy).ToString()),
                    //new("IdVacancyFolder", (request.IdVacancyFolder).ToString())
                };

                var content = new FormUrlEncodedContent(formData);

                // Realizar petición
                var response = await client.PostAsync("https://ats.pandape.com/Company/Match/ListMatches", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var matchesResponse = JsonSerializer.Deserialize<MatchesResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        var dataList = ExtractMatchesWithSpecificSelectors(matchesResponse.ViewList, request);
                        return new MatchesApiResult
                        {
                            Success = true,
                            Matches = dataList
                        };
                    }
                    catch (JsonException jsonEx)
                    {
                        return new MatchesApiResult
                        {
                            Success = false,
                            Error = $"Error deserializando respuesta: {jsonEx.Message}"
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new MatchesApiResult
                {
                    Success = false,
                    Error = $"API returned {response.StatusCode}: {errorContent}"
                };
            }
            catch (HttpRequestException httpEx)
            {
                return new MatchesApiResult
                {
                    Success = false,
                    Error = $"Error de conexión: {httpEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new MatchesApiResult
                {
                    Success = false,
                    Error = $"Error inesperado: {ex.Message}"
                };
            }
        }

        // Método auxiliar para configurar HttpClient (si no lo tienes ya)
        private static void ConfigureHttpClient(HttpClient client, string cookieString)
        {
            // Limpiar headers previos
            client.DefaultRequestHeaders.Clear();

            // Configurar cookies
            //if (cookies?.Any() == true)
            //{
            //    var cookieString = string.Join("; ", cookies.Select(c =>
            //        $"{Uri.EscapeDataString(c.Name)}={Uri.EscapeDataString(c.Value)}"));

            //    client.DefaultRequestHeaders.Add("Cookie", cookieString);
            //}
            if (cookieString != "")
            {

                client.DefaultRequestHeaders.Add("Cookie", cookieString);
            }


            // Configurar headers estándar
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9,en;q=0.8");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

    }

    // Clases auxiliares
    public static class UrlBuilder
    {
        public static string BuildMatchesUrl(DetailRequest request)
        {
            var parameters = new Dictionary<string, string>
            {
                ["idvacancyfolder"] = (request.IdVacancyFolder).ToString(),
                ["matchesfilter.idvacancy"] = (request.IdVacancy).ToString(),
                ["matchesfilter.idvacancyfolder"] = (request.IdVacancyFolder).ToString(),
                ["matchesfilter.pagenumber"] = (request.PageNumber).ToString(),
                ["matchesfilter.pagesize"] = (request.PageSize).ToString(),
                ["matchesfilter.loadall"] = "false",
                ["matchesfilter.order"] = "0"
            };

            var queryString = string.Join("&", parameters.Select(p =>
                $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

            return $"https://ats.pandape.com/Company/Match/Matches/{request.IdVacancy ?? 10473535}?{queryString}";
        }
    }

    public class FormElements
    {
        public IElementHandle? UsernameField { get; set; }
        public IElementHandle? PasswordField { get; set; }
        public IElementHandle? LoginButton { get; set; }
        public bool IsValid { get; set; }
        public object? Debug { get; set; }
    }
}
