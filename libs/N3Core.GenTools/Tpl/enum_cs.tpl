{{
    idx = 0
    idx2 = 0
}}
namespace {{namespace}}
{
    public static partial class {{ name }}
    {
{{~ for e in items ~}}
    {{~ if e.localization ~}}
        /// <summary> 
        /// {{ e.comment }}
        /// {{~ for loc in e.localization; $" <para>{loc}</para>"; end }} 
        /// </summary>
    {{~ else ~}}
        {{~ if !(string.empty e.comment)~}}
        /// <summary> {{ e.comment }} </summary>
        {{~ end ~}}
    {{~ end ~}}
        public const int {{ e.name }} = {{ e.value }};
{{~ end }}
{{~ if is_i18n ~}}
#if ERROR_LOCALIZATION
        /// <summary> 当前使用语言 </summary>
        public static byte Language = 0;
    {{~ for e in language; idx=idx+1 ~}}
        public const byte Language_{{e}} = {{idx}};
    {{~ end}}

        public static string T(int error)
        {
            if (_i18n.TryGetValue(error, out var localization) && localization.Length >= Language + 1)
            {
                var str = localization[Language];
                if(string.IsNullOrEmpty(str))
                {
                    return localization[0];
                }
                return str;
            }
            return error.ToString();
        }

        static readonly System.Collections.Generic.Dictionary<int, string[]> _i18n = new()
        {
    {{~ for e in items ~}}
        {{~ if e.localization }}
            { 
                {{e.value}}, 
                new string[] { "{{e.comment}}" , {{~ for loc in e.localization; $"\"{loc}\","; end ~}} } 
            },
        {{~ end ~}}
    {{~ end }}
        };
#endif
{{~ end ~}}
    }
}