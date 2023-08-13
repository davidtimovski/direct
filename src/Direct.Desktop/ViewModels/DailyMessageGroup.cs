using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Direct.Desktop.ViewModels;

public partial class DailyMessageGroup : ObservableCollection<MessageViewModel>
{
    public DateOnly Date { get; }

    public DailyMessageGroup(List<MessageViewModel> messages, DateOnly groupDate, DateOnly localDate, double labelFontSize) : base(messages)
    {
        Date = groupDate;
        DateLabel = FormatHeader(groupDate, localDate);
        LabelFontSize = labelFontSize;
    }

    public string DateLabel { get; }

    private double _labelFontSize;
    public double LabelFontSize
    {
        get => _labelFontSize;
        set
        {
            if (_labelFontSize != value)
            {
                _labelFontSize = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(LabelFontSize)));
            }
        }
    }

    private static string FormatHeader(DateOnly groupDate, DateOnly localDate)
    {
        if (groupDate == localDate)
        {
            return "Today";
        }

        if ((localDate.DayNumber - groupDate.DayNumber) == 1)
        {
            return "Yesterday";
        }

        if ((localDate.DayNumber - groupDate.DayNumber) < 7)
        {
            return groupDate.ToString("dddd", Globals.Culture);
        }

        if (groupDate.Year == localDate.Year)
        {
            return groupDate.ToString("MMM dd", Globals.Culture);
        }

        return groupDate.ToString("MMM dd, yyyy", Globals.Culture);
    }
}
