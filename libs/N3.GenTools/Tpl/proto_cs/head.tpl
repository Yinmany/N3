using ProtoBuf;
using N3;
using System.Collections.Generic;

{{- for item in using_list }}
using {{ item }};
{{- end }}

namespace {{namespace}}
{
    {{ codes }}    
}