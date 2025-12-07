using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UniSpace.Bo.Enums;
using UniSpace.Service.Hubs;
using UniSpace.Service.Interfaces;

namespace UniSpace.Service.BackgroundServices
{
    public class BookingCompletionService : BackgroundService
    {
        private readonly ILogger<BookingCompletionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public BookingCompletionService(
            ILogger<BookingCompletionService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Completion Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CompleteExpiredBookingsAsync(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Booking Completion Service is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Booking Completion Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute on error
                }
            }

            _logger.LogInformation("Booking Completion Service has stopped.");
        }

        private async Task CompleteExpiredBookingsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<BookingHub>>();

            _logger.LogInformation("Checking for bookings to complete...");

            try
            {
                // Get all approved bookings
                var result = await bookingService.GetBookingsAsync(
                    pageNumber: 1,
                    pageSize: 1000, // Get many at once to reduce queries
                    status: BookingStatus.Approved
                );

                if (result == null || result.Count == 0)
                {
                    _logger.LogDebug("No approved bookings found to check.");
                    return;
                }

                var currentTime = DateTime.UtcNow;
                var completedCount = 0;

                foreach (var booking in result)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Check if booking has ended
                    if (booking.EndTime <= currentTime)
                    {
                        try
                        {
                            _logger.LogInformation(
                                $"Completing booking {booking.Id} for room '{booking.RoomName}' " +
                                $"(ended at {booking.EndTime:yyyy-MM-dd HH:mm})");

                            await bookingService.CompleteBookingAsync(booking.Id);
                            completedCount++;

                            _logger.LogInformation(
                                $"? Booking {booking.Id} completed successfully");

                            // Send SignalR notification to user
                            try
                            {
                                await hubContext.SendBookingCompletedAsync(
                                    booking.UserId.ToString(),
                                    booking.Id,
                                    booking.RoomName,
                                    booking.EndTime
                                );

                                _logger.LogInformation(
                                    $"? Sent completion notification to user {booking.UserId}");
                            }
                            catch (Exception notifEx)
                            {
                                _logger.LogWarning(notifEx,
                                    $"Failed to send notification for booking {booking.Id}");
                                // Continue even if notification fails
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                $"Failed to complete booking {booking.Id}");
                        }
                    }
                }

                if (completedCount > 0)
                {
                    _logger.LogInformation(
                        $"Completed {completedCount} expired booking(s)");
                }
                else
                {
                    _logger.LogDebug("No expired bookings to complete.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired bookings");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Booking Completion Service is stopping gracefully...");
            await base.StopAsync(cancellationToken);
        }
    }
}
