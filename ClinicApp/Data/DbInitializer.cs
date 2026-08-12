using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicApp.Data;

public static class DbInitializer
{
    public static async Task SeedDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply migrations if pending
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            await db.Database.MigrateAsync();
        }

        // 1. Seed Roles
        string[] roles = { "Admin", "Client" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Admin User
        var adminEmail = "admin@clinic.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Dr. Anita Sharma (Clinic Director)",
                PhoneNumber = "+919876543210"
            };
            var result = await userManager.CreateAsync(adminUser, "AdminPass123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Seed Client Users
        var clientsData = new List<(string Email, string Name, string Phone, double StartW, double TargetW, double Height)>
        {
            ("sarah@gmail.com", "Sarah Jenkins", "+919812345678", 78.5, 65.0, 164.0),
            ("david@gmail.com", "David Miller", "+919823456789", 96.2, 85.0, 180.0),
            ("emma@gmail.com", "Emma Watson", "+919834567890", 63.8, 55.0, 168.0)
        };

        foreach (var c in clientsData)
        {
            var user = await userManager.FindByEmailAsync(c.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = c.Email,
                    Email = c.Email,
                    EmailConfirmed = true,
                    FullName = c.Name,
                    PhoneNumber = c.Phone
                };
                var createResult = await userManager.CreateAsync(user, "ClientPass123!");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Client");
                }
            }

            // Ensure ClientProfile exists
            var profile = await db.ClientProfiles.FirstOrDefaultAsync(cp => cp.UserId == user.Id);
            if (profile == null)
            {
                profile = new ClientProfile
                {
                    UserId = user.Id,
                    StartWeight = c.StartW,
                    TargetWeight = c.TargetW,
                    Height = c.Height,
                    DoctorNotes = $"Prefers low carb diet. Target loss target established. Monitored for treatment: {(c.StartW > 80 ? "Physio and Slimming" : "Slimming Focus")}.",
                    IsActive = true,
                    JoiningDate = DateTime.UtcNow.AddDays(-30)
                };
                db.ClientProfiles.Add(profile);
                await db.SaveChangesAsync();
            }

            // Seed Weight & Food Logs for last 5 days
            var logsExist = await db.DailyLogs.AnyAsync(l => l.UserId == user.Id);
            if (!logsExist)
            {
                var rand = new Random();
                for (int i = 5; i >= 1; i--)
                {
                    var logDate = DateTime.Today.AddDays(-i);
                    // Gradual weight decrease simulation
                    double weightOffset = (5 - i) * 0.3 + (rand.NextDouble() * 0.1);
                    double weightVal = c.StartW - weightOffset;

                    var carb = rand.Next(25, 45);
                    var prot = rand.Next(30, 45);
                    var fat = rand.Next(15, 25);
                    var veg = 100 - (carb + prot + fat);

                    db.DailyLogs.Add(new DailyLog
                    {
                        UserId = user.Id,
                        LogDate = logDate,
                        Weight = Math.Round(weightVal, 1),
                        DietNotes = i switch
                        {
                            5 => "High protein breakfast, vegetable salad for lunch. Missed evening walk.",
                            4 => "Followed diet plan strictly. Felt highly energetic during therapy.",
                            3 => "Cheat snack in afternoon, compensated with light vegetable soup dinner.",
                            2 => "Took slimming detox tea. Protein shake and grilled chicken.",
                            _ => "Standard diet chart followed. Drank 3L water today."
                        },
                        CarbsPercentage = carb,
                        ProteinPercentage = prot,
                        FatPercentage = fat,
                        VegPercentage = veg,
                        MealPhotoUrl = "", // Empty indicates default placeholder
                        LoggedAt = logDate.AddHours(20)
                    });
                }
                await db.SaveChangesAsync();
            }

            // Seed Attendance Record
            var attendanceExist = await db.AttendanceRecords.AnyAsync(ar => ar.UserId == user.Id);
            if (!attendanceExist)
            {
                // Completed past visit
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    UserId = user.Id,
                    CheckInTime = DateTime.UtcNow.AddDays(-3).AddHours(-2),
                    CheckOutTime = DateTime.UtcNow.AddDays(-3).AddHours(-1),
                    TreatmentReceived = c.StartW > 80 ? "Physiotherapy Knee Therapy" : "G5 Slimming Massage",
                    Therapist = "Dr. Sameer (Physiotherapist)",
                    Notes = "Client reported slight soreness, but overall muscle stiffness is reduced.",
                    Status = "Completed"
                });

                // Active check-in for one client (David)
                if (c.Name.Contains("David"))
                {
                    db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        UserId = user.Id,
                        CheckInTime = DateTime.UtcNow.AddMinutes(-30),
                        TreatmentReceived = "Laser Fat Reduction Therapy",
                        Therapist = "Dr. Anita Sharma",
                        Notes = "Treatment session started. Laser parameters set to default standard.",
                        Status = "Checked In"
                    });
                }
                await db.SaveChangesAsync();
            }

            // Seed Appointments
            var apptsExist = await db.Appointments.AnyAsync(ap => ap.UserId == user.Id);
            if (!apptsExist)
            {
                // Past completed appt
                db.Appointments.Add(new Appointment
                {
                    UserId = user.Id,
                    AppointmentDate = DateTime.UtcNow.AddDays(-3),
                    TreatmentType = c.StartW > 80 ? "Physio" : "Slimming",
                    Status = "Completed",
                    Notes = "Done, attendance logged."
                });

                // Upcoming appointment
                db.Appointments.Add(new Appointment
                {
                    UserId = user.Id,
                    AppointmentDate = DateTime.Today.AddDays(1).AddHours(10 + (c.Name.Length % 4)),
                    TreatmentType = c.StartW > 80 ? "Physio" : "Slimming",
                    Status = "Scheduled",
                    Notes = "Follow-up consultation and treatment session."
                });
                await db.SaveChangesAsync();
            }

            // Seed Invoices/Payment notifications
            var paymentsExist = await db.PaymentNotifications.AnyAsync(pn => pn.UserId == user.Id);
            if (!paymentsExist)
            {
                // Completed payment
                db.PaymentNotifications.Add(new PaymentNotification
                {
                    UserId = user.Id,
                    Amount = 1500,
                    BillingDate = DateTime.UtcNow.AddDays(-15),
                    Description = "Slimming & Therapy 10-Session Package Enrolment",
                    IsPaid = true
                });

                // Unpaid invoice
                db.PaymentNotifications.Add(new PaymentNotification
                {
                    UserId = user.Id,
                    Amount = 250,
                    BillingDate = DateTime.UtcNow.AddDays(-2),
                    Description = "Single Physio Session & Detox Kit Supplement",
                    IsPaid = false,
                    IsTriggered = false
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
