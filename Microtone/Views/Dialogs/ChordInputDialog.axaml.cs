using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microtone.Models;
using Microtone.Services.Parser;
using Microtone.ViewModels.Dialogs;

namespace Microtone.Views.Dialogs;

public partial class ChordInputDialog : Window
{
    private readonly Dimensions<int> offset1d;
    public ChordInputDialog(Dimensions<int> offset1d)
    {
        this.offset1d = offset1d;
        InitializeComponent();
        DataContext = new ChordInputDialogViewModel();

        Opened += ChordInputDialog_Opened;
    }

    private void ChordInputDialog_Opened(object? sender, System.EventArgs e)
    {
        InputBox.Focus();
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (InputBox.Text == null || InputBox.Text.Length == 0) return;
        var res = HarmononymParser.TryParseToHarmonographs(InputBox.Text);
        if (res is null || res.Value is null) return;

        res.Value.Sort( (a, b) => a.ToOvertoneFormula(offset1d).RatioValue.CompareTo(
                                    b.ToOvertoneFormula(offset1d).RatioValue) );
        Close(res.Value);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void InputBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (InputBox.Text == null || InputBox.Text.Length == 0) return;
        var res = HarmononymParser.TryParseToHarmonographs(InputBox.Text);
        DetailLabel.Text = res.ToString();
        OkButton.IsEnabled = res.IsOk;
    }
}