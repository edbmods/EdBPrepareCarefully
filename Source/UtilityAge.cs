using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdB.PrepareCarefully {
    public static class UtilityAge {
        public static long TicksPerYear = 3600000L;
        public static long TicksPerDay = 60000L;
        public static long DaysPerYear = 60L;
        public static long TicksFromYearsAndDays(int years, int days) {
            return ((long)years * TicksPerYear) + ((long)days * TicksPerDay);
        }
        public static long TicksFromYears(int years) {
            return TicksFromYearsAndDays(years, 0);
        }
        public static int TicksToDays(long ticks) {
            return (int)(ticks / TicksPerDay);
        }
        public static int TicksToDayOfYear(long ticks) {
            return TicksToDays(ticks) % (int)AgeModifier.DaysPerYear;
        }
        public static int TicksOfDay(long ticks) {
            return (int)(ticks % TicksPerDay);
        }

    }
}
