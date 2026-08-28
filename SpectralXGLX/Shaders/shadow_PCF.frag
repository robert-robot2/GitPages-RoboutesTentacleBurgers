#version 300 es
precision mediump float;

in vec3 vNormal;
in vec3 vWorldPos;
in vec2 vTexCoord;
in vec4 vShadowCoord;

const int MAX_LIGHTS = 32;
const int MAX_SHADOW_LIGHTS = 8;

uniform int uLightCount;
uniform vec3 uLightPos[MAX_LIGHTS];
uniform vec3 uLightColor[MAX_LIGHTS];
uniform vec3 uLightDir[MAX_LIGHTS];
uniform float uLightIntensity[MAX_LIGHTS];
uniform float uLightRange[MAX_LIGHTS];
uniform int uLightType[MAX_LIGHTS];
uniform float uLightSpotAngle[MAX_LIGHTS];

uniform vec4 uColor;
uniform vec3 uCamPos;
uniform sampler2D uTexture;
uniform bool uHasTexture;
uniform bool uIsEmissive;
uniform float uEmissiveIntensity;

uniform sampler2D uShadowMap0;
uniform sampler2D uShadowMap1;
uniform sampler2D uShadowMap2;
uniform sampler2D uShadowMap3;
uniform sampler2D uShadowMap4;
uniform sampler2D uShadowMap5;
uniform sampler2D uShadowMap6;
uniform sampler2D uShadowMap7;
uniform mat4 uLightVP0;
uniform mat4 uLightVP1;
uniform mat4 uLightVP2;
uniform mat4 uLightVP3;
uniform mat4 uLightVP4;
uniform mat4 uLightVP5;
uniform mat4 uLightVP6;
uniform mat4 uLightVP7;

out vec4 fragColor;

float sampleShadowMap(int index, vec2 uv) {
    if (index >= MAX_SHADOW_LIGHTS) return 1.0;
    if (index == 0) return texture(uShadowMap0, uv).r;
    if (index == 1) return texture(uShadowMap1, uv).r;
    if (index == 2) return texture(uShadowMap2, uv).r;
    if (index == 3) return texture(uShadowMap3, uv).r;
    if (index == 4) return texture(uShadowMap4, uv).r;
    if (index == 5) return texture(uShadowMap5, uv).r;
    if (index == 6) return texture(uShadowMap6, uv).r;
    if (index == 7) return texture(uShadowMap7, uv).r;
    return 1.0;
}

vec4 getLightVPPos(int index, vec4 worldPos) {
    if (index >= MAX_SHADOW_LIGHTS) return vec4(0.0);
    if (index == 0) return uLightVP0 * worldPos;
    if (index == 1) return uLightVP1 * worldPos;
    if (index == 2) return uLightVP2 * worldPos;
    if (index == 3) return uLightVP3 * worldPos;
    if (index == 4) return uLightVP4 * worldPos;
    if (index == 5) return uLightVP5 * worldPos;
    if (index == 6) return uLightVP6 * worldPos;
    if (index == 7) return uLightVP7 * worldPos;
    return vec4(0.0);
}

float shadowFactor(int lightIndex, vec3 normal, vec3 lightDir) {
    vec3 toLight = uLightPos[lightIndex] - vWorldPos;
    if (dot(toLight, toLight) < 2.0) return 1.0;

    vec4 shadowCoord = getLightVPPos(lightIndex, vec4(vWorldPos, 1.0));
    vec3 proj = shadowCoord.xyz / shadowCoord.w;
    proj = proj * 0.5 + 0.5;
    if (proj.x < 0.0 || proj.x > 1.0 ||
        proj.y < 0.0 || proj.y > 1.0 ||
        proj.z > 1.0) return 1.0;

    float currentDepth = proj.z;
    float cosTheta = clamp(dot(normal, lightDir), 0.0, 1.0);
    float bias = mix(0.005, 0.001, cosTheta);

    float shadow = 0.0;
    vec2 texelSize = vec2(1.0 / 1024.0);
    for (int x = -2; x <= 2; x++) {
        for (int y = -2; y <= 2; y++) {
            float pcfDepth = sampleShadowMap(lightIndex,
                proj.xy + vec2(float(x), float(y)) * texelSize);
            shadow += currentDepth - bias > pcfDepth ? 0.0 : 1.0;
        }
    }
    return shadow / 25.0;
}

