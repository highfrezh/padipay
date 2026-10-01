// PadiPay Global Client Script

document.addEventListener('DOMContentLoaded', function () {
    const notifDropdownToggle = document.getElementById('navbarNotifDropdown');
    const notifBadge = document.getElementById('navbarNotifBadge');
    const notifCountPill = document.getElementById('navbarNotifCountPill');
    const notifList = document.getElementById('navbarNotifList');
    const btnMarkAll = document.getElementById('btnNavbarMarkAllRead');

    if (!notifDropdownToggle || !notifList) return;

    // Helper: Map NotificationType string to Bootstrap icon + color
    function getNotificationIcon(type) {
        switch (type) {
            case 'OrderPlaced':
                return { icon: 'bi-box-seam', color: '#2563EB', bg: 'rgba(37,99,235,0.1)' };
            case 'WorkDelivered':
                return { icon: 'bi-rocket-takeoff-fill', color: '#0284C7', bg: 'rgba(2,132,199,0.1)' };
            case 'RevisionRequested':
                return { icon: 'bi-arrow-repeat', color: '#D97706', bg: 'rgba(217,119,6,0.1)' };
            case 'OrderCompleted':
                return { icon: 'bi-check-circle-fill', color: '#059669', bg: 'rgba(5,150,105,0.1)' };
            case 'DisputeRaised':
                return { icon: 'bi-exclamation-triangle-fill', color: '#DC2626', bg: 'rgba(220,38,38,0.1)' };
            case 'DisputeResolved':
                return { icon: 'bi-shield-check', color: '#7C3AED', bg: 'rgba(124,58,237,0.1)' };
            case 'MessageReceived':
                return { icon: 'bi-chat-dots-fill', color: '#2563EB', bg: 'rgba(37,99,235,0.1)' };
            case 'WalletFunded':
                return { icon: 'bi-wallet2', color: '#059669', bg: 'rgba(5,150,105,0.1)' };
            default:
                return { icon: 'bi-bell-fill', color: '#6B7280', bg: 'rgba(107,114,128,0.1)' };
        }
    }

    // Load recent notifications when opening the dropdown
    async function loadNotifications() {
        try {
            const res = await fetch('/Notification/GetRecent');
            if (!res.ok) return;

            const data = await res.json();
            const items = data.notifications || [];

            if (items.length === 0) {
                notifList.innerHTML = `
                    <div class="text-center py-4 text-muted small">
                        <i class="bi bi-bell-slash fs-4 d-block mb-1 opacity-50"></i>
                        No notifications yet
                    </div>
                `;
                return;
            }

            let html = '<div class="list-group list-group-flush">';
            items.forEach(n => {
                const style = getNotificationIcon(n.type);
                const bgStyle = n.isRead ? 'background: #fff;' : 'background: rgba(0, 192, 118, 0.04);';
                const unreadDot = n.isRead ? '' : '<span class="badge bg-primary p-1 rounded-circle flex-shrink-0" style="width: 7px; height: 7px;"></span>';

                html += `
                    <a href="${n.targetUrl}" class="list-group-item list-group-item-action p-2.5 border-bottom d-flex align-items-start gap-2.5 notif-dropdown-item" 
                       data-id="${n.id}" style="${bgStyle} transition: background 0.15s ease;">
                        <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0" 
                             style="width: 32px; height: 32px; font-size: 0.9rem; background: ${style.bg}; color: ${style.color};">
                            <i class="bi ${style.icon}"></i>
                        </div>
                        <div class="flex-grow-1 min-w-0">
                            <div class="d-flex align-items-center justify-content-between gap-1">
                                <span class="fw-semibold text-dark small text-truncate" style="font-size: 0.8rem;">${escapeHtml(n.title)}</span>
                                ${unreadDot}
                            </div>
                            <p class="text-secondary small mb-1 text-truncate" style="font-size: 0.75rem; max-width: 230px;">${escapeHtml(n.message)}</p>
                            <span class="text-muted" style="font-size: 0.68rem;">${n.createdAtFormatted}</span>
                        </div>
                    </a>
                `;
            });
            html += '</div>';
            notifList.innerHTML = html;

            // Attach click event to mark single notification read
            document.querySelectorAll('.notif-dropdown-item').forEach(el => {
                el.addEventListener('click', function () {
                    const id = this.getAttribute('data-id');
                    fetch('/Notification/MarkAsRead?id=' + id, { method: 'POST' });
                });
            });

        } catch (e) {
            console.error('Failed to load notifications', e);
        }
    }

    // Refresh unread count
    async function updateUnreadCount() {
        try {
            const res = await fetch('/Notification/GetUnreadCount');
            if (!res.ok) return;
            const data = await res.json();
            const count = data.count || 0;

            if (count > 0) {
                notifBadge.style.display = '';
                notifBadge.textContent = count > 99 ? '99+' : count;
                if (notifCountPill) {
                    notifCountPill.style.display = '';
                    notifCountPill.textContent = count + ' unread';
                }
            } else {
                notifBadge.style.display = 'none';
                if (notifCountPill) {
                    notifCountPill.style.display = 'none';
                }
            }
        } catch (e) {
            console.error('Failed to update unread count', e);
        }
    }

    // Listen to dropdown open
    notifDropdownToggle.addEventListener('show.bs.dropdown', function () {
        loadNotifications();
    });

    // Mark all read button
    if (btnMarkAll) {
        btnMarkAll.addEventListener('click', async function (e) {
            e.stopPropagation();
            try {
                const res = await fetch('/Notification/MarkAllAsRead', { method: 'POST' });
                if (res.ok) {
                    notifBadge.style.display = 'none';
                    if (notifCountPill) notifCountPill.style.display = 'none';
                    loadNotifications();
                }
            } catch (e) {
                console.error(e);
            }
        });
    }

    // Periodic count refresh every 60s
    setInterval(updateUnreadCount, 60000);

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
});
