using Microsoft.AspNetCore.SignalR;

namespace QuanLyBanHang.Hubs;

public class OrderHub : Hub
{
    public async Task NotifyStatusChange(long orderId, string newStatus)
    {
        await Clients.All.SendAsync("OrderStatusChanged", orderId, newStatus);
    }
}
