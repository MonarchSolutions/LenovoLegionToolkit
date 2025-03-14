﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Controls.Dashboard;

public partial class DiscreteGPUControl
{
    private readonly GPUController _gpuController = IoCContainer.Resolve<GPUController>();
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener = IoCContainer.Resolve<NativeWindowsMessageListener>();

    public DiscreteGPUControl()
    {
        InitializeComponent();

        _gpuController.Refreshed += GpuController_Refreshed;
        _nativeWindowsMessageListener.Changed += NativeWindowsMessageListener_Changed;

        IsVisibleChanged += DiscreteGPUControl_IsVisibleChanged;
    }

    protected override void OnFinishedLoading() { }

    protected override async Task OnRefreshAsync()
    {
        if (!_gpuController.IsSupported())
            throw new InvalidOperationException("Unsupported operation");

        if (!IsVisible)
            return;

        await _gpuController.StartAsync();
    }

    private async void NativeWindowsMessageListener_Changed(object? sender, NativeWindowsMessage e)
    {
        if (e != NativeWindowsMessage.OnDisplayDeviceArrival)
            return;

        Visibility = Visibility.Visible;
        await RefreshAsync();
    }

    private async void DiscreteGPUControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            return;

        _content.Visibility = Visibility.Hidden;

        await _gpuController.StopAsync();
    }

    private void GpuController_Refreshed(object? sender, GPUController.GPUStatus e) => Dispatcher.Invoke(() =>
    {
        var tooltipStringBuilder = new StringBuilder(Resource.DiscreteGPUControl_PerformanceState);
        tooltipStringBuilder.AppendLine().Append(" · ").Append(e.PerformanceState ?? Resource.DiscreteGPUControl_PerformanceState_Unknown);

        if (e.State is GPUController.GPUState.Unknown or GPUController.GPUState.NvidiaGpuNotFound)
        {
            _discreteGPUStatusActiveIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusInactiveIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusDescription.Content = "-";
            _gpuInfoButton.ToolTip = null;
            _gpuInfoButton.IsEnabled = false;
            Visibility = Visibility.Collapsed;
            return;
        }

        if (e.IsActive)
        {
            var processesStringBuilder = new StringBuilder();

            if (e.ProcessCount > 0)
            {
                processesStringBuilder.Append(Resource.DiscreteGPUControl_Processes);
                foreach (var p in e.Processes.OrderBy(p => p.ProcessName))
                {
                    try { processesStringBuilder.AppendLine().Append(" · ").Append(p.ProcessName); }
                    catch {  /* Ignored. */ }
                }
            }
            else
            {
                processesStringBuilder.Append(Resource.DiscreteGPUControl_NoProcesses);
            }

            _discreteGPUStatusActiveIndicator.Visibility = Visibility.Visible;
            _discreteGPUStatusInactiveIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusPoweredOffIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusDescription.Content = Resource.Active;
            _gpuInfoButton.ToolTip = tooltipStringBuilder.AppendLine().AppendLine().Append(processesStringBuilder).ToString();
            _gpuInfoButton.IsEnabled = true;
        }
        else if (e.IsPoweredOff)
        {
            _discreteGPUStatusActiveIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusInactiveIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusPoweredOffIndicator.Visibility = Visibility.Visible;
            _discreteGPUStatusDescription.Content = Resource.PoweredOff;
            _gpuInfoButton.ToolTip = tooltipStringBuilder.ToString();
            _gpuInfoButton.IsEnabled = true;
        }
        else
        {
            _discreteGPUStatusActiveIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusInactiveIndicator.Visibility = Visibility.Visible;
            _discreteGPUStatusPoweredOffIndicator.Visibility = Visibility.Collapsed;
            _discreteGPUStatusDescription.Content = Resource.Inactive;
            _gpuInfoButton.ToolTip = tooltipStringBuilder.ToString();
            _gpuInfoButton.IsEnabled = true;
        }

        _deactivateGPUButton.IsEnabled = e.CanBeDeactivated;

        if (e.CanBeDeactivated)
        {
            _deactivateGPUButtonText.SetResourceReference(ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            _deactivateGPUButtonIcon.SetResourceReference(ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
        }
        else
        {
            _deactivateGPUButtonText.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _deactivateGPUButtonIcon.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
        }

        _deactivateGPUButton.ToolTip = e.State switch
        {
            GPUController.GPUState.MonitorsConnected => Resource.DiscreteGPUControl_MonitorConnected,
            GPUController.GPUState.DeactivatePossible => Resource.DiscreteGPUControl_DisablePossible,
            _ => null,
        };

        _content.Visibility = Visibility.Visible;
    });

    private void DeactivateGPUButton_Click(object sender, RoutedEventArgs e)
    {
        if (_deactivateGPUButton.ContextMenu is null)
            return;

        _deactivateGPUButton.ContextMenu.PlacementTarget = _deactivateGPUButton;
        _deactivateGPUButton.ContextMenu.Placement = PlacementMode.Bottom;
        _deactivateGPUButton.ContextMenu.IsOpen = true;
    }

    private async void KillAppsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _deactivateGPUButton.IsEnabled = false;
        await _gpuController.KillGPUProcessesAsync();
    }

    private async void RestartGPUMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _deactivateGPUButton.IsEnabled = false;
        await _gpuController.DeactivateGPUAsync();
    }
}
