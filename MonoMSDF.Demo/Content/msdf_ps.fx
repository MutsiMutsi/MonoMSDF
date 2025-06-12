#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Texture and sampler
Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Shader parameters
float4x4 WorldViewProjection;

// Only needed for small text optimization - can be omitted if you don't use those techniques
float2 AtlasSize;

// Vertex shader input structure
struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 FillColor : COLOR0;
    float4 StrokeColor : COLOR1;
    
    // These are the 4 rows of the instance's transform matrix
    float4 WorldMatrixRow0 : TEXCOORD1;
    float4 WorldMatrixRow1 : TEXCOORD2;
    float4 WorldMatrixRow2 : TEXCOORD3;
    float4 WorldMatrixRow3 : TEXCOORD4;
    
    float2 PixelRanges : TEXCOORD5;
};

// Vertex shader output structure
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 FillColor : COLOR0;
    float4 StrokeColor : COLOR1;
    float2 TextureCoordinates : TEXCOORD0;
    
    float2 PixelRanges : TEXCOORD1;
};

// Vertex shader
VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output;
	
	// Transform vertex position to screen space
    float4x4 WorldMatrix = float4x4(
        input.WorldMatrixRow0,
        input.WorldMatrixRow1,
        input.WorldMatrixRow2,
        input.WorldMatrixRow3
    );

    float4 worldPos = mul(float4(input.Position, 1.0), WorldMatrix);
    output.Position = mul(worldPos, WorldViewProjection);

	// Pass through texture coordinates
    output.TextureCoordinates = input.TextureCoordinates;
	
	// Pass through color (can be modulated later)
    output.FillColor = input.FillColor;
    output.StrokeColor = input.StrokeColor;
    
    // Pass through instance data for distancerange and pxrange
    output.PixelRanges = input.PixelRanges;

    return output;
}

// MSDF utility functions
float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

// MSDF utility functions
float median(float3 v)
{
    return max(min(v.r, v.g), min(max(v.r, v.g), v.b));
}

// Normalizes the provided vector, safely.
float2 SafeNormalize(float2 v)
{
    float vLength = length(v);

    vLength = (vLength > 0.0) ? 1.0 / vLength : 0.0;

    return v * vLength;
}

float GetOpacityFromDistance(float signedDistance, float distanceRange, float2 Jdx, float2 Jdy)
{
    const float distanceLimit = sqrt(2.0f) / 2.0f;
    const float thickness = 1.0f / distanceRange;
    float2 gradientDistance = SafeNormalize(float2(ddx(signedDistance), ddy(signedDistance)));
    float2 gradient = float2(gradientDistance.x * Jdx.x + gradientDistance.y * Jdy.x, gradientDistance.x * Jdx.y + gradientDistance.y * Jdy.y);
    float scaledDistanceLimit = min(thickness * distanceLimit * length(gradient), 0.5f);

    return smoothstep(-scaledDistanceLimit, scaledDistanceLimit, signedDistance);
}

// Standard MSDF pixel shader
float4 MainPS(VertexShaderOutput input) : COLOR
{
    // Sample the MSDF texture
    float3 msd = tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb;
    float sd = median(msd.r, msd.g, msd.b);
    
    // Calculate screen pixel distance
    float screenPxDistance = (sd - 0.5) * input.PixelRanges.x;
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
    
    // Apply text color with calculated opacity
    return float4(input.FillColor.rgb, input.FillColor.a * opacity);
}

// MSDF pixel shader with stroke support (using your existing ScreenPxRange)
float4 StrokedPS(VertexShaderOutput input) : COLOR
{
    float3 msd = tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb;
    float medianSample = median(msd.r, msd.g, msd.b);
    float signedDistance = medianSample - 0.5;

    // Use your existing ScreenPxRange calculation
    signedDistance *= input.PixelRanges.x;

    const float strokeThickness = 0.1875;
    // Calculate stroke distance - positive values result in outline along the edge
    float strokeDistance = -(abs(medianSample - 0.25 - strokeThickness) - strokeThickness);
    strokeDistance *= input.PixelRanges.x;
    
    float opacity = clamp(signedDistance + 0.5, 0.0, 1.0);
    float strokeOpacity = clamp(strokeDistance + 0.5, 0.0, 1.0);

    float4 strokeColor = input.StrokeColor;
    float4 finalColor = lerp(strokeColor, input.FillColor, opacity);
    
    return float4(finalColor.rgb, finalColor.a * max(opacity, strokeOpacity));
}

// Small text optimized MSDF pixel shader
float4 SmallTextPS(VertexShaderOutput input) : COLOR
{
    float2 pixelCoord = input.TextureCoordinates * AtlasSize;
    float2 Jdx = ddx(pixelCoord);
    float2 Jdy = ddy(pixelCoord);
    float3 sample = tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb;
    float signedDistance = median(sample) - 0.5f;
    
    float opacity = GetOpacityFromDistance(signedDistance, input.PixelRanges.y, Jdx, Jdy);
    float4 color;

    color.a = pow(abs(input.FillColor.a * opacity), 1.0f / 2.2f); // Correct for gamma, 2.2 is a valid gamma for most LCD monitors.
    color.rgb = input.FillColor.rgb;

    return color;
}

// Small text with stroke support
float4 StrokedSmallTextPS(VertexShaderOutput input) : COLOR
{
    float2 pixelCoord = input.TextureCoordinates * AtlasSize;
    float2 Jdx = ddx(pixelCoord);
    float2 Jdy = ddy(pixelCoord);
    float medianSample = median(tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb);
    float signedDistance = medianSample - 0.5f;

    const float strokeThickness = 0.1875f;
    float strokeDistance = -(abs(medianSample - 0.25f - strokeThickness) - strokeThickness);

    float opacity = GetOpacityFromDistance(signedDistance, input.PixelRanges.y, Jdx, Jdy);
    float strokeOpacity = GetOpacityFromDistance(strokeDistance, input.PixelRanges.y, Jdx, Jdy);
    
    float4 strokeColor = input.StrokeColor;
    return lerp(strokeColor, input.FillColor, opacity) * max(opacity, strokeOpacity);
}

// Main technique for standard MSDF text rendering
technique MSDFTextRendering
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL
            MainVS();
        PixelShader = compile PS_SHADERMODEL
            MainPS();
    }
}

// Technique for text with stroke/outline
technique MSDFTextWithStroke
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL
            MainVS();
        PixelShader = compile PS_SHADERMODEL
            StrokedPS();
    }
}

// Technique optimized for small text rendering
technique MSDFSmallText
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL
            MainVS();
        PixelShader = compile PS_SHADERMODEL
            SmallTextPS();
    }
}

// Technique for small text with stroke
technique MSDFSmallTextWithStroke
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL
            MainVS();
        PixelShader = compile PS_SHADERMODEL
            StrokedSmallTextPS();
    }
}