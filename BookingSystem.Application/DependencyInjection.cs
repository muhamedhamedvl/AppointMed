using BookingSystem.Application.Behaviors;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BookingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register AutoMapper
        services.AddAutoMapper(assembly);

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Register Application services (business logic layer)
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IReviewService, ReviewService>();

        return services;
    }
}
