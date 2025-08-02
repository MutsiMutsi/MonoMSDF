#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
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

float4x4 WorldViewProjection;
float ZoomLevel;

// Vertex shader input structure
struct VertexShaderInput
{
    //VertexBuffer data
    float3 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 FillColor : COLOR0;
    float4 StrokeColor : COLOR1;
    
#if OPENGL
    float vertexID : TEXCOORD7;
#else
    uint vertexID : SV_VertexID;
#endif
    
    //Instanced data
    float4 WorldMatrixRow0 : TEXCOORD1;
    float4 WorldMatrixRow1 : TEXCOORD2;
    float4 WorldMatrixRow2 : TEXCOORD3;
    float4 WorldMatrixRow3 : TEXCOORD4;
    float2 PixelRanges : TEXCOORD5;
    
    float2 VertexRange : TEXCOORD6;
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
    
#if OPENGL
    if ((int) input.vertexID < input.VertexRange.x || (int) input.vertexID >= input.VertexRange.y)
    {
        output.FillColor = float4(0, 0, 0, 0);
        output.PixelRanges = 0.0;
        output.Position = float4(-10000, -10000, -10000, -10000);
        output.StrokeColor = float4(0, 0, 0, 0);
        output.TextureCoordinates = float2(0, 0);
        return output;
    }
#else
    if (input.vertexID < input.VertexRange.x || input.vertexID >= input.VertexRange.y)
    {
        output.FillColor = float4(0, 0, 0, 0);
        output.PixelRanges = 0.0;
        output.Position = float4(-10000, -10000, -10000, -10000);
        output.StrokeColor = float4(0, 0, 0, 0);
        output.TextureCoordinates = float2(0, 0);
        return output;
    }
#endif
    
    
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
    
   // float2 scale = float2(length(WorldViewProjection._11_12), length(WorldViewProjection._21_22));
   // output.PixelRanges.x /= scale.x;
    
    output.PixelRanges.x *= ZoomLevel;
    
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
    if (input.StrokeColor.a == 0.0)
    {
        return MainPS(input);
    }
    
    float3 msd = tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb;
    float medianSample = median(msd.r, msd.g, msd.b);
    float signedDistance = medianSample - 0.5;

    // Use your existing ScreenPxRange calculation
    signedDistance *= input.PixelRanges.x;

    const float strokeThickness = 48.0 / ddx(input.TextureCoordinates.x);
    
    // Calculate stroke distance - positive values result in outline along the edge
    float strokeDistance = -(abs(medianSample - 0.25 - strokeThickness) - strokeThickness);
    strokeDistance *= input.PixelRanges.x;
    
    float opacity = clamp(signedDistance + 0.5, 0.0, 1.0);
    float strokeOpacity = clamp(strokeDistance + 0.5, 0.0, 1.0);

    float4 strokeColor = input.StrokeColor;
    float4 finalColor = lerp(strokeColor, input.FillColor, opacity);
    
    return float4(finalColor.rgb, finalColor.a * max(opacity, strokeOpacity));
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

// Small text optimized MSDF pixel shader
float4 SmallTextPS(VertexShaderOutput input) : COLOR
{
    float2 pixelCoord = input.TextureCoordinates * float2(256, 256);
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
    float2 pixelCoord = input.TextureCoordinates;
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



float4 SubPixelPS(VertexShaderOutput input) : COLOR
{
    float2 dx = ddx(input.TextureCoordinates);
    float pixelWidthInUV = length(dx);
    float subpixelOffsetX = pixelWidthInUV / 3.0;
    
    float2 uv = input.TextureCoordinates;

    // Sample shifted MSDF values for RGB subpixels
    float3 msdR = tex2D(SpriteTextureSampler, uv - float2(subpixelOffsetX, 0)).rgb;
    float3 msdG = tex2D(SpriteTextureSampler, uv).rgb;
    float3 msdB = tex2D(SpriteTextureSampler, uv + float2(subpixelOffsetX, 0)).rgb;

    // Compute median distances
    float sdR = median(msdR.r, msdR.g, msdR.b);
    float sdG = median(msdG.r, msdG.g, msdG.b);
    float sdB = median(msdB.r, msdB.g, msdB.b);

    // Use screen-space antialiasing range
    float pxRange = input.PixelRanges.x;

    // Convert distances to opacities
    float opacityR = clamp((sdR - 0.5) * pxRange + 0.5, 0.0, 1.0);
    float opacityG = clamp((sdG - 0.5) * pxRange + 0.5, 0.0, 1.0);
    float opacityB = clamp((sdB - 0.5) * pxRange + 0.5, 0.0, 1.0);

    // Gamma correction for better perceptual subpixel rendering
    float gamma = 1.0 / 2.2;
    opacityR = pow(opacityR, gamma);
    opacityG = pow(opacityG, gamma);
    opacityB = pow(opacityB, gamma);

    // Compose subpixel-colored output
    float3 subpixelOpacity = float3(opacityR, opacityG, opacityB);
    float3 rgb = input.FillColor.rgb * subpixelOpacity;

    // Average alpha for consistent coverage
    float alpha = input.FillColor.a * (opacityR + opacityG + opacityB) / 3.0;

    return float4(rgb, alpha);
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

// Technique for text with stroke/outline
technique MSDFSubPixel
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL
            MainVS();
        PixelShader = compile PS_SHADERMODEL
            SubPixelPS();
    }
}
