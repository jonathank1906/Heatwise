using Avalonia.Controls;

namespace Heatwise.Views
{
    public partial class ToolTipView : UserControl
    {
        public ToolTipView()
        {
            InitializeComponent();
        }
        
        public void UpdateContent(string text)
        {
            var textBlock = this.FindControl<TextBlock>("TooltipText");
            textBlock!.Text = text;
        }
    }
}