namespace Application.DTOs
{
    public class ModelsDataPandape
    {
        public class MatchInfo
        {
            public string IdMatch { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string? userImageSrc { get; set; }
            public int IdDetail { get; set; }
            public string? EmailUser { get; set; }
            public string? DescriptionUser { get; set; }
            public int IdVacancyFolder { get; set; }
            //public string Href { get; set; } = string.Empty;
            //public string FullUrl { get; set; } = string.Empty;
        }

        public class EnrichedMatchInfo : MatchInfo
        {
            public string? CvUrl { get; set; }
            public string? Error { get; set; }
            public string? userImageSrc { get; set; }
            public string? PhoneNumber { get; set; }
        }

        public class BodyCvCookie
        {
            public List<EnrichedMatchInfo> MatchesUser { get; set; }
            public string CookieString { get; set; }
        }

        // Modelos de datos
        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class DetailRequest
        {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int? IdVacancy { get; set; }
            public int IdVacancyFolder { get; set; }
            public string CookieString { get; set; }
        }

        public class DetailFolderRequest
        {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int IdVacancy { get; set; }
            public int[] IdVacancyFolder { get; set; }
            public string CookieString { get; set; }
        }

        public class CookieInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
            public string? Path { get; set; }
            public bool HttpOnly { get; set; }
            public bool Secure { get; set; }
        }

        public class AuthenticationResult
        {
            public bool Success { get; set; }
            public List<CookieInfo> Cookies { get; set; } = new();
            public string? Error { get; set; }
            public string? ErrorMessage { get; set; }
            public object? Debug { get; set; }
        }

        public class ScrapingResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public object? Data { get; set; }
            public string? Error { get; set; }
        }

        public class VacancyInfo
        {
            public string? VacancyId { get; set; }
            public string? NameProcess { get; set; }
            public string? Location { get; set; }
            public string? CreatedBy { get; set; }
            public int CounterNumVacancy { get; set; }
            public string? StatusProcess { get; set; }
            public List<VacancyUrlInfo>? Urls { get; set; }
        }

        public class VacancyUrlInfo
        {
            public string? Url { get; set; }
            public string? Category { get; set; }
            public string? Count { get; set; }
        }

        public class MatchesApiResult
        {
            public bool Success { get; set; }
            public List<MatchInfo> Matches { get; set; } = new();
            public string? Error { get; set; }
        }

        public class MatchesResponse
        {
            public List<MatchInfo> Matches { get; set; } = new();
            public int Total { get; set; }
            public string Url { get; set; } = string.Empty;
            public string ViewList { get; set; } = string.Empty;
        }

        public class PaginatedResult<T>
        {
            public IEnumerable<T> Data { get; set; } = new List<T>();
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalRecords { get; set; }
            public int TotalPages { get; set; }
            public bool HasPrevious => PageNumber > 1;
            public bool HasNext => PageNumber < TotalPages;
        }

        public class CandidatesGroupsDto
        {
            public int IdVacancy { get; set; }
            public List<MatchInfo> Candidates { get; set; }
        }
    }
}
