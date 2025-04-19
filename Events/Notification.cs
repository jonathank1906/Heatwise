/*
--/ Notification Event Handler \--

Overview:
 This class can be used to send notifications from anywhere in the code.

Usage:
    0. Add 'using Sem2Proj.Events;' [optional since you can use the full path Events.Notification when firing the event]
    0.5 Add 'using Sem2Proj.Enums;' [optional since you can use the full path Enums.NotificationType when firing the event]

    1. Call Notification.Invoke("[Your message]", NotificationType.[Your Type]) to send a notification.

Types of Notifications:
    1. Info
    2. Warning
    3. Error
    4. Confirmation
 */

using System;
using Sem2Proj.Enums;

namespace Sem2Proj.Events;

public static class Notification
{
    public static event Action<string, NotificationType>? OnNewNotification;

    public static void Invoke(string message, NotificationType type)
    {
        OnNewNotification?.Invoke(message, type);
    }
}