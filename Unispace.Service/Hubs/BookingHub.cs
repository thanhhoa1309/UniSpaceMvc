using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace UniSpace.Service.Hubs
{
    public class BookingHub : Hub
    {
        private readonly ILogger<BookingHub> _logger;

        public BookingHub(ILogger<BookingHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation($"User {userId} connected to BookingHub");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation($"User {userId} disconnected from BookingHub");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Client can call this to join booking updates
        public async Task JoinBookingUpdates(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation($"Connection {Context.ConnectionId} joined user_{userId} group");
        }

        public async Task LeaveBookingUpdates(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation($"Connection {Context.ConnectionId} left user_{userId} group");
        }
    }

    // Extension methods for sending notifications
    public static class BookingHubExtensions
    {
        public static async Task SendBookingStatusUpdateAsync(
            this IHubContext<BookingHub> hubContext,
            string userId,
            Guid bookingId,
            string newStatus,
            string message)
        {
            await hubContext.Clients.Group($"user_{userId}").SendAsync(
                "BookingStatusUpdated",
                new
                {
                    bookingId,
                    status = newStatus,
                    message,
                    timestamp = DateTime.UtcNow
                });
        }

        public static async Task SendBookingCompletedAsync(
            this IHubContext<BookingHub> hubContext,
            string userId,
            Guid bookingId,
            string roomName,
            DateTime endTime)
        {
            await hubContext.Clients.Group($"user_{userId}").SendAsync(
                "BookingCompleted",
                new
                {
                    bookingId,
                    roomName,
                    endTime,
                    message = $"Your booking for '{roomName}' has been completed.",
                    timestamp = DateTime.UtcNow
                });
        }
    }
}
