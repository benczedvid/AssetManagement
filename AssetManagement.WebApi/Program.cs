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

builder.Services.AddHealthChecks();

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
builder.Services.AddOptions<StartOfDayAssetReportOptions>()
                .Bind(builder.Configuration.GetSection(StartOfDayAssetReportOptions.SectionName))
                .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.CronExpression),
                    "StartOfDayAssetReport:CronExpression is required when the report is enabled.")
                .ValidateOnStart();
builder.Services.AddOptions<EndOfDayAssetReportOptions>()
                .Bind(builder.Configuration.GetSection(EndOfDayAssetReportOptions.SectionName))
                .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.CronExpression),
                    "EndOfDayAssetReport:CronExpression is required when the report is enabled.")
                .ValidateOnStart();
builder.Services.AddOptions<OverdueAssetReportOptions>().Bind(builder.Configuration.GetSection(OverdueAssetReportOptions.SectionName))
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.CronExpression),
        "OverdueAssetReport:CronExpression is required when the report is enabled.")
    .Validate(options => !options.Enabled || options.MinimumUnreturnedDays > 0,
        "OverdueAssetReport:MinimumUnreturnedDays must be greater than zero.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.TimeZoneId),
        "OverdueAssetReport:TimeZoneId is required when the report is enabled.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Subject),
        "OverdueAssetReport:Subject is required when the report is enabled.")
    .Validate(options => !options.Enabled || !options.TestMode || !string.IsNullOrWhiteSpace(options.TestRecipientAddress),
        "OverdueAssetReport:TestRecipientAddress is required when test mode is enabled.")
    .Validate(options => !options.Enabled || options.TestMode || options.RecipientAddresses is
            {
                Length: > 0
            },
        "OverdueAssetReport:RecipientAddresses must contain at least one address when the report is enabled outside test mode.")
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
var startOfDayReportOptions = builder.Configuration.GetSection(StartOfDayAssetReportOptions.SectionName).Get<StartOfDayAssetReportOptions>();
var endOfDayReportOptions = builder.Configuration.GetSection(EndOfDayAssetReportOptions.SectionName).Get<EndOfDayAssetReportOptions>();
var overdueAssetReportOptions = builder.Configuration.GetSection(OverdueAssetReportOptions.SectionName).Get<OverdueAssetReportOptions>();
var isStartOfDayReportEnabled = startOfDayReportOptions?.Enabled == true;
var isEndOfDayReportEnabled = endOfDayReportOptions?.Enabled == true;
var isOverdueAssetReportEnabled = overdueAssetReportOptions?.Enabled == true;

if (isStartOfDayReportEnabled || isEndOfDayReportEnabled || isOverdueAssetReportEnabled)
{
    builder.Services.AddQuartz(quartz =>
        {
            if (isStartOfDayReportEnabled)
            {
                var startOfDayJobKey = new JobKey("start-of-day-asset-report-job");

                quartz.AddJob<StartOfDayAssetReportJob>(options => options.WithIdentity(startOfDayJobKey));
                quartz.AddTrigger(trigger => trigger
                    .ForJob(startOfDayJobKey)
                    .WithIdentity("start-of-day-asset-report-trigger")
                    .WithCronSchedule(startOfDayReportOptions!.CronExpression,
                        schedule => schedule
                            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(startOfDayReportOptions.TimeZoneId))
                            .WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed)));
            }
            if (isEndOfDayReportEnabled)
            {
                var endOfDayJobKey = new JobKey("end-of-day-asset-report-job");

                quartz.AddJob<EndOfDayAssetReportJob>(options => options.WithIdentity(endOfDayJobKey));
                quartz.AddTrigger(trigger => trigger
                    .ForJob(endOfDayJobKey)
                    .WithIdentity("end-of-day-asset-report-trigger")
                    .WithCronSchedule(endOfDayReportOptions!.CronExpression,
                        schedule => schedule
                            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(endOfDayReportOptions.TimeZoneId))
                            .WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed)));
            }

            if (isOverdueAssetReportEnabled)
            {
                var overdueAssetJobKey = new JobKey("overdue-asset-report-job");

                quartz.AddJob<OverdueAssetReportJob>(options =>options.WithIdentity(overdueAssetJobKey));
                quartz.AddTrigger(trigger => trigger
                    .ForJob(overdueAssetJobKey)
                    .WithIdentity("overdue-asset-report-trigger")
                    .WithCronSchedule(overdueAssetReportOptions!.CronExpression,
                        schedule => schedule
                            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(overdueAssetReportOptions.TimeZoneId))
                            .WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed)));
            }
        });

    builder.Services.AddQuartzHostedService(
        options =>
        {
            options.WaitForJobsToComplete = true;
        });
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "AssetManagement API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle ="AssetManagement API";
    });
    app.UseHttpsRedirection();
    app.UseCors(AngularCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();

public partial class Program;