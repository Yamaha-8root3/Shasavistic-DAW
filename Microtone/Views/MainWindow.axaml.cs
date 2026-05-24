using Avalonia.Controls;
using Avalonia.Media;
using Microtone.ViewModels;
using Microtone.Views.Services;

namespace Microtone.Views
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      DataContext = new MainWindowViewModel(new DialogService(this));
    }
  }
}