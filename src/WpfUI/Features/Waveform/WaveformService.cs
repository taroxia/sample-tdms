// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using R3;
using R3.Collections;
using ScottPlot;

namespace WpfUI.Features.Waveform;


public record PlotInfo(
    string FilePath,
    string GroupName,
    string ChannelName,
    double[] Data
);

public sealed class WaveformStateService
{
    public ReactiveProperty<AxisLimits> CurrentAxisLimits { get; } = new(AxisLimits.NoLimits);
    public ReactiveProperty<double[]?> Xs { get; } = new(null);


    private readonly ObservableCollection<double[]> _leftYs = new([]);
    public ObservableCollection<double[]> LeftYs => _leftYs;
    public Observable<NotifyCollectionChangedEventArgs> OnLeftYsChanged =>
        Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
            h => new NotifyCollectionChangedEventHandler((sender, e) => h(e)),
            h => _leftYs.CollectionChanged += h,
            h => _leftYs.CollectionChanged -= h
        );
    public void AppendLeftYs(double[] newData) => _leftYs.Add(newData);
}
