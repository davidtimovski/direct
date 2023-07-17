using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Direct.ViewModels;

public class DailyMessageGroup : ObservableCollection<MessageViewModel>
{
    public DateOnly Date { get; }

    public DailyMessageGroup(List<MessageViewModel> items, DateOnly groupDate, DateOnly localDate) : base(items)
    {
        Date = groupDate;
        DateLabel = FormatHeader(groupDate, localDate);
    }

    public string DateLabel { get; set; }

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
