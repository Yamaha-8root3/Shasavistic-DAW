using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microtone.ViewModels.Controls;
using System;
using System.Diagnostics;

namespace Microtone.Views.Controls;

public partial class TimeIndicatorControl : UserControl
{
  //public TimeIndicatorViewModel ViewModel { get; } = new();
  public TimeIndicatorControl()
  {
    InitializeComponent();
  }
  private void OnEditCommit(object? sender, RoutedEventArgs e)
  {
    if (DataContext is TimeIndicatorViewModel vm)
    {
      vm.CommitEdit(vm.EditText);
      vm.IsEditing = false;
    }
  }

  private void OnEditKeyDown(object? sender, KeyEventArgs e)
  {
    if (DataContext is TimeIndicatorViewModel vm)
    {
      if (e.Key == Key.Enter) { vm.CommitEdit(vm.EditText); vm.IsEditing = false; }
      else if (e.Key == Key.Escape) vm.IsEditing = false;
    }
  }

  private void OnTextClicked(object? sender, PointerPressedEventArgs e)
  {
    if (DataContext is TimeIndicatorViewModel vm)
    {
      if (e.Properties.IsLeftButtonPressed)
      {
        vm.EditText = vm.DisplayText;
        vm.IsEditing = true;
        this.editbox.Focus();
        this.editbox.SelectAll();
      }
    }
  }
}