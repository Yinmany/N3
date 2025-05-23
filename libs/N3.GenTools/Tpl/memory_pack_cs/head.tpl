using MemoryPack;
using FastSu;

{{ for item in using_list }}
using {{ item }};
{{- end }}

namespace {{namespace}}
{
    {{codes}}
}