using System.Collections.Generic;

namespace MemoryInfo
{
    public static class ExtensionMethods
    {
        #region ByteArray

        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

        //public static IEnumerable<uint> Locate(this byte[] self, byte[] candidate)
        //{
        //    if (self == null
        //        || candidate == null
        //        || self.Length == 0
        //        || candidate.Length == 0
        //        || candidate.Length > self.Length)
        //        yield break;

        //    int selfLength = self.Length - candidate.Length;
        //    for (uint selfIndex = 0; selfIndex < selfLength; selfIndex++)
        //    {
        //        for (int candidateIndex = 0; candidateIndex < candidate.Length; candidateIndex++)
        //            if (self[selfIndex + candidateIndex] != candidate[candidateIndex])
        //                continue;

        //        yield return selfIndex;
        //    }
        //}

        public static unsafe long IndexOf(this byte[] selfArray, byte[] candidateArray)
        {
            fixed (byte* self = selfArray)
            fixed (byte* candidate = candidateArray)
            {
                byte* selfEnd = self + selfArray.LongLength - candidateArray.LongLength;
                byte* selfNext = self;
                byte* candidateEnd = candidate + candidateArray.LongLength;

                for (uint selfIndex = 0; selfNext < selfEnd; selfIndex++, selfNext++)
                {
                    byte* selfCurrent = selfNext;
                    byte* candidateCurrent = candidate;
                    bool isPatternFound = true;

                    for (; candidateCurrent < candidateEnd; candidateCurrent++, selfCurrent++)
                    {
                        if (*candidateCurrent != *selfCurrent)
                        {
                            isPatternFound = false;
                            break;
                        }
                    }

                    if (isPatternFound)
                        return selfIndex;
                }
                return -1;
            }
        }

        public static unsafe List<long> IndexesOf(this byte[] Haystack, byte[] Needle)
        {
            List<long> Indexes = new List<long>();
            fixed (byte* H = Haystack)
            fixed (byte* N = Needle)
            {
                uint i = 0;
                for (byte* hNext = H, hEnd = H + Haystack.LongLength; hNext < hEnd; i++, hNext++)
                {
                    bool Found = true;
                    for (byte* hInc = hNext, nInc = N, nEnd = N + Needle.LongLength; Found && nInc < nEnd; Found = *nInc == *hInc, nInc++, hInc++) ;
                    if (Found) Indexes.Add(i);
                }
                return Indexes;
            }
        }

        #endregion ByteArray
    }
}