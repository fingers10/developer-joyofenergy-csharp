using JOIEnergy.Domain;
using JOIEnergy.Enums;
using JOIEnergy.Generator;
using JOIEnergy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JOIEnergy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var readings =
                GenerateMeterElectricityReadings();

            var pricePlans = new List<PricePlan> {
                new PricePlan{
                    EnergySupplier = Enums.Supplier.DrEvilsDarkEnergy,
                    UnitRate = 10m,
                    PeakTimeMultiplier = new List<PeakTimeMultiplier>()
                },
                new PricePlan{
                    EnergySupplier = Enums.Supplier.TheGreenEco,
                    UnitRate = 2m,
                    PeakTimeMultiplier = new List<PeakTimeMultiplier>()
                },
                new PricePlan{
                    EnergySupplier = Enums.Supplier.PowerForEveryone,
                    UnitRate = 1m,
                    PeakTimeMultiplier = new List<PeakTimeMultiplier>()
                }
            };

            services.AddControllers();
            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<IMeterReadingService, MeterReadingService>();
            services.AddTransient<IPricePlanService, PricePlanService>();
            services.AddSingleton((IServiceProvider arg) => readings);
            services.AddSingleton((IServiceProvider arg) => pricePlans);
            services.AddSingleton((IServiceProvider arg) => SmartMeterToPricePlanAccounts);

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VV";
                //options.SubstituteApiVersionInUrl = true;
            });

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("api-version");
                //options.ApiVersionReader = new MediaTypeApiVersionReader();
            });

            IApiVersionDescriptionProvider apiVersionDescriptionProvider = GetApiVersionDescriptionProvider(services);

            services.AddSwaggerGen(options =>
            {
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(
                        $"JOIOpenAPISpecification{description.GroupName}",
                        new OpenApiInfo
                        {
                            Title = "JOI API",
                            Version = description.ApiVersion.ToString(),
                            Description = "Through this API you can access JOI Endpoints",
                            Contact = new OpenApiContact
                            {
                                Email = "abdulrahman.smsi@gmail.com",
                                Name = "Abdul Rahman",
                                Url = new Uri("https://www.linkedin.com/in/fingers10")
                            },
                            // Need to change the license in future
                            License = new OpenApiLicense
                            {
                                Name = "MIT License",
                                Url = new Uri("https://opensource.org/licenses/MIT")
                            },
                            //TermsOfService = new Uri("")
                        });
                }

                options.DocInclusionPredicate((documentName, apiDescription) =>
                {
                    var actionApiVersionModel = apiDescription.ActionDescriptor
                    .GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);

                    if (actionApiVersionModel == null)
                    {
                        return true;
                    }

                    if (actionApiVersionModel.DeclaredApiVersions.Count > 0)
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v =>
                        $"JOIOpenAPISpecificationv{v}" == documentName);
                    }

                    return actionApiVersionModel.ImplementedApiVersions.Any(v =>
                        $"JOIOpenAPISpecificationv{v}" == documentName);
                });

                var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);

                options.IncludeXmlComments(xmlCommentsFullPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IApiVersionDescriptionProvider apiVersionDescriptionProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection()
               .UseSwagger()
               .UseSwaggerUI(options =>
               {
                   foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                   {
                       options.SwaggerEndpoint(
                           $"swagger/JOIOpenAPISpecification{description.GroupName}/swagger.json",
                           $"JOI API - {description.GroupName.ToUpperInvariant()}");
                   }

                   options.OAuthClientId("gallaswaggerclient");
                   options.OAuthAppName("JOI API");
                   options.OAuthUsePkce();

                   options.RoutePrefix = string.Empty;
                   options.DefaultModelExpandDepth(2);
                   options.DefaultModelRendering(ModelRendering.Model);
                   options.DocExpansion(DocExpansion.None);
                   options.DisplayRequestDuration();
                   options.EnableValidator();
                   options.EnableFilter();
                   options.EnableDeepLinking();
                   options.DisplayOperationId();
               })
               .UseRouting()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapControllers();
               });
        }

        private Dictionary<string, List<ElectricityReading>> GenerateMeterElectricityReadings() {
            var readings = new Dictionary<string, List<ElectricityReading>>();
            var generator = new ElectricityReadingGenerator();
            var smartMeterIds = SmartMeterToPricePlanAccounts.Select(mtpp => mtpp.Key);

            foreach (var smartMeterId in smartMeterIds)
            {
                readings.Add(smartMeterId, generator.Generate(20));
            }
            return readings;
        }

        public Dictionary<String, Supplier> SmartMeterToPricePlanAccounts
        {
            get
            {
                Dictionary<String, Supplier> smartMeterToPricePlanAccounts = new Dictionary<string, Supplier>
                {
                    { "smart-meter-0", Supplier.DrEvilsDarkEnergy },
                    { "smart-meter-1", Supplier.TheGreenEco },
                    { "smart-meter-2", Supplier.DrEvilsDarkEnergy },
                    { "smart-meter-3", Supplier.PowerForEveryone },
                    { "smart-meter-4", Supplier.TheGreenEco }
                };
                return smartMeterToPricePlanAccounts;
            }
        }

        private static IApiVersionDescriptionProvider GetApiVersionDescriptionProvider(IServiceCollection services)
        {
            return services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();
        }
    }
}
