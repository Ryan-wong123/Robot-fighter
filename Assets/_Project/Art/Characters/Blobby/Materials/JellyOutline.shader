Shader "Wobble Brawl/Jelly Outline"
{
    Properties
    {
        _Outline ("Outline Color", Color) = (0.045, 0.13, 0.025, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.04)) = 0.018
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        UsePass "Wobble Brawl/Toon Jelly/OUTLINE"
    }
}
