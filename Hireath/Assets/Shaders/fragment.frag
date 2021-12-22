#version 450

layout(location = 0) out vec4 fsout_Color;

layout(location = 0) in vec2 fsin_texCoords;
layout(location = 1) in vec3 fsin_worldPos;
layout(location = 2) in vec3 fsin_normal;

layout(set = 0, binding = 2) uniform PositionBuffer { vec3 cameraPosition; };

layout(set = 2, binding = 0) uniform AlbedoBuffer { vec3 albedo; };
layout(set = 2, binding = 1) uniform MetallicBuffer { float metallic; };
layout(set = 2, binding = 2) uniform RoughnessBuffer { float roughness; };
layout(set = 2, binding = 3) uniform AmbientOcclusionBuffer { float ambientOcclusion; };

layout(set = 3, binding = 0) uniform AmountOfLightsBuffer { int amountOfLights; };
layout(set = 3, binding = 1) uniform LightPositionsBuffer { vec3 lightPosition[128]; };
layout(set = 3, binding = 2) uniform LightColorsBuffer { vec3 lightColor[128]; };
layout(set = 3, binding = 3) uniform LightStrengthsBuffer { float lightStrength[128]; };

const float PI = 3.1415926535897932384626433832795;

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;
	
    float num   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
	
    return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
	
    return num / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);
	
    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
} 

void main()
{
    vec3 N = normalize(fsin_normal); 
    vec3 V = normalize(cameraPosition - fsin_worldPos);

    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedo, metallic);

    vec3 Lo = vec3(0.0);

    for(int i = 0; i < amountOfLights; ++i) 
    {
        vec3 lightPos = lightPosition[i];

        // calculate per-light radiance
        vec3 L = normalize(lightPos - fsin_worldPos);
        vec3 H = normalize(V + L);
        float distance    = length(lightPos - fsin_worldPos);
        float attenuation = lightStrength[i] / min((distance * distance), 1.0);	
        vec3 radiance     = lightColor[i] * attenuation;        
            
        // cook-torrance brdf
        float NDF = DistributionGGX(N, H, roughness);        
        float G   = GeometrySmith(N, V, L, roughness);      
        vec3 F    = fresnelSchlick(max(dot(H, V), 0.0), F0);       
            
        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;	  
            
        vec3 numerator    = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
        vec3 specular     = numerator / denominator;  
                
        // add to outgoing radiance Lo
        float NdotL = max(dot(N, L), 0.0);                
        Lo += (kD * albedo / PI + specular) * radiance * NdotL; 
    }

    vec3 ambient = vec3(0.03) * albedo * ambientOcclusion;
    vec3 color = ambient + Lo;
	
    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0/2.2));  
   
    fsout_Color = vec4(color, 1.0);
}