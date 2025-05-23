using System.Collections.Generic;

namespace N3
{
    public class Map2<Tk, Tv> where Tk : notnull where Tv : notnull
    {
        private readonly Dictionary<Tk, Tv> _kvMap = new();
        private readonly Dictionary<Tv, Tk> _vkMap = new();

        public void Add(Tk key, Tv value)
        {
            _kvMap.Add(key, value);
            _vkMap.Add(value, key);
        }
    }
}