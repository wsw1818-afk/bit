using System.Collections.Generic;

namespace AIBeat.Utils
{
    /// <summary>
    /// List<T> 오브젝트 풀링 유틸리티
    /// 매 프레임 List 할당/GC 방지용
    /// 사용법: var list = ListPool<T>.Get(); ... ListPool<T>.Return(list);
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new Stack<List<T>>();
        private const int DefaultCapacity = 32;

        /// <summary>
        /// 풀에서 List를 가져옴 (없으면 새로 생성)
        /// </summary>
        public static List<T> Get()
        {
            return pool.Count > 0 ? pool.Pop() : new List<T>(DefaultCapacity);
        }

        /// <summary>
        /// 사용 완료된 List를 풀에 반환
        /// </summary>
        public static void Return(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            pool.Push(list);
        }
    }
}
