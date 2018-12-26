using System;

namespace AspNetCoreWebApi.Domain
{
    static class StatusHelper
    {
        public static Status Parse(string status)
        {
            switch (status)
            {
                case "свободны":
                    return Status.Free;
                case "заняты":
                    return Status.Reserved;
                case "всё сложно":
                    return Status.Complicated;
                default:
                    throw new ArgumentException(nameof(status));
            }
        }

        public static bool TryParse(string str, out Status status)
        {
            switch (str)
            {
                case "свободны":
                    status = Status.Free;
                    return true;
                case "заняты":
                    status = Status.Reserved;
                    return true;
                case "всё сложно":
                    status = Status.Complicated;
                    return true;
                default:
                    status = Status.Free;
                    return false;
            }
        }

        public static string ToStr(this Status status)
        {
            switch(status)
            {
                case Status.Free:
                    return "свободны";
                case Status.Reserved:
                    return "заняты";
                case Status.Complicated:
                    return "всё сложно";
            }

            return String.Empty;
        }
    }

    public enum Status
    {
        Free,
        Reserved,
        Complicated
    }
}