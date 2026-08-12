document.addEventListener("DOMContentLoaded", function () {

    loadUnreadNotificationCount();

    loadRecentNotifications();

    const markAllButton =
        document.getElementById("notificationMarkAllBtn");

    if (markAllButton) {

        markAllButton.addEventListener(
            "click",
            async function (event) {

                event.preventDefault();

                await markAllNotificationsAsRead();

            });
    }

});


// ============================================================
// Load Unread Notification Count
// ============================================================

async function loadUnreadNotificationCount() {

    try {

        const response =
            await fetch(
                "/Notification/UnreadCount");

        if (!response.ok) {
            return;
        }

        const result =
            await response.json();

        if (!result.success) {
            return;
        }

        updateNotificationBadge(
            result.count);

    }
    catch (error) {

        console.error(
            "Unable to load notification count.",
            error);

    }
}


// ============================================================
// Update Notification Badge
// ============================================================

function updateNotificationBadge(count) {

    const badge =
        document.getElementById(
            "notificationBadge");

    if (!badge) {
        return;
    }

    if (count > 0) {

        badge.textContent =
            count > 99
                ? "99+"
                : count;

        badge.classList.remove("d-none");

    }
    else {

        badge.classList.add("d-none");

    }
}


// ============================================================
// Load Recent Notifications
// ============================================================

async function loadRecentNotifications() {

    try {

        const response =
            await fetch(
                "/Notification/Recent");

        if (!response.ok) {
            return;
        }

        const result =
            await response.json();

        if (!result.success) {
            return;
        }

        renderRecentNotifications(
            result.notifications);

    }
    catch (error) {

        console.error(
            "Unable to load notifications.",
            error);

    }
}


// ============================================================
// Render Recent Notifications
// ============================================================

function renderRecentNotifications(
    notifications) {

    const container =
        document.getElementById(
            "notificationList");

    if (!container) {
        return;
    }

    container.innerHTML = "";

    if (!notifications ||
        notifications.length === 0) {

        container.innerHTML = `
            <div class="text-center py-4">

                <i class="bi bi-bell-slash
                          fs-3
                          text-muted
                          d-block
                          mb-2"></i>

                <small class="text-muted">
                    No notifications yet.
                </small>

            </div>
        `;

        return;
    }


    notifications.forEach(
        notification => {

            const item =
                createNotificationElement(
                    notification);

            container.appendChild(item);

        });
}


// ============================================================
// Create Notification Element
// ============================================================

function createNotificationElement(notification) {

    const wrapper =
        document.createElement("div");

    wrapper.className =
        "notification-dropdown-item";

    if (!notification.isRead) {

        wrapper.classList.add(
            "notification-unread");
    }


    const icon =
        getNotificationIcon(
            notification.notificationType);


    const createdAt =
        formatNotificationDate(
            notification.createdAt);


    wrapper.innerHTML = `

        <div class="notification-content">

            <!-- Icon -->

            <div class="notification-dropdown-icon">

                ${icon}

            </div>


            <!-- Content -->

            <div class="notification-body">

                <!-- Title -->

                <div class="notification-title-row">

                    <strong class="notification-title">

                        ${escapeHtml(
        notification.title)}

                    </strong>

                    ${!notification.isRead
            ? `
                            <span class="badge
                                         bg-primary
                                         notification-new-badge">

                                New

                            </span>
                          `
            : ""
        }

                </div>


                <!-- Message -->

                <div class="notification-message">

                    ${escapeHtml(
            notification.message)}

                </div>


                <!-- Date -->

                <div class="notification-date">

                    ${createdAt}

                </div>


                <!-- Mark as Read -->

                ${!notification.isRead
            ? `
                        <button
                            type="button"
                            class="btn btn-sm
                                   btn-link
                                   p-0
                                   notification-read-button
                                   mark-notification-read"
                            data-id="${notification.notificationId}">

                            Mark as read

                        </button>
                      `
            : ""
        }

            </div>

        </div>
    `;


    const markReadButton =
        wrapper.querySelector(
            ".mark-notification-read");


    if (markReadButton) {

        markReadButton.addEventListener(
            "click",
            async function (event) {

                event.preventDefault();

                event.stopPropagation();

                const notificationId =
                    this.dataset.id;

                await markNotificationAsRead(
                    notificationId);

            });
    }


    return wrapper;
}


// ============================================================
// Notification Icon
// ============================================================

function getNotificationIcon(type) {

    switch (type) {

        case 1:

            return `
                <i class="bi bi-person-plus-fill
                          text-primary"></i>
            `;

        case 2:

            return `
                <i class="bi bi-clock-history
                          text-danger"></i>
            `;

        case 3:

            return `
                <i class="bi bi-chat-left-text-fill
                          text-danger"></i>
            `;

        case 4:

            return `
                <i class="bi bi-calendar-x-fill
                          text-warning"></i>
            `;

        default:

            return `
                <i class="bi bi-bell-fill
                          text-secondary"></i>
            `;
    }
}


// ============================================================
// Mark One Notification as Read
// ============================================================

async function markNotificationAsRead(
    notificationId) {

    try {

        const token =
            document.querySelector(
                '#notificationAntiForgeryToken input[name="__RequestVerificationToken"]'
            )?.value;


        if (!token) {

            console.error(
                "Anti-forgery token not found.");

            return;
        }


        const response =
            await fetch(
                "/Notification/MarkAsRead",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },

                    body:
                        `notificationId=${encodeURIComponent(notificationId)}` +
                        `&__RequestVerificationToken=${encodeURIComponent(token)}`
                });


        if (!response.ok) {
            return;
        }


        await loadUnreadNotificationCount();

        await loadRecentNotifications();

    }
    catch (error) {

        console.error(
            "Unable to mark notification as read.",
            error);

    }
}


// ============================================================
// Mark All Notifications as Read
// ============================================================

async function markAllNotificationsAsRead() {

    try {

        const token =
            document.querySelector(
                '#notificationAntiForgeryToken input[name="__RequestVerificationToken"]'
            )?.value;


        if (!token) {

            console.error(
                "Anti-forgery token not found.");

            return;
        }


        const response =
            await fetch(
                "/Notification/MarkAllAsRead",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },

                    body:
                        `__RequestVerificationToken=${encodeURIComponent(token)}`
                });


        if (!response.ok) {
            return;
        }


        await loadUnreadNotificationCount();

        await loadRecentNotifications();

    }
    catch (error) {

        console.error(
            "Unable to mark all notifications as read.",
            error);

    }
}


// ============================================================
// Format Date
// ============================================================

function formatNotificationDate(
    dateString) {

    const date =
        new Date(dateString);

    if (Number.isNaN(date.getTime())) {

        return "";

    }

    return date.toLocaleString(
        "en-GB",
        {
            day: "2-digit",
            month: "short",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
}


// ============================================================
// Prevent HTML Injection
// ============================================================

function escapeHtml(value) {

    if (!value) {
        return "";
    }

    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}