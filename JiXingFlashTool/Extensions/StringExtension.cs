using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JiXingFlashTool.Extensions
{
    public static class StringExtension
    {
        public static string RemoveNonNumeric(this string str)
        {
            return Regex.Replace(str, "[^\\d]*", "");
        }

        public static string RemoveNonNumericUntilNonNumeric(this string str)
        {
            string retStr = "";
            foreach (char c in str)
            {
                bool flag = c >= '0' && c <= '9';
                if (!flag)
                {
                    break;
                }
                retStr += c.ToString();
            }
            return retStr;
        }

        public static string RemoveIncompatiblePersonCount(this string str)
        {
            return Regex.Replace(str, "[^.kKMm\\d]*", "");
        }

        public static string RemoveNumeric(this string str)
        {
            return Regex.Replace(str, "\\d", "");
        }

        public static int MSConvertSecond(this string str)
        {
            string[] times = str.Split(new char[]
            {
                ':'
            });
            return int.Parse(times[0]) * 60 + int.Parse(times[1]);
        }

        public static bool IsMSString(this string str)
        {
            Regex regex = new Regex("^([0-5][0-9]):([0-5][0-9])$");
            return regex.IsMatch(str);
        }

        public static bool IsDomain(this string str)
        {
            string pattern = "^[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$";
            return StringExtension.IsMatch(pattern, str);
        }

        public static string GetIPByDomain(this string str)
        {
            IPAddress host = Dns.GetHostAddresses(str).First<IPAddress>();
            return host.ToString();
        }

        public static bool IsRunRedsocks2(this string str)
        {
            Regex regex = new Regex("[0-9][0-9]:[0-9][0-9]:[0-9][0-9]\\sredsocks2");
            return regex.IsMatch(str);
        }

        public static bool IsNumber(this string str)
        {
            bool flag = str == null;
            return !flag && str.All(new Func<char, bool>(char.IsDigit));
        }


        public static string RetailModel(this string str) { 

            if(str.IndexOf("G930") != -1 || str.IndexOf("G935") != -1) {
                return "S7";
            }
            return string.Empty;
        }

        public static int TiktokLivePersonConvertInt(this string str)
        {
            bool flag = str.Contains('k') || str.Contains('K');
            int result = 0;
            try
            {
                if (flag)
                {
                    string[] livePersonStrs = str.Replace("K", "").Replace("k", "").Split(new char[]
                    {
                    '.'
                    });
                    bool flag2 = !livePersonStrs[0].IsNumber();
                    if (flag2)
                    {
                        result = 0;
                    }
                    else
                    {
                        bool flag3 = !livePersonStrs[1].IsNumber();
                        if (flag3)
                        {
                            result = 0;
                        }
                        else
                        {
                            result = int.Parse(livePersonStrs[0]) * 1000 + int.Parse(livePersonStrs[1]) * 100;
                        }
                    }
                }
                else
                {
                    bool flag4 = str.Contains('m') || str.Contains('M');
                    if (flag4)
                    {
                        string[] livePersonStrs2 = str.Replace("m", "").Replace("M", "").Split(new char[]
                        {
                        '.'
                        });
                        bool flag5 = !livePersonStrs2[0].IsNumber();
                        if (flag5)
                        {
                            result = 0;
                        }
                        else
                        {
                            bool flag6 = !livePersonStrs2[1].IsNumber();
                            if (flag6)
                            {
                                result = 0;
                            }
                            else
                            {
                                result = int.Parse(livePersonStrs2[0]) * 1000000 + int.Parse(livePersonStrs2[1]) * 1000;
                            }
                        }
                    }
                    else
                    {
                        bool flag7 = str.Length == 0 || str == null;
                        if (flag7)
                        {
                            result = 0;
                        }
                        else
                        {
                            bool flag8 = !str.IsNumber();
                            if (flag8)
                            {
                                result = 0;
                            }
                            else
                            {
                                result = int.Parse(str);
                            }
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        public static long IPToLong(this string ip)
        {
            string newIP = ip.Split(new char[]
            {
                ':'
            }).First<string>();
            string[] ips = newIP.Split(new char[]
            {
                '.'
            });
            string sumIPString = string.Format("{0}{1}{2}{3}", new object[]
            {
                ips[0],
                ips[1],
                ips[2],
                ips[3]
            });
            return long.Parse(sumIPString);
        }

        public static bool IsMatch(string expression, string str)
        {
            Regex reg = new Regex(expression);
            bool flag = string.IsNullOrEmpty(str);
            return !flag && reg.IsMatch(str);
        }
        public static long SortNumber(this string str)
        {
            long retNumber = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c >= '0' && c <= '9')
                {
                    retNumber += int.Parse(c.ToString()) * (int)Math.Pow(10, str.Length - i);
                }
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    int number = System.Text.Encoding.ASCII.GetBytes(str).First();
                    retNumber += number * (int)Math.Pow(10, str.Length - i);
                }
            }

            return retNumber;
        }

        public static string ToPascal(this string str)
        {
            string[] split = str.Split(new char[] { '/', ' ', '_', '.' });
            string newStr = "";
            foreach (var item in split)
            {
                char[] chars = item.ToCharArray();
                chars[0] = char.ToUpper(chars[0]);
                for (int i = 1; i < chars.Length; i++)
                {
                    chars[i] = char.ToLower(chars[i]);
                }
                newStr += new string(chars);
            }
            return newStr;
        }

        public static List<Point> YMPointStringConvertDoublePoint(this string str)
        {
            List<Point> list = new List<Point>();
            string[] verifyCodeInfoStringArray = str.Split('|');
            string[] point1 = verifyCodeInfoStringArray[0].Split(',');
            string[] point2 = verifyCodeInfoStringArray[1].Split(',');
            list.Add(new Point(Convert.ToInt16(point1[0]), Convert.ToInt16(point1[1])));
            list.Add(new Point(Convert.ToInt16(point2[0]), Convert.ToInt16(point2[1])));
            return list;
        }

        ///// <summary>
        ///// 分秒字符串转秒 格式 01:30 转成90
        ///// </summary>
        ///// <param name="minuteSecondString">分秒字符串</param>
        public static int MinuteSecondConvertRealSecond(this string str)
        {
            string[] minuteSecondSpliteRet = str.Split(':');
            return int.Parse(minuteSecondSpliteRet.First()) * 60 + int.Parse(minuteSecondSpliteRet.Last());
        }


        public static List<string> Split(this string str, string[] splitArray) {
            try
            {
                List<string> striparr = str.Split(splitArray, StringSplitOptions.None).ToList();
                striparr = striparr.Where(s => !string.IsNullOrEmpty(s)).ToList();
                return striparr;
            }
            catch {
                return null;
            }
        }

        public static bool IsNull(this string str)
        {
            return str == null || str == string.Empty || str.Length == 0;
        }

        public static DateTime ConvertStringToDateTime(this string timeStamp)
        {
           return  DateTimeOffset.FromUnixTimeSeconds(long.Parse(timeStamp)).LocalDateTime;
        }

        public static string ConvertStringToDateTimeString(this string timeStamp,string format)
        {
            return timeStamp.ConvertStringToDateTime().ToString(format);
        }

        public static string MatchedPercentage(this string txt) {
            string pattern = @"(\d+)%";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(txt);

            if (match.Success)
            {
                return match.Value;
            }
            else
            {
                return string.Empty;
            }

        }

        public static string ReplaceText(this string txt,string oldText,string newText) {
            if (txt.IsNull()) return txt;
            return txt.Replace(oldText,newText);
        }
    }
}
