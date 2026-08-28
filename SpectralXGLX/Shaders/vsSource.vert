#version 300 es
in vec3 aPosition;
in vec3 aNormal;
in vec2 aTexCoord;
uniform vec2 uUVOffset;
uniform vec2 uUVScale;
uniform mat4 uMVP;
uniform mat4 uModel;
uniform mat4 uLightVP;
uniform vec2 uJitter;
out vec3 vNormal;
out vec3 vWorldPos;
out vec2 vTexCoord;
out vec4 vShadowCoord;
void main() {
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vec4 pos = uMVP * vec4(aPosition, 1.0);
    pos.xy += uJitter * pos.w;
    gl_Position = pos;
    vWorldPos = worldPos.xyz;
    vNormal = normalize(mat3(uModel) * aNormal);
    vTexCoord = aTexCoord * uUVScale + uUVOffset;
    vShadowCoord = uLightVP * worldPos;
}
