using System;

namespace Heatwise.Interfaces;

public interface IPopupViewModel
{
    bool IsDraggable { get; } // Indicates if the popup is draggable
    bool ShowBackdrop { get; } // Indicates if the popup should show a backdrop
    PopupStartupLocation StartupLocation { get; }
    void SetCloseAction(Action close);
}
