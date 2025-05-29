using System;

namespace Heatwise.Interfaces;

public interface IPopupViewModel
{
    bool IsDraggable { get; } 
    bool ShowBackdrop { get; }
    PopupStartupLocation StartupLocation { get; }
    void SetCloseAction(Action close);
}