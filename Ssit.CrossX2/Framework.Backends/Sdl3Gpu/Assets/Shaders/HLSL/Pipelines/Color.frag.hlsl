// Direct3D 12 port of Metal/Pipelines/Color.frag.metal (ps_5_1).

struct VertexOut
{
    float4 position : SV_Position;
    float4 color    : TEXCOORD0;
};

float4 fragmentMain(VertexOut input) : SV_Target0
{
    return input.color;
}
