#version 450

layout(location = 0) in vec3 fsin_Position;
layout(location = 1) in vec3 fsin_Normal;

layout(set = 0, binding = 2) uniform PositionBuffer { vec3 cameraPosition; };

layout(set = 2, binding = 0) uniform AlbedoBuffer { vec3 albedo; };
layout(set = 2, binding = 1) uniform EmissivityBuffer { vec3 emissivity; };
layout(set = 2, binding = 2) uniform RoughnessBuffer { float roughness; };
layout(set = 2, binding = 3) uniform ReflectivityBuffer { vec3 reflectivity; };

layout(location = 0) out vec4 fsout_Color;

float PI = 3.1415926535897932384626433832795;

// GGX/Throwbridge-Reitz normal distribution function
float D (float alpha, vec3 N, vec3 H) {
    float numerator = pow(alpha, 2.0);

    float NdotH = max(dot(N, H), 0.0);
    float denominator = PI * pow(pow(NdotH, 2.0) * (pow(alpha, 2.0) - 1.0) + 1.0, 2.0);
    denominator = max(denominator, 0.000001);

    return numerator / denominator;
}

// Schlick-Beckmann Geometric Shadowing Function
float G1(float alpha, vec3 N, vec3 X) {
    float numerator = max(dot(N, X), 0.0);

    float k = alpha / 2.0;
    float denominator = max(dot(N, X), 0.0) * (1.0 - k) + k;
    denominator = max(denominator, 0.000001);

    return numerator / denominator;
}

// Smith model
float G(float alpha, vec3 N, vec3 V, vec3 L) {
    return G1(alpha, N, V) * G1(alpha, N, L);
}

// Fresnel-Schlick funtion
vec3 F(vec3 F0, vec3 V, vec3 H) {
    return F0 + (vec3(1.0) - F0) * pow(1.0 - max(dot(V, H), 0.0), 5.0);
}

void main()
{
    vec3 position = fsin_Position;
    vec3 normal = fsin_Normal;
    vec3 cameraPosition = cameraPosition;
    vec3 lightPosition = vec3(10, 0, 0);
    vec3 lightColor = vec3(1, 1, 1);
    vec3 albedo = albedo;
    vec3 emissivity = emissivity;
    float roughness = roughness;
    vec3 reflectivity = reflectivity;

    float alpha = 1;

    vec3 F0 = albedo;

    // Main vectors 
    vec3 N = normalize(normal);
    vec3 V = normalize(cameraPosition - position);

    // Directional light
    vec3 L = normalize(lightPosition);

    // Point light
    // vec3 L = normalize(lightPosition - position);

    vec3 H = normalize(V + L);

    // PBR
    vec3 Ks = F(F0, V, H);
    vec3 Kd = vec3(1.0) - Ks;

    vec3 lambert = albedo / PI;

    vec3 cookTorranceNumerator = D(alpha, N, H) * G(alpha, N, V, L) * F(F0, V, H);
    float cookTorranceDenumerator = 4.0 * max(dot(V, N), 0.0) * max(dot(L, N), 0.0);
    cookTorranceDenumerator = max(cookTorranceDenumerator, 0.000001);

    vec3 cookTorrance = cookTorranceNumerator / cookTorranceDenumerator;

    vec3 BRDF = Kd * lambert + cookTorrance;
    vec3 outgoingLight = emissivity + BRDF * 10 * lightColor * max(dot(L, N), 0.0);
 
    fsout_Color = vec4(outgoingLight, 1.0);
}