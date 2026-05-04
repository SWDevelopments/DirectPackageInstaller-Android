using System;

namespace DirectPackageInstaller.Host
{
    struct HttpRange
    {
        public HttpRange(string Header)
        {
            Begin = 0;
            End = null;
            IsSuffixRange = false;
            SuffixLength = null;
            IsValid = false;

            if (string.IsNullOrWhiteSpace(Header))
                return;

            var EqualIndex = Header.IndexOf('=');
            if (EqualIndex >= 0)
            {
                var Unit = Header.Substring(0, EqualIndex).Trim();
                if (!Unit.Equals("bytes", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            var RangeStr = EqualIndex >= 0 ? Header.Substring(EqualIndex + 1).Trim() : Header.Trim();
            var CommaIndex = RangeStr.IndexOf(',');
            if (CommaIndex >= 0)
                RangeStr = RangeStr.Substring(0, CommaIndex).Trim();

            var DashIndex = RangeStr.IndexOf('-');
            if (DashIndex < 0)
                return;

            var BeginStr = RangeStr.Substring(0, DashIndex).Trim();
            var EndStr = RangeStr.Substring(DashIndex + 1).Trim();

            if (string.IsNullOrWhiteSpace(BeginStr))
            {
                if (long.TryParse(EndStr, out var ParsedSuffixLength) && ParsedSuffixLength > 0)
                {
                    IsSuffixRange = true;
                    SuffixLength = ParsedSuffixLength;
                    IsValid = true;
                }
                return;
            }

            if (!long.TryParse(BeginStr, out var ParsedBegin) || ParsedBegin < 0)
                return;

            Begin = ParsedBegin;

            if (!string.IsNullOrWhiteSpace(EndStr) && EndStr != "*")
            {
                if (!long.TryParse(EndStr, out var ParsedEnd) || ParsedEnd < Begin)
                    return;

                End = ParsedEnd;
            }

            IsValid = true;
        }

        public long Begin;
        public long? End;
        public bool IsValid;
        public bool IsSuffixRange;
        public long? SuffixLength;
        public long? Length => IsSuffixRange ? SuffixLength : (End - Begin) + 1;

        public long GetStart(long TotalLength)
        {
            if (!IsValid || TotalLength <= 0)
                return 0;

            if (IsSuffixRange)
                return Math.Max(0, TotalLength - (SuffixLength ?? 0));

            if (Begin > TotalLength)
                return TotalLength;

            return Begin;
        }

        public long GetLength(long TotalLength)
        {
            if (!IsValid || TotalLength <= 0)
                return 0;

            var Start = GetStart(TotalLength);
            var EndPosition = IsSuffixRange ? TotalLength - 1 : End ?? TotalLength - 1;

            if (EndPosition >= TotalLength)
                EndPosition = TotalLength - 1;

            if (EndPosition < Start)
                return 0;

            return EndPosition - Start + 1;
        }
    }
}
