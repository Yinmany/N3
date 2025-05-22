namespace N3Core;

public static class TypeId
{
    private static readonly Dictionary<Type, int> TypeIdDic = new();
    private static int _typeIdGen = 0;

    /// <summary>
    /// 获取类型的Id(加了锁，注意不要在多线程下频繁获取)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int Get(Type type)
    {
        lock (TypeIdDic)
        {
            if (!TypeIdDic.TryGetValue(type, out var value))
            {
                value = ++_typeIdGen;
                TypeIdDic.Add(type, value);
            }

            return value;
        }
    }

    /// <summary>
    /// 泛型Id缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Cache<T>
    {
        public static readonly int Value;

        static Cache()
        {
            Value = Get(typeof(T));
        }
    }
}