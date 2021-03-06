using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RssNotify.Functions;
using RssNotify.Services;
using RssNotify.Services.Configuration;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace RssNotify.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            InjectKeyVaultIntoConfiguration(builder);

            builder.Services.AddOptions<FunctionConfig>()
                .Configure<IConfiguration>((funcConfig, configuration) => configuration.Bind(funcConfig));
            builder.Services.AddOptions<MatrixConfiguration>()
                .Configure<IConfiguration>((matrix, configuration) =>
                {
                    configuration.Bind("Matrix", matrix);
                    if (string.IsNullOrEmpty(matrix.AccessToken))
                        throw new NotSupportedException("Missing 'Matrix:AccessToken'");
                    if (string.IsNullOrEmpty(matrix.RoomId))
                        throw new NotSupportedException("Missing 'Matrix:RoomId'");
                    if (string.IsNullOrEmpty(matrix.TimeZone))
                        throw new NotSupportedException("Missing 'Matrix:Timezone'");
                    if (string.IsNullOrEmpty(matrix.TableName))
                        throw new NotSupportedException("Missing 'Matrix:TableName'");
                    // otherwise will spam with all old content
                    // on first launch this will still spam but then persist in table and on subsequent runs only new subscriptions are added
                    matrix.IgnoreSubscriptionsOlderThan = DateTimeOffset.UtcNow.Date.AddDays(-3);
                });
            builder.Services.AddSingleton<ISubscriptionProvider, SubscriptionProvider>();

            builder.Services.AddSingleton(p =>
            {
                var connectionString = p.GetRequiredService<IOptions<FunctionConfig>>().Value.AzureWebJobsStorage;
                return CloudStorageAccount.Parse(connectionString);
            });
            // singleton to prevent connection exhaustion
            builder.Services.AddSingleton<IHttpClient, CustomHttpClient>();
            builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
            builder.Services.AddSingleton<IMatrixNotificationService, MatrixNotificationService>();
        }

        private void InjectKeyVaultIntoConfiguration(IFunctionsHostBuilder builder)
        {
            // https://stackoverflow.com/a/60349484
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configurationRoot = serviceProvider.GetService<IConfiguration>();
            var configurationBuilder = new ConfigurationBuilder();

            if (configurationRoot is IConfigurationRoot)
            {
                configurationBuilder.AddConfiguration(configurationRoot);
            }

            var tokenProvider = new AzureServiceTokenProvider();
            var kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
            var keyVaultName = configurationRoot["KeyVaultName"];
            if (string.IsNullOrEmpty(keyVaultName))
                throw new NotSupportedException("KeyVaultName must be set as environment variable");

            configurationBuilder.AddAzureKeyVault($"https://{keyVaultName}.vault.azure.net", kvClient, new DefaultKeyVaultSecretManager());

            var configuration = configurationBuilder.Build();

            builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), configuration));
        }
    }
}
