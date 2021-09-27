using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public static class WorkingDayHelper
    {
        public static DateTime WorkingDayUsingOffset(DateTime date, int offset)
        {
            DateTime workingDay = date;

            if (date.DayOfWeek == DayOfWeek.Saturday)
                workingDay = date.AddDays(2);
            if (date.DayOfWeek == DayOfWeek.Sunday)
                workingDay = date.AddDays(1);

            int offsetDays = offset / 24;

            for (int i = 1; i <= offsetDays; i++)
            {
                workingDay = workingDay.AddDays(1);

                switch (workingDay.DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                        workingDay = workingDay.AddDays(2);
                        break;
                    case DayOfWeek.Sunday:
                        workingDay = workingDay.AddDays(1);
                        break;
                }
            }

            while (workingDay.DayOfWeek == DayOfWeek.Saturday || workingDay.DayOfWeek == DayOfWeek.Sunday)
            {
                workingDay = workingDay.AddDays(2);
            }

            return workingDay;
        }
    }
}
