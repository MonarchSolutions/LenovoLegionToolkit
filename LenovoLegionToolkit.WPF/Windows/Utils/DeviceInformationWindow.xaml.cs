﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class DeviceInformationWindow
{
    private readonly WarrantyChecker _warrantyChecker = IoCContainer.Resolve<WarrantyChecker>();

    public DeviceInformationWindow() => InitializeComponent();

    private async void DeviceInformationWindow_Loaded(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync(bool forceRefresh = false)
    {
        var mi = await Compatibility.GetMachineInformationAsync();

        _manufacturerLabel.Content = mi.Vendor;
        _modelLabel.Content = mi.Model;
        _mtmLabel.Content = mi.MachineType;
        _serialNumberLabel.Content = mi.SerialNumber;
        _biosLabel.Content = mi.BiosVersionRaw;

        try
        {
            _refreshWarrantyButton.IsEnabled = false;

            _warrantyStartLabel.Content = "-";
            _warrantyEndLabel.Content = "-";
            _warrantyLinkCardAction.Tag = null;
            _warrantyLinkCardAction.IsEnabled = false;

            var warrantyInfo = await _warrantyChecker.GetWarrantyInfo(mi, forceRefresh);
            _warrantyLinkCardAction.IsEnabled = false;

            if (!warrantyInfo.HasValue)
                return;

            _warrantyStartLabel.Content = warrantyInfo.Value.Start is not null ? warrantyInfo.Value.Start?.ToString(LocalizationHelper.ShortDateFormat) : "-";
            _warrantyEndLabel.Content = warrantyInfo.Value.End is not null ? warrantyInfo.Value.End?.ToString(LocalizationHelper.ShortDateFormat) : "-";
            _warrantyLinkCardAction.Tag = warrantyInfo.Value.Link;

            _warrantyLinkCardAction.IsEnabled = true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't load warranty info.", ex);
        }
        finally
        {
            _refreshWarrantyButton.IsEnabled = true;
        }
    }

    private async void RefreshWarrantyButton_OnClick(object sender, RoutedEventArgs e) => await RefreshAsync(true);

    private async void DeviceCardControl_Click(object sender, RoutedEventArgs e)
    {
        if (((sender as CardControl)?.Content as Label)?.Content is not string str)
            return;

        try
        {
            Clipboard.SetText(str);
            await _snackBar.ShowAsync(Resource.CopiedToClipboard_Title, string.Format(Resource.CopiedToClipboard_Message_WithParam, str));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't copy to clipboard", ex);
        }
    }

    private void WarrantyLinkCardAction_OnClick(object sender, RoutedEventArgs e)
    {
        var link = _warrantyLinkCardAction.Tag as Uri;
        link?.Open();
    }
}
