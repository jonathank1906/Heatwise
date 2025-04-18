/*
--/ Notification Event Handler \--

Overview:
 This class can be used to send notifications from anywhere in the code.

Usage:
    0. Add using Sem2Proj.Events; [optional since you can use the full path Events.Notification when firing the event]
    1. Subscribe to the event using Notification.OnNewNotification += YourMethod;
    2. Call Notification.Invoke("Your message") to send a notification
 */

using System;

namespace Sem2Proj.Events;

public static class Notification
{
    public static event Action<string>? OnNewNotification;

    public static void Invoke(string message)
    {
        OnNewNotification?.Invoke(message);
    }
}