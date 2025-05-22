public enum {{ name }}
{
    {{~ for e in elems ~}}
        {{~ if !(string.empty e.tail_comment)~}}
    /// <summary>
    /// {{ e.tail_comment }}
    /// </summary>
        {{~ end ~}}
    {{ e.name }} = {{ e.index }},
    {{~ end -}}
}