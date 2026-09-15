using AssetManagement.Application;
using AssetManagement.Application.AssetMovements.AssetMovementReports;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Infrastructure;
using AssetManagement.WebApi.Authentication;
using AssetManagement.WebApi.DependencyInjection;
using AssetManagement.WebApi.ExceptionHandling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;
using AssetManagement.WebApi.BackgroundJobs;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

const string AngularCorsPolicy = "AngularClient";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
    options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.MapInboundClaims = false;
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.TokenValidationParameters.NameClaimType = "name";
    },
    options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    }
    );
builder.Services.AddOptions<EndOfDayAssetReportOptions>()
                .Bind(builder.Configuration.GetSection(EndOfDayAssetReportOptions.SectionName))
                .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.CronExpression), "EndOfDayAssetReport:CronExpression is required when the report is enabled.")
                .Validate(options => !options.Enabled || options.RecipientAddressTemplate.Contains("{0}", StringComparison.Ordinal), "EndOfDayAssetReport: RecipientAddressTemplate must contain {0}.")
                .ValidateOnStart();
builder.Services.AddAssetManagementAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        AngularCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
var endOfDayReportSection = builder.Configuration.GetSection(EndOfDayAssetReportOptions.SectionName);
var endOfDayReportOptions = endOfDayReportSection.Get<EndOfDayAssetReportOptions>();
if (endOfDayReportOptions?.Enabled == true)
{
    var reportJobKey = new JobKey("end-of-day-asset-report-job");
    builder.Services.AddQuartz(
        quartz =>
        {
            quartz.AddJob<EndOfDayAssetReportJob>(options => options.WithIdentity(reportJobKey));
            quartz.AddTrigger(trigger => trigger
                .ForJob(reportJobKey)
                .WithIdentity("end-of-day-asset-report-trigger")
                .WithCronSchedule(endOfDayReportOptions.CronExpression, schedule => schedule.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(endOfDayReportOptions.TimeZoneId))
                .WithMisfireHandlingInstructionFireAndProceed()));
        });
    builder.Services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
    app.UseCors(AngularCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;