void main() {
   vec4 baseColor = uHasTexture ? texture(uTexture, vTexCoord) * uColor : uColor;
    if (baseColor.a < 0.1) discard;
    if (uIsEmissive) {
        fragColor = vec4(baseColor.rgb * uEmissiveIntensity, baseColor.a);
        return;
    }

    vec3 normal = normalize(gl_FrontFacing ? vNormal : -vNormal);
    vec3 viewDir = normalize(uCamPos - vWorldPos);
    vec3 totalDiffuse  = vec3(0.0);
    vec3 totalSpecular = vec3(0.0);

    for (int i = 0; i < MAX_LIGHTS; i++) {
        if (i >= uLightCount) break;

        vec3 lightDir;
        float attenuation;

        if (uLightType[i] == 1) {
            lightDir    = normalize(-uLightDir[i]);
            attenuation = 1.0;
        } else if (uLightType[i] == 2) {
            vec3 toLight   = uLightPos[i] - vWorldPos;
            float distance = length(toLight);
            lightDir       = normalize(toLight);
            attenuation    = 1.0 / (1.0 + (distance * distance) /
                (uLightRange[i] * uLightRange[i]));
            attenuation    = attenuation * attenuation * attenuation;
            float cosAngle = cos(radians(uLightSpotAngle[i]));
            float cosOuter = cos(radians(uLightSpotAngle[i] * 1.3));
            float spotDot  = dot(-lightDir, normalize(uLightDir[i]));
            attenuation   *= smoothstep(cosOuter, cosAngle, spotDot);
        } else if (uLightType[i] == 3) {
            vec3 upRef     = abs(uLightDir[i].z) < 0.9
                ? vec3(0.0, 0.0, 1.0) : vec3(0.0, 1.0, 0.0);
            vec3 areaRight = normalize(cross(uLightDir[i], upRef));
            vec3 areaUp    = normalize(cross(areaRight, uLightDir[i]));
            float hw       = uLightSpotAngle[i] * 0.1;
            float hh       = uLightRange[i] * 0.05;
            vec3 c0 = uLightPos[i] + areaRight * hw + areaUp * hh;
            vec3 c1 = uLightPos[i] - areaRight * hw + areaUp * hh;
            vec3 c2 = uLightPos[i] + areaRight * hw - areaUp * hh;
            vec3 c3 = uLightPos[i] - areaRight * hw - areaUp * hh;
            lightDir    = normalize(
                normalize(c0 - vWorldPos) + normalize(c1 - vWorldPos) +
                normalize(c2 - vWorldPos) + normalize(c3 - vWorldPos));
            float dist  = length(uLightPos[i] - vWorldPos);
            attenuation = 1.0 / (1.0 + (dist * dist) /
                (uLightRange[i] * uLightRange[i]));
            attenuation = attenuation * attenuation;
        } else {
            vec3 toLight   = uLightPos[i] - vWorldPos;
            float distance = length(toLight);
            lightDir       = normalize(toLight);
            attenuation    = 1.0 / (1.0 + (distance * distance) /
                (uLightRange[i] * uLightRange[i]));
            attenuation    = attenuation * attenuation * attenuation;
        }

        float diff    = max(dot(normal, lightDir), 0.0);
        vec3 halfDir  = normalize(lightDir + viewDir);
        float spec    = uLightType[i] == 3 ? 0.0
            : pow(max(dot(normal, halfDir), 0.0), 32.0);
        float shadow  = shadowFactor(i, normal, lightDir);

        totalDiffuse  += shadow * diff * uLightColor[i] *
            uLightIntensity[i] * attenuation;
        totalSpecular += shadow * spec * uLightColor[i] *
            uLightIntensity[i] * attenuation * 0.3;
    }

    fragColor = vec4(
        (totalDiffuse + totalSpecular) * baseColor.rgb,
        baseColor.a);
}