using System;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Api.Services
{
    public class LdapService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LdapService> _logger;
        private readonly string _ldapServer;
        private readonly int _ldapPort;
        private readonly string _requiredGroup;

        public LdapService(IConfiguration configuration, ILogger<LdapService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Obtener configuración LDAP
            _ldapServer = _configuration["Ldap:Server"] ?? "10.128.50.2";
            _ldapPort = int.Parse(_configuration["Ldap:Port"] ?? "389");
            _requiredGroup = _configuration["Ldap:RequiredGroup"] ?? "herramientas_facturacion_user";
            
            _logger.LogInformation($"Servicio LDAP configurado con servidor: {_ldapServer}:{_ldapPort}");
        }

        public async Task<(bool Success, string Message)> AuthenticateAsync(string username, string password)
        {
            return await Task.Run(() => Authenticate(username, password));
        }

        private (bool Success, string Message) Authenticate(string username, string password)
        {
            try
            {
                _logger.LogInformation($"Intentando autenticación LDAP para usuario: {username}");
                
                // Initialize a new instance of the DirectoryEntry class.
                string ldapPath = $"LDAP://{_ldapServer}:{_ldapPort}";
                using DirectoryEntry ldapConnection = new DirectoryEntry(ldapPath, username, password);

                // Create a DirectorySearcher object.
                using DirectorySearcher search = new DirectorySearcher(ldapConnection);
                search.Filter = $"(&(objectClass=user)(sAMAccountName={username}))";

                // Use the FindOne method to find the user object.
                SearchResult result = search.FindOne();

                // If the user is found, then they are authenticated.
                if (result != null)
                {
                    // Get the 'userAccountControl' property of the user
                    int userAccountControl = Convert.ToInt32(result.Properties["userAccountControl"][0]);
                    bool isAccountDisabled = (userAccountControl & 0x2) > 0; // The account is disabled if bit 1 is set

                    if (isAccountDisabled)
                    {
                        _logger.LogWarning($"Cuenta LDAP desactivada para usuario: {username}");
                        return (false, "DISABLEDACCOUNT");
                    }
                    else
                    {
                        // Check if the user is a member of the required group
                        foreach (string group in result.Properties["memberOf"])
                        {
                            if (group.Contains($"CN={_requiredGroup}"))
                            {
                                _logger.LogInformation($"Autenticación LDAP exitosa para usuario: {username}");
                                return (true, "OK");
                            }
                        }
                        _logger.LogWarning($"Usuario {username} no pertenece al grupo requerido: {_requiredGroup}");
                        return (false, "NOTAMEMBER");
                    }
                }
                else
                {
                    _logger.LogWarning($"Usuario no encontrado en LDAP: {username}");
                    return (false, "NOTFOUND");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en autenticación LDAP para usuario: {username}");
                return (false, "INVALIDCREDENTIALS");
            }
        }
    }
}