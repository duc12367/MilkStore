// FILE: wwwroot/js/notifications.js
// Kết nối SignalR NotificationHub, hiển thị toast thông báo realtime.
// Đọc userId từ <meta name="user-id"> được render bởi _Layout.cshtml

(function () {
    // Chỉ kết nối nếu user đã đăng nhập
    const meta = document.querySelector('meta[name="user-id"]');
    if (!meta || !meta.content) return;

    if (typeof signalR === 'undefined') return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on("ReceiveNotification", function (notif) {
        showNotifToast(notif.message, notif.type, notif.url);

        // Nếu là admin → cập nhật badge đơn mới
        const badge = document.getElementById('admin-new-order-badge');
        if (badge && notif.type === 'NewOrder') {
            const current = parseInt(badge.textContent || '0');
            badge.textContent = current + 1;
            badge.style.display = 'inline';
        }
    });

    connection.start().catch(err => console.warn('[NotificationHub]', err));

    function showNotifToast(message, type, url) {
        const colors = {
            Paid: '#00b894', Shipping: '#0984e3',
            Cancelled: '#e17055', NewOrder: '#6c5ce7'
        };
        const bg = colors[type] || '#2d3436';

        const toast = document.createElement('div');
        toast.className = 'ms-toast show';
        toast.style.background = bg;
        toast.textContent = message;
        if (url) {
            toast.style.cursor = 'pointer';
            toast.onclick = () => window.location.href = url;
        }
        document.body.appendChild(toast);
        setTimeout(() => { toast.classList.remove('show'); setTimeout(() => toast.remove(), 300); }, 5000);
    }
})();