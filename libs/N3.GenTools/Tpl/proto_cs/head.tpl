using ProtoBuf;
using N3Core;
using System.Collections.Generic;

{{- for item in using_list }}
using {{ item }};
{{- end }}

namespace {{namespace}}
{
    {{ codes }}    
}