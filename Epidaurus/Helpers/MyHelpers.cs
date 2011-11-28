using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Epidaurus.Helpers
{
    public static class MH
    {
        public static string ScoreHelper(short score)
        {
            double db = ((double)score) / 10.0;
            return db.ToString("0.0");
        }

        public static string ScoreHelper(int score)
        {
            double db = ((double)score) / 10.0;
            return db.ToString("0.0");
        }

        public static string RuntimeHelper(int minutes)
        {
            int orig = minutes;
            int days = minutes / (60 * 24);
            minutes -= days * (24 * 60);
            int hrs = minutes / 60;
            minutes -= hrs * 60;
            string res = minutes.ToString() + "m";
            if (hrs > 0)
                res = hrs.ToString() + "t " + res;
            if (days > 0)
                res = days.ToString() + "d " + res;
            return res;
        }
    }
